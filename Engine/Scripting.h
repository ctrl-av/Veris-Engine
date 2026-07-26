#pragma once
#include "TextureResources.h"
#include <cstdint>
#include <map>
#include <memory>
#include <string>
#include <vector>

struct lua_State;
namespace Veris
{
    class Engine;
    struct ScriptComponent { std::string Path; bool Enabled = true; };
    std::string SerializeScript(const ScriptComponent& script);
    ScriptComponent DeserializeScript(const std::string& json);

    void RegisterGameplayApi(lua_State* state, Engine& engine);

    class ScriptRuntime
    {
    public:
        ScriptRuntime();
        ~ScriptRuntime();
        void SetRoot(const std::filesystem::path& directory);
        void Update(Engine& engine, double seconds);
        void Clear();
        void Validate(const ScriptComponent& script);
        const std::vector<std::string>& Diagnostics() const { return _diagnostics; }
        void Log(std::string message);
        std::size_t InstanceCount() const { return _instances.size(); }

    private:
        struct Instance;
        void UpdateEntity(Engine& engine, std::uint64_t id, double seconds);
        TextureResources _files;
        std::map<std::uint64_t, std::unique_ptr<Instance>> _instances;
        std::vector<std::string> _diagnostics;
    };
}
