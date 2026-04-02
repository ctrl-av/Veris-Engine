#include "VerisInterop.h"
#include "../Engine/Engine.h"
#include <SFML/Graphics/Image.hpp>
#include <Windows.h>
#include <cstring>
#include <iostream>
#include <limits>
#include <map>
#include <iterator>
#include <mutex>
#include <thread>

namespace
{
    struct EngineSession
    {
        Veris::Engine Engine;
        std::thread::id Owner = std::this_thread::get_id();
    };

    std::mutex SessionsMutex;
    std::map<std::uint64_t, std::unique_ptr<EngineSession>> Sessions;
    std::uint64_t NextSession = 1;
    thread_local std::string LastError;

    template<typename Operation>
    std::int32_t Boundary(Operation operation)
    {
        try
        {
            std::lock_guard<std::mutex> lock(SessionsMutex);
            operation();
            LastError.clear();
            return VerisOk;
        }
        catch (const std::invalid_argument& exception) { LastError = exception.what(); }
        catch (const std::out_of_range& exception) { LastError = exception.what(); OutputDebugStringA(LastError.c_str()); return VerisNotFound; }
        catch (const std::exception& exception) { LastError = exception.what(); OutputDebugStringA(LastError.c_str()); return VerisFailure; }
        catch (...) { LastError = "Unknown native engine failure."; OutputDebugStringA(LastError.c_str()); return VerisFailure; }
        OutputDebugStringA(LastError.c_str());
        return VerisInvalidArgument;
    }

    Veris::Engine& FindEngine(std::uint64_t handle)
    {
        const auto session = Sessions.find(handle);
        if (session == Sessions.end()) throw std::out_of_range("Engine handle is invalid or has been released.");
        if (session->second->Owner != std::this_thread::get_id()) throw std::logic_error("Engine operations must run on the creating thread.");
        return session->second->Engine;
    }
}

int32_t veris_create(uint32_t width, uint32_t height, uint64_t* engine)
{
    if (engine) *engine = 0;
    return Boundary([&]
    {
        if (!engine) throw std::invalid_argument("Engine output pointer is required.");
        if (NextSession == (std::numeric_limits<std::uint64_t>::max)()) throw std::overflow_error("Engine handles exhausted.");
        auto session = std::make_unique<EngineSession>();
        session->Engine.Initialize({width, height});
        Sessions.emplace(NextSession, std::move(session));
        *engine = NextSession++;
    });
}

int32_t veris_destroy(uint64_t engine)
{
    return Boundary([&]
    {
        if (Sessions.erase(engine) == 0) throw std::out_of_range("Engine handle is invalid or has been released.");
    });
}

int32_t veris_resize(uint64_t engine, uint32_t width, uint32_t height)
{
    return Boundary([&] { FindEngine(engine).Resize({width, height}); });
}

int32_t veris_update(uint64_t engine)
{
    return Boundary([&] { FindEngine(engine).Update(); });
}

int32_t veris_create_entity(uint64_t engine, const char* name, uint64_t* entity)
{
    if (entity) *entity = 0;
    return Boundary([&]
    {
        if (!name || !entity) throw std::invalid_argument("Entity name and output pointer are required.");
        *entity = FindEngine(engine).ActiveScene().CreateEntity(name);
    });
}

int32_t veris_destroy_entity(uint64_t engine, uint64_t entity)
{
    return Boundary([&] { FindEngine(engine).ActiveScene().DestroyEntity(entity); });
}

int32_t veris_entity_count(uint64_t engine, uint64_t* count)
{
    if (count) *count = 0;
    return Boundary([&]
    {
        if (!count) throw std::invalid_argument("Entity count output pointer is required.");
        *count = FindEngine(engine).ActiveScene().Entities().size();
    });
}

int32_t veris_duplicate_entity(uint64_t engine, uint64_t entity, uint64_t* duplicate)
{
    return Boundary([&]
    {
        if (!duplicate) throw std::invalid_argument("Duplicate entity output pointer is required.");
        *duplicate = FindEngine(engine).ActiveScene().DuplicateEntity(entity);
    });
}

