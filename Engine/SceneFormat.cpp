#include "pch.h"
#include "SceneFormat.h"
#include "JsonValidation.h"

namespace
{
    using Veris::Json::Document;
    Document WriteSprite(const Veris::SpriteComponent& sprite);
    Veris::SpriteComponent ReadSprite(const Document& sprite, Veris::TextureResources& textures);
    sf::Vector2f ReadVector(const Document& vector);
}

namespace Veris
{
    std::string SerializeScene(const Scene& scene)
    {
        Document entities = Document::array();
        for (const auto& entry : scene.Entities())
        {
            const auto& entity = entry.second;
            entity.Transform.Matrix();
            entities.push_back({{"id", entity.Id}, {"name", entity.Name}, {"parent", entity.Parent()},
                {"transform", {{"position", {entity.Transform.Position.x, entity.Transform.Position.y}},
                    {"scale", {entity.Transform.Scale.x, entity.Transform.Scale.y}}, {"rotation", entity.Transform.Rotation}}},
                {"sprite", entity.Sprite ? WriteSprite(*entity.Sprite) : Document(nullptr)}});
        }
        return Document{{"formatVersion", 1}, {"entities", entities}}.dump(2);
    }

    Scene DeserializeScene(std::string_view json, TextureResources& textures)
    {
        if (json.size() > 16 * 1024 * 1024) throw std::invalid_argument("Scene documents cannot exceed 16 MiB.");
        const auto document = Json::Parse(json);
        Json::Fields(document, {"formatVersion", "entities"});
        if (Json::Integer(document.at("formatVersion")) != 1) throw std::invalid_argument("Unsupported scene format version.");
        const auto& entities = document.at("entities");
        if (!entities.is_array() || entities.size() > 100000) throw std::invalid_argument("Scene entities must be an array of at most 100000 entries.");
        Scene scene;
        for (const auto& entity : entities)
        {
            Json::Fields(entity, {"id", "name", "parent", "transform", "sprite"});
            if (!entity.at("id").is_number_unsigned() || !entity.at("parent").is_number_unsigned()) throw std::invalid_argument("Scene identifiers must be unsigned integers.");
            const auto id = scene.ImportEntity(entity.at("id").get<EntityId>(), entity.at("name").get<std::string>());
            const auto& transform = entity.at("transform");
            Json::Fields(transform, {"position", "scale", "rotation"});
            scene.Get(id).Transform = {ReadVector(transform.at("position")), ReadVector(transform.at("scale")), Json::Number(transform.at("rotation"))};
            if (!entity.at("sprite").is_null()) scene.Get(id).Sprite = ReadSprite(entity.at("sprite"), textures);
        }
        for (const auto& entity : entities) scene.SetParent(entity.at("id").get<EntityId>(), entity.at("parent").get<EntityId>());
        return scene;
    }
}

namespace
{
    Document WriteSprite(const Veris::SpriteComponent& sprite)
    {
        if (!sprite.Texture || sprite.TexturePath.empty()) throw std::invalid_argument("Saved sprites require a content-relative texture path.");
        Document rectangle = nullptr;
        if (sprite.TextureRectangle) rectangle = {sprite.TextureRectangle->position.x, sprite.TextureRectangle->position.y, sprite.TextureRectangle->size.x, sprite.TextureRectangle->size.y};
        return {{"texture", sprite.TexturePath}, {"assetId", sprite.TextureAssetId}, {"origin", {sprite.Origin.x, sprite.Origin.y}}, {"tint", sprite.Tint.toInteger()},
            {"layer", sprite.Layer}, {"visible", sprite.Visible}, {"rectangle", rectangle}};
    }

    Veris::SpriteComponent ReadSprite(const Document& sprite, Veris::TextureResources& textures)
    {
        Veris::Json::Fields(sprite, {"texture", "origin", "tint", "layer", "visible", "rectangle"}, {"assetId"});
        Veris::SpriteComponent component;
        component.TexturePath = sprite.at("texture").get<std::string>();
        if (sprite.contains("assetId")) component.TextureAssetId = sprite.at("assetId").get<std::string>();
        if (!component.TextureAssetId.empty())
        {
            component.TextureAssetId = Veris::TextureResources::NormalizeAssetId(component.TextureAssetId);
            component.TexturePath = textures.AssetPath(component.TextureAssetId).u8string();
        }
        component.Texture = textures.Load(std::filesystem::u8path(component.TexturePath));
        component.Origin = ReadVector(sprite.at("origin"));
        const auto& tint = sprite.at("tint");
        if (!tint.is_number_unsigned() || tint.get<std::uint64_t>() > 0xffffffff) throw std::invalid_argument("Sprite tint must be an unsigned RGBA32 integer.");
        component.Tint = sf::Color(tint.get<std::uint32_t>());
        component.Layer = Veris::Json::Integer(sprite.at("layer"));
        component.Visible = sprite.at("visible").get<bool>();
        const auto& rectangle = sprite.at("rectangle");
        if (rectangle.is_null()) return component;
        if (!rectangle.is_array() || rectangle.size() != 4) throw std::invalid_argument("Sprite rectangle requires four integers.");
        const auto x = Veris::Json::Integer(rectangle[0]); const auto y = Veris::Json::Integer(rectangle[1]);
        const auto width = Veris::Json::Integer(rectangle[2]); const auto height = Veris::Json::Integer(rectangle[3]);
        const auto size = component.Texture->getSize();
        if (x < 0 || y < 0 || width <= 0 || height <= 0 || static_cast<std::uint64_t>(x) + width > size.x || static_cast<std::uint64_t>(y) + height > size.y)
            throw std::invalid_argument("Sprite rectangle must fit within its texture.");
        component.TextureRectangle = sf::IntRect({x, y}, {width, height});
        return component;
    }

    sf::Vector2f ReadVector(const Document& vector)
    {
        if (!vector.is_array() || vector.size() != 2) throw std::invalid_argument("Scene vectors require exactly two numbers.");
        return {Veris::Json::Number(vector[0]), Veris::Json::Number(vector[1])};
    }
}
