#include "pch.h"
#include "Engine.h"
#include "lua.h"
#include "lauxlib.h"
#include <cmath>
#include <limits>

namespace
{
    Veris::EntityId EntityArgument(lua_State* state, int index)
    {
        const auto id = luaL_checkinteger(state, index);
        if (id <= 0) luaL_error(state, "Entity IDs must be positive integers.");
        return static_cast<Veris::EntityId>(id);
    }
    float Number(lua_State* state, int index)
    {
        const auto number = luaL_checknumber(state, index);
        if (!std::isfinite(number) || std::abs(number) > std::numeric_limits<float>::max()) luaL_error(state, "Gameplay numbers must be finite floats.");
        return static_cast<float>(number);
    }
    std::string Text(lua_State* state, int index)
    {
        std::size_t length = 0;
        const auto text = luaL_checklstring(state, index, &length);
        if (length > 4096 || std::string_view(text, length).find('\0') != std::string_view::npos) luaL_error(state, "Gameplay text must be at most 4096 bytes with no embedded null.");
        return std::string(text, length);
    }
    template<int (*Operation)(lua_State*, Veris::Engine&)>
    int Boundary(lua_State* state)
    {
        auto& engine = *static_cast<Veris::Engine*>(lua_touserdata(state, lua_upvalueindex(1)));
        try { return Operation(state, engine); }
        catch (const std::exception& exception) { return luaL_error(state, "%s", exception.what()); }
    }
    int Position(lua_State* state, Veris::Engine& engine)
    {
        const auto& transform = engine.ActiveScene().Get(EntityArgument(state, 1)).Transform;
        lua_pushnumber(state, transform.Position.x); lua_pushnumber(state, transform.Position.y); return 2;
    }
    int SetPosition(lua_State* state, Veris::Engine& engine)
    {
        const sf::Vector2f position{Number(state, 2), Number(state, 3)};
        auto& entity = engine.ActiveScene().Get(EntityArgument(state, 1));
        if (entity.Physics && (std::abs(position.x) > 100000000 || std::abs(position.y) > 100000000)) throw std::invalid_argument("Physics positions exceed supported bounds.");
        entity.Transform.Position = position; return 0;
    }
    int Rotation(lua_State* state, Veris::Engine& engine)
    {
        lua_pushnumber(state, engine.ActiveScene().Get(EntityArgument(state, 1)).Transform.Rotation); return 1;
    }
    int SetRotation(lua_State* state, Veris::Engine& engine)
    {
        const auto rotation = Number(state, 2);
        engine.ActiveScene().Get(EntityArgument(state, 1)).Transform.Rotation = rotation; return 0;
    }
    int Velocity(lua_State* state, Veris::Engine& engine)
    {
        const auto& body = engine.ActiveScene().Get(EntityArgument(state, 1)).Physics;
        if (!body) throw std::invalid_argument("Velocity requires a physics body.");
        lua_pushnumber(state, body->Velocity.x); lua_pushnumber(state, body->Velocity.y); return 2;
    }
    int SetVelocity(lua_State* state, Veris::Engine& engine)
    {
        auto& body = engine.ActiveScene().Get(EntityArgument(state, 1)).Physics;
        if (!body) throw std::invalid_argument("Velocity requires a physics body.");
        auto replacement = *body; replacement.Velocity = {Number(state, 2), Number(state, 3)}; replacement.Validate(); body = replacement; return 0;
    }
    int Impulse(lua_State* state, Veris::Engine& engine)
    {
        engine.Physics().ApplyImpulse(engine.ActiveScene(), EntityArgument(state, 1), {Number(state, 2), Number(state, 3)}); return 0;
    }
    int Action(lua_State* state, Veris::Engine& engine)
    {
        const auto name = Text(state, 1);
        const auto button = engine.Actions().Contains(name) ? engine.Actions().Get(name) : Veris::ButtonState{};
        lua_pushboolean(state, button.Down); lua_pushboolean(state, button.Pressed); lua_pushboolean(state, button.Released); return 3;
    }
    int Find(lua_State* state, Veris::Engine& engine)
    {
        const auto name = Text(state, 1);
        for (const auto& entry : engine.ActiveScene().Entities())
            if (entry.second.Name == name) { lua_pushinteger(state, static_cast<lua_Integer>(entry.first)); return 1; }
        lua_pushinteger(state, 0); return 1;
    }
    int Create(lua_State* state, Veris::Engine& engine)
    {
        if (engine.ActiveScene().Entities().size() >= 100000) throw std::invalid_argument("Scene entity limit reached.");
        lua_pushinteger(state, static_cast<lua_Integer>(engine.ActiveScene().CreateEntity(Text(state, 1)))); return 1;
    }
    int Destroy(lua_State* state, Veris::Engine& engine) { engine.ActiveScene().DestroyEntity(EntityArgument(state, 1)); return 0; }
    int PlayAudio(lua_State* state, Veris::Engine& engine)
    {
        const auto id = EntityArgument(state, 1); const auto& source = engine.ActiveScene().Get(id).Audio;
        if (!source) throw std::invalid_argument("Entity has no audio source.");
        engine.Audio().Play(id, *source); return 0;
    }
    int PauseAudio(lua_State* state, Veris::Engine& engine) { engine.Audio().Pause(EntityArgument(state, 1)); return 0; }
    int StopAudio(lua_State* state, Veris::Engine& engine) { engine.Audio().Stop(EntityArgument(state, 1)); return 0; }
    int Animation(lua_State* state, Veris::Engine& engine)
    {
        auto& animation = engine.ActiveScene().Get(EntityArgument(state, 1)).Animation;
        if (!animation || !lua_isboolean(state, 2)) throw std::invalid_argument("Animation requires an animated entity and a boolean playing state.");
        animation->Playing = lua_toboolean(state, 2) != 0; return 0;
    }
    int Tint(lua_State* state, Veris::Engine& engine)
    {
        const auto tint = luaL_checkinteger(state, 2);
        auto& sprite = engine.ActiveScene().Get(EntityArgument(state, 1)).Sprite;
        if (!sprite || tint < 0 || tint > 0xffffffffLL) throw std::invalid_argument("Tint requires a sprite and an unsigned RGBA32 color.");
        sprite->Tint = sf::Color(static_cast<std::uint32_t>(tint)); return 0;
    }
    int SetCameraView(lua_State* state, Veris::Engine& engine)
    {
        auto camera = engine.ActiveCamera();
        camera.SetCenter({Number(state, 1), Number(state, 2)}); camera.SetZoom(Number(state, 3)); camera.SetRotation(Number(state, 4));
        engine.ActiveCamera() = camera; return 0;
    }
    int Prefab(lua_State* state, Veris::Engine& engine)
    {
        const auto id = engine.InstantiatePrefab(std::filesystem::u8path(Text(state, 1)), {Number(state, 2), Number(state, 3)});
        lua_pushinteger(state, static_cast<lua_Integer>(id)); return 1;
    }
    int Transition(lua_State* state, Veris::Engine& engine)
    {
        engine.RequestScene(std::filesystem::u8path(Text(state, 1))); return 0;
    }
    int Contacts(lua_State* state, Veris::Engine& engine)
    {
        const auto id = EntityArgument(state, 1); engine.ActiveScene().Get(id);
        lua_newtable(state); int index = 1;
        for (const auto& contact : engine.Physics().Contacts())
        {
            if (contact.First != id && contact.Second != id) continue;
            lua_newtable(state);
            lua_pushinteger(state, static_cast<lua_Integer>(contact.First == id ? contact.Second : contact.First)); lua_setfield(state, -2, "other");
            lua_pushstring(state, contact.Phase == Veris::ContactPhase::Begin ? "begin" : contact.Phase == Veris::ContactPhase::Stay ? "stay" : "end"); lua_setfield(state, -2, "phase");
            lua_pushboolean(state, contact.Trigger); lua_setfield(state, -2, "trigger");
            const auto direction = contact.First == id ? 1.f : -1.f;
            lua_pushnumber(state, contact.Normal.x * direction); lua_setfield(state, -2, "normalX");
            lua_pushnumber(state, contact.Normal.y * direction); lua_setfield(state, -2, "normalY");
            if (contact.HasPoint)
            {
                lua_pushnumber(state, contact.Point.x); lua_setfield(state, -2, "pointX");
                lua_pushnumber(state, contact.Point.y); lua_setfield(state, -2, "pointY");
            }
            lua_rawseti(state, -2, index++);
        }
        return 1;
    }
}

