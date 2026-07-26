#include "pch.h"
#include "Scripting.h"
#include "Engine.h"
#include "JsonValidation.h"
#include "lua.h"
#include "lauxlib.h"
#include "lualib.h"
#include <cstdlib>
#include <fstream>
#include <iterator>

namespace Veris
{
    struct ScriptRuntime::Instance
    {
        lua_State* State = nullptr;
        ScriptRuntime& Runtime;
        Engine& Host;
        std::uint64_t Entity;
        std::string Path;
        std::size_t Bytes = 0;
        int Budget = 100;
        int Callbacks = LUA_NOREF;
        bool Failed = false;
        Instance(ScriptRuntime& runtime, Engine& engine, std::uint64_t entity, std::string path);
        ~Instance() { if (State) lua_close(State); }
        void Load(const std::filesystem::path& path);
        void Call(const char* name, double seconds);
        void Check(int status);
        static void* Allocate(void* owner, void* block, std::size_t oldSize, std::size_t size);
        static int Bootstrap(lua_State* state);
        static void Instructions(lua_State* state, lua_Debug*);
        static int Print(lua_State* state);
        static int Invoke(lua_State* state);
        static int StoreCallbacks(lua_State* state);
    };

    void* ScriptRuntime::Instance::Allocate(void* owner, void* block, std::size_t oldSize, std::size_t size)
    {
        auto& instance = *static_cast<Instance*>(owner);
        if (!block) oldSize = 0;
        if (size == 0) { std::free(block); instance.Bytes -= oldSize; return nullptr; }
        if (size > 4 * 1024 * 1024 || instance.Bytes - oldSize > 4 * 1024 * 1024 - size) return nullptr;
        auto replacement = std::realloc(block, size);
        if (replacement) instance.Bytes = instance.Bytes - oldSize + size;
        return replacement;
    }

    ScriptRuntime::Instance::Instance(ScriptRuntime& runtime, Engine& engine, std::uint64_t entity, std::string path)
        : Runtime(runtime), Host(engine), Entity(entity), Path(std::move(path))
    {
        State = lua_newstate(Allocate, this);
        if (!State) throw std::runtime_error("Cannot allocate Lua state.");
        *static_cast<Instance**>(lua_getextraspace(State)) = this;
    }

    int ScriptRuntime::Instance::Bootstrap(lua_State* state)
    {
        auto& instance = **static_cast<Instance**>(lua_getextraspace(state));
        const luaL_Reg libraries[]{{"_G", luaopen_base}, {"table", luaopen_table}, {"string", luaopen_string}, {"math", luaopen_math}, {"utf8", luaopen_utf8}, {nullptr, nullptr}};
        for (const auto* library = libraries; library->name; ++library) { luaL_requiref(state, library->name, library->func, 1); lua_pop(state, 1); }
        for (const auto* name : {"dofile", "loadfile", "load", "collectgarbage", "pcall", "xpcall"}) { lua_pushnil(state); lua_setglobal(state, name); }
        lua_pushcfunction(state, Print); lua_setglobal(state, "print");
        lua_pushinteger(state, static_cast<lua_Integer>(instance.Entity)); lua_setglobal(state, "self");
        RegisterGameplayApi(state, instance.Host);
        return 0;
    }

    int ScriptRuntime::Instance::Print(lua_State* state)
    {
        auto& instance = **static_cast<Instance**>(lua_getextraspace(state));
        std::string message;
        for (int index = 1; index <= lua_gettop(state); ++index)
        {
            std::size_t length = 0;
            const auto text = luaL_tolstring(state, index, &length);
            if (message.size() + length > 4096) return luaL_error(state, "Log message exceeds 4096 bytes.");
            if (index > 1) message += '\t';
            message.append(text, length); lua_pop(state, 1);
        }
        instance.Runtime.Log(instance.Path + " [" + std::to_string(instance.Entity) + "]: " + message);
        return 0;
    }

