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
#include <cmath>

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
    std::map<std::uint64_t, std::pair<std::uint64_t, std::unique_ptr<Veris::Scene>>> Snapshots;
    std::uint64_t NextSnapshot = 1;
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

    const Veris::Scene& FindSnapshot(std::uint64_t engine, std::uint64_t snapshot)
    {
        const auto found = Snapshots.find(snapshot);
        if (found == Snapshots.end() || found->second.first != engine) throw std::invalid_argument("Snapshot does not belong to this active engine.");
        return *found->second.second;
    }

    bool CopyText(const std::string& text, char* utf8, std::uint32_t capacity, std::uint32_t* required)
    {
        if (!required || (!utf8 && capacity != 0)) throw std::invalid_argument("UTF-8 buffer arguments are invalid.");
        if (text.size() >= (std::numeric_limits<std::uint32_t>::max)()) throw std::overflow_error("Text exceeds the ABI size limit.");
        *required = static_cast<std::uint32_t>(text.size() + 1);
        if (!utf8 && capacity == 0) return true;
        if (capacity < *required) return false;
        std::memcpy(utf8, text.c_str(), *required);
        return true;
    }

    void ValidateSprite(const VerisSprite& sprite, sf::Vector2u size)
    {
        if (!std::isfinite(sprite.OriginX) || !std::isfinite(sprite.OriginY) ||
            (sprite.Visible != 0 && sprite.Visible != 1) || (sprite.UseRectangle != 0 && sprite.UseRectangle != 1))
            throw std::invalid_argument("Sprite origins must be finite and flags must be zero or one.");
        if (sprite.UseRectangle && (sprite.RectangleX < 0 || sprite.RectangleY < 0 || sprite.RectangleWidth <= 0 || sprite.RectangleHeight <= 0 ||
            static_cast<std::uint64_t>(sprite.RectangleX) + sprite.RectangleWidth > size.x ||
            static_cast<std::uint64_t>(sprite.RectangleY) + sprite.RectangleHeight > size.y))
            throw std::invalid_argument("Sprite rectangle must fit within the loaded texture.");
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
        for (auto snapshot = Snapshots.begin(); snapshot != Snapshots.end();)
            if (snapshot->second.first == engine) snapshot = Snapshots.erase(snapshot); else ++snapshot;
    });
}

int32_t veris_capture_scene(uint64_t engine, uint64_t* snapshot)
{
    return Boundary([&]
    {
        if (!snapshot) throw std::invalid_argument("Snapshot output pointer is required.");
        if (NextSnapshot == (std::numeric_limits<std::uint64_t>::max)()) throw std::overflow_error("Snapshot identifiers exhausted.");
        auto copy = std::make_unique<Veris::Scene>(FindEngine(engine).ActiveScene());
        Snapshots.emplace(NextSnapshot, std::make_pair(engine, std::move(copy)));
        *snapshot = NextSnapshot++;
    });
}

int32_t veris_restore_scene(uint64_t engine, uint64_t snapshot)
{
    return Boundary([&] { FindEngine(engine).ActiveScene().Replace(FindSnapshot(engine, snapshot)); });
}

int32_t veris_matches_scene(uint64_t engine, uint64_t snapshot, int32_t* matches)
{
    return Boundary([&]
    {
        if (!matches) throw std::invalid_argument("Scene comparison output pointer is required.");
        *matches = FindEngine(engine).ActiveScene().Matches(FindSnapshot(engine, snapshot)) ? 1 : 0;
    });
}