int32_t veris_rename_entity(uint64_t engine, uint64_t entity, const char* name)
{
    return Boundary([&]
    {
        if (!name) throw std::invalid_argument("Entity name is required.");
        FindEngine(engine).ActiveScene().RenameEntity(entity, name);
    });
}

int32_t veris_entity_parent(uint64_t engine, uint64_t entity, uint64_t* parent)
{
    return Boundary([&]
    {
        if (!parent) throw std::invalid_argument("Parent output pointer is required.");
        *parent = FindEngine(engine).ActiveScene().Get(entity).Parent();
    });
}

int32_t veris_set_parent(uint64_t engine, uint64_t entity, uint64_t parent)
{
    return Boundary([&] { FindEngine(engine).ActiveScene().SetParent(entity, parent); });
}

int32_t veris_set_transform(uint64_t engine, uint64_t entity, const VerisTransform* transform)
{
    return Boundary([&]
    {
        if (!transform) throw std::invalid_argument("Transform pointer is required.");
        Veris::TransformComponent component{{transform->PositionX, transform->PositionY}, {transform->ScaleX, transform->ScaleY}, transform->Rotation};
        component.Matrix();
        FindEngine(engine).ActiveScene().Get(entity).Transform = component;
    });
}

int32_t veris_entity_at(uint64_t engine, uint64_t index, uint64_t* entity)
{
    return Boundary([&]
    {
        if (!entity) throw std::invalid_argument("Entity output pointer is required.");
        const auto& entities = FindEngine(engine).ActiveScene().Entities();
        if (index >= entities.size()) throw std::out_of_range("Entity index is out of range.");
        *entity = std::next(entities.begin(), static_cast<std::ptrdiff_t>(index))->first;
    });
}

int32_t veris_entity_name(uint64_t engine, uint64_t entity, char* utf8, uint32_t capacity, uint32_t* required)
{
    bool tooSmall = false;
    const auto status = Boundary([&]
    {
        if (!required || (!utf8 && capacity != 0)) throw std::invalid_argument("Entity name buffer arguments are invalid.");
        const auto& name = FindEngine(engine).ActiveScene().Get(entity).Name;
        if (name.size() >= (std::numeric_limits<std::uint32_t>::max)()) throw std::overflow_error("Entity name exceeds the ABI size limit.");
        *required = static_cast<std::uint32_t>(name.size() + 1);
        if (!utf8 && capacity == 0) return;
        if (capacity < *required) { tooSmall = true; return; }
        std::memcpy(utf8, name.c_str(), *required);
    });
    if (status == VerisOk && tooSmall) { LastError = "Entity name buffer is too small."; return VerisBufferTooSmall; }
    return status;
}

int32_t veris_get_transform(uint64_t engine, uint64_t entity, VerisTransform* transform)
{
    return Boundary([&]
    {
        if (!transform) throw std::invalid_argument("Transform output pointer is required.");
        const auto& component = FindEngine(engine).ActiveScene().Get(entity).Transform;
        *transform = {component.Position.x, component.Position.y, component.Scale.x, component.Scale.y, component.Rotation};
    });
}

int32_t veris_copy_frame(uint64_t engine, uint8_t* rgba, uint64_t capacity, uint64_t* required)
{
    bool tooSmall = false;
    const auto status = Boundary([&]
    {
        if (!required || (!rgba && capacity != 0)) throw std::invalid_argument("Frame buffer arguments are invalid.");
        const auto image = FindEngine(engine).GetFrame().copyToImage();
        *required = static_cast<std::uint64_t>(image.getSize().x) * image.getSize().y * 4;
        if (!rgba && capacity == 0) return;
        if (capacity < *required) { tooSmall = true; return; }
        std::memcpy(rgba, image.getPixelsPtr(), static_cast<std::size_t>(*required));
    });
    if (status == VerisOk && tooSmall) { LastError = "Frame buffer is too small; use the returned required byte count."; return VerisBufferTooSmall; }
    return status;
}

uint32_t veris_error_message(char* utf8, uint32_t capacity)
{
    const auto required = static_cast<std::uint32_t>(LastError.size() + 1);
    if (utf8 && capacity >= required) std::memcpy(utf8, LastError.c_str(), required);
    return required;
}