    void ScriptRuntime::Instance::Instructions(lua_State* state, lua_Debug*)
    {
        auto& instance = **static_cast<Instance**>(lua_getextraspace(state));
        if (--instance.Budget <= 0) luaL_error(state, "Script instruction budget exceeded.");
    }
    void ScriptRuntime::Instance::Check(int status)
    {
        if (status == LUA_OK) return;
        const auto message = lua_tostring(State, -1);
        const std::string error = message ? message : "Lua raised a non-string error.";
        lua_settop(State, 0);
        throw std::runtime_error(error);
    }
    void ScriptRuntime::Instance::Load(const std::filesystem::path& path)
    {
        lua_pushcfunction(State, Bootstrap); Check(lua_pcall(State, 0, 0, 0));
        std::ifstream stream(path, std::ios::binary);
        if (!stream) throw std::runtime_error("Cannot read script: " + Path);
        const std::string source((std::istreambuf_iterator<char>(stream)), std::istreambuf_iterator<char>());
        Check(luaL_loadbufferx(State, source.data(), source.size(), Path.c_str(), "t"));
        Budget = 100; lua_sethook(State, Instructions, LUA_MASKCOUNT, 1000);
        Check(lua_pcall(State, 0, 1, 0));
        if (!lua_istable(State, -1)) throw std::invalid_argument("A script must return a callback table.");
        lua_pushcfunction(State, StoreCallbacks); lua_insert(State, -2);
        Check(lua_pcall(State, 1, 0, 0));
        Call("start", 0);
    }
    void ScriptRuntime::Instance::Call(const char* name, double seconds)
    {
        lua_pushcfunction(State, Invoke);
        lua_pushlightuserdata(State, const_cast<char*>(name));
        lua_pushnumber(State, seconds); Budget = 100;
        Check(lua_pcall(State, 2, 0, 0));
    }
    int ScriptRuntime::Instance::StoreCallbacks(lua_State* state)
    {
        auto& instance = **static_cast<Instance**>(lua_getextraspace(state));
        instance.Callbacks = luaL_ref(state, LUA_REGISTRYINDEX);
        return 0;
    }
    int ScriptRuntime::Instance::Invoke(lua_State* state)
    {
        auto& instance = **static_cast<Instance**>(lua_getextraspace(state));
        const auto name = static_cast<const char*>(lua_touserdata(state, 1));
        lua_rawgeti(state, LUA_REGISTRYINDEX, instance.Callbacks);
        lua_pushstring(state, name); lua_rawget(state, -2); lua_remove(state, -2);
        if (lua_isnil(state, -1)) return 0;
        if (!lua_isfunction(state, -1)) return luaL_error(state, "Callback must be a function: %s", name);
        lua_pushvalue(state, 2); lua_call(state, 1, 0);
        return 0;
    }

    ScriptRuntime::ScriptRuntime() = default;
    ScriptRuntime::~ScriptRuntime() { Clear(); }
    void ScriptRuntime::SetRoot(const std::filesystem::path& directory) { _files.SetRoot(directory); Clear(); }
    void ScriptRuntime::Clear() { _instances.clear(); _diagnostics.clear(); }
    void ScriptRuntime::Log(std::string message)
    {
        if (_diagnostics.size() == 200) _diagnostics.erase(_diagnostics.begin());
        _diagnostics.push_back(std::move(message));
    }
    void ScriptRuntime::Validate(const ScriptComponent& script)
    {
        const auto path = _files.ContentPath(std::filesystem::u8path(script.Path));
        if (path.extension() != L".lua" || !std::filesystem::is_regular_file(path) || std::filesystem::file_size(path) > 256 * 1024)
            throw std::invalid_argument("Scripts must be .lua files inside GameCode and at most 256 KiB.");
    }
    void ScriptRuntime::Update(Engine& engine, double seconds)
    {
        for (auto instance = _instances.begin(); instance != _instances.end();)
        {
            const auto& scene = engine.ActiveScene();
            if (!scene.Contains(instance->first) || !scene.Get(instance->first).Script || !scene.Get(instance->first).Script->Enabled || scene.Get(instance->first).Script->Path != instance->second->Path)
                instance = _instances.erase(instance);
            else ++instance;
        }
        std::vector<std::uint64_t> entities;
        for (const auto& entry : engine.ActiveScene().Entities()) if (entry.second.Script && entry.second.Script->Enabled) entities.push_back(entry.first);
        for (const auto id : entities)
            if (engine.ActiveScene().Contains(id) && engine.ActiveScene().Get(id).Script && engine.ActiveScene().Get(id).Script->Enabled) UpdateEntity(engine, id, seconds);
    }
    void ScriptRuntime::UpdateEntity(Engine& engine, std::uint64_t id, double seconds)
    {
        auto found = _instances.find(id);
        if (found != _instances.end() && found->second->Failed) return;
        try
        {
            if (found == _instances.end())
            {
                const auto script = *engine.ActiveScene().Get(id).Script;
                auto instance = std::make_unique<Instance>(*this, engine, id, script.Path);
                found = _instances.emplace(id, std::move(instance)).first;
                Validate(script); found->second->Load(_files.ContentPath(std::filesystem::u8path(script.Path)));
            }
            found->second->Call("update", seconds);
        }
        catch (const std::exception& exception)
        {
            if (found != _instances.end()) found->second->Failed = true;
            Log("Entity " + std::to_string(id) + ": " + exception.what());
        }
    }
    std::string SerializeScript(const ScriptComponent& script) { return Json::Document{{"path", script.Path}, {"enabled", script.Enabled}}.dump(); }
    ScriptComponent DeserializeScript(const std::string& json)
    {
        const auto document = Json::Parse(json); Json::Fields(document, {"path", "enabled"});
        ScriptComponent script{document.at("path").get<std::string>(), document.at("enabled").get<bool>()};
        if (script.Path.empty() || script.Path.find('\0') != std::string::npos) throw std::invalid_argument("A script path is required.");
        return script;
    }
}