namespace Veris
{
    void RegisterGameplayApi(lua_State* state, Engine& engine)
    {
        const luaL_Reg functions[]{ {"position", Boundary<Position>}, {"set_position", Boundary<SetPosition>}, {"rotation", Boundary<Rotation>}, {"set_rotation", Boundary<SetRotation>},
            {"velocity", Boundary<Velocity>}, {"set_velocity", Boundary<SetVelocity>}, {"impulse", Boundary<Impulse>}, {"action", Boundary<Action>},
            {"find", Boundary<Find>}, {"create", Boundary<Create>}, {"destroy", Boundary<Destroy>}, {"play_audio", Boundary<PlayAudio>},
            {"pause_audio", Boundary<PauseAudio>}, {"stop_audio", Boundary<StopAudio>}, {"animation", Boundary<Animation>}, {"tint", Boundary<Tint>}, {"camera", Boundary<SetCameraView>}, {"contacts", Boundary<Contacts>}, {"transition", Boundary<Transition>}, {"prefab", Boundary<Prefab>}, {nullptr, nullptr} };
        lua_newtable(state);
        for (const auto* function = functions; function->name; ++function)
        {
            lua_pushlightuserdata(state, &engine); lua_pushcclosure(state, function->func, 1); lua_setfield(state, -2, function->name);
        }
        lua_setglobal(state, "engine");
    }
}