int32_t veris_release_snapshot(uint64_t snapshot)
{
    return Boundary([&] { Snapshots.erase(snapshot); });
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

int32_t veris_pick_entity(uint64_t engine, int32_t x, int32_t y, uint64_t* entity)
{
    return Boundary([&]
    {
        if (!entity) throw std::invalid_argument("Pick output pointer is required.");
        *entity = FindEngine(engine).PickEntity({x, y});
    });
}

int32_t veris_entity_corners(uint64_t engine, uint64_t entity, VerisPoint* corners, uint32_t count)
{
    return Boundary([&]
    {
        if (!corners || count < 4) throw std::invalid_argument("Entity corners require space for four points.");
        const auto geometry = FindEngine(engine).EntityCorners(entity);
        for (std::size_t index = 0; index < geometry.size(); ++index) corners[index] = {geometry[index].x, geometry[index].y};
    });
}

int32_t veris_parent_coordinates(uint64_t engine, uint64_t entity, int32_t x, int32_t y, VerisPoint* local)
{
    return Boundary([&]
    {
        if (!local) throw std::invalid_argument("Coordinate output pointer is required.");
        auto& session = FindEngine(engine);
        auto point = session.PixelToWorld({x, y});
        const auto parent = session.ActiveScene().Get(entity).Parent();
        if (parent != 0)
        {
            const auto transform = session.ActiveScene().WorldTransform(parent);
            const auto matrix = transform.getMatrix();
            const auto determinant = matrix[0] * matrix[5] - matrix[1] * matrix[4];
            if (!std::isfinite(determinant) || std::abs(determinant) < 0.000001f) throw std::invalid_argument("Cannot manipulate an entity under a collapsed parent transform.");
            point = transform.getInverse().transformPoint(point);
        }
        if (!std::isfinite(point.x) || !std::isfinite(point.y)) throw std::invalid_argument("Pointer coordinates exceed the transform range.");
        *local = {point.x, point.y};
    });
}

int32_t veris_entity_origin(uint64_t engine, uint64_t entity, VerisPoint* pixel)
{
    return Boundary([&]
    {
        if (!pixel) throw std::invalid_argument("Origin output pointer is required.");
        auto& session = FindEngine(engine);
        const auto world = session.ActiveScene().WorldTransform(entity).transformPoint({0, 0});
        const auto origin = session.WorldToPixel(world);
        *pixel = {static_cast<float>(origin.x), static_cast<float>(origin.y)};
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
        const auto& name = FindEngine(engine).ActiveScene().Get(entity).Name;
        tooSmall = !CopyText(name, utf8, capacity, required);
    });
    if (status == VerisOk && tooSmall) { LastError = "Entity name buffer is too small."; return VerisBufferTooSmall; }
    return status;
}

int32_t veris_set_content_root(uint64_t engine, const wchar_t* directory)
{
    return Boundary([&]
    {
        if (!directory) throw std::invalid_argument("Content directory is required.");
        FindEngine(engine).Textures().SetRoot(directory);
    });
}

int32_t veris_set_sprite(uint64_t engine, uint64_t entity, const char* texture, const VerisSprite* sprite)
{
    return Boundary([&]
    {
        if (!texture || !sprite) throw std::invalid_argument("Texture path and sprite properties are required.");
        auto& session = FindEngine(engine);
        auto& target = session.ActiveScene().Get(entity);
        Veris::SpriteComponent component;
        component.TexturePath = texture;
        component.Texture = session.Textures().Load(std::filesystem::u8path(texture));
        ValidateSprite(*sprite, component.Texture->getSize());
        component.Origin = {sprite->OriginX, sprite->OriginY};
        component.Tint = sf::Color(sprite->Tint);
        component.Layer = sprite->Layer;
        component.Visible = sprite->Visible != 0;
        if (sprite->UseRectangle) component.TextureRectangle = sf::IntRect({sprite->RectangleX, sprite->RectangleY}, {sprite->RectangleWidth, sprite->RectangleHeight});
        target.Sprite = std::move(component);
    });
}

int32_t veris_get_sprite(uint64_t engine, uint64_t entity, VerisSprite* sprite, int32_t* present)
{
    return Boundary([&]
    {
        if (!sprite || !present) throw std::invalid_argument("Sprite output pointers are required.");
        const auto& component = FindEngine(engine).ActiveScene().Get(entity).Sprite;
        *sprite = {};
        *present = component ? 1 : 0;
        if (!component) return;
        sprite->OriginX = component->Origin.x;
        sprite->OriginY = component->Origin.y;
        sprite->Tint = component->Tint.toInteger();
        sprite->Layer = component->Layer;
        sprite->Visible = component->Visible ? 1 : 0;
        sprite->UseRectangle = component->TextureRectangle ? 1 : 0;
        if (!component->TextureRectangle) return;
        sprite->RectangleX = component->TextureRectangle->position.x;
        sprite->RectangleY = component->TextureRectangle->position.y;
        sprite->RectangleWidth = component->TextureRectangle->size.x;
        sprite->RectangleHeight = component->TextureRectangle->size.y;
    });
}

int32_t veris_sprite_path(uint64_t engine, uint64_t entity, char* utf8, uint32_t capacity, uint32_t* required)
{
    bool tooSmall = false;
    const auto status = Boundary([&]
    {
        const auto& component = FindEngine(engine).ActiveScene().Get(entity).Sprite;
        if (!component) throw std::out_of_range("Entity has no sprite component.");
        tooSmall = !CopyText(component->TexturePath, utf8, capacity, required);
    });
    if (status == VerisOk && tooSmall) { LastError = "Sprite path buffer is too small."; return VerisBufferTooSmall; }
    return status;
}

int32_t veris_remove_sprite(uint64_t engine, uint64_t entity)
{
    return Boundary([&] { FindEngine(engine).ActiveScene().Get(entity).Sprite.reset(); });
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
