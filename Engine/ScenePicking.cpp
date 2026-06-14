#include "pch.h"
#include "Engine.h"
#include <cmath>
#include <limits>
#include <stdexcept>

namespace
{
    bool ContainsPoint(const std::array<sf::Vector2f, 4>& corners, sf::Vector2f point)
    {
        bool positive = false;
        bool negative = false;
        float area = 0;
        for (std::size_t index = 0; index < corners.size(); ++index)
        {
            const auto edge = corners[(index + 1) % corners.size()] - corners[index];
            const auto cross = edge.cross(point - corners[index]);
            positive = positive || cross > 0;
            negative = negative || cross < 0;
            area += corners[index].cross(corners[(index + 1) % corners.size()]);
        }
        return std::abs(area) > 0.001f && !(positive && negative);
    }
}

namespace Veris
{
    std::array<sf::Vector2f, 4> Engine::EntityCorners(EntityId id) const
    {
        const auto& entity = _scene.Get(id);
        const auto world = _scene.WorldTransform(id);
        const auto center = sf::Vector2f(WorldToPixel(world.transformPoint({0, 0})));
        if (!entity.Sprite) return {center + sf::Vector2f{-8, -8}, center + sf::Vector2f{8, -8}, center + sf::Vector2f{8, 8}, center + sf::Vector2f{-8, 8}};
        const auto& sprite = *entity.Sprite;
        if (!sprite.Texture) throw std::invalid_argument("Sprite has no texture.");
        const auto size = entity.Animation ? sf::Vector2f(entity.Animation->Frames.at(entity.Animation->Frame).Rectangle.size) :
            sprite.TextureRectangle ? sf::Vector2f(sprite.TextureRectangle->size) : sf::Vector2f(sprite.Texture->getSize());
        const sf::Vector2f extent{std::abs(size.x), std::abs(size.y)};
        std::array<sf::Vector2f, 4> corners{sf::Vector2f{0, 0}, sf::Vector2f{extent.x, 0}, extent, sf::Vector2f{0, extent.y}};
        for (auto& corner : corners) corner = sf::Vector2f(WorldToPixel(world.transformPoint(corner - sprite.Origin)));
        return corners;
    }

    EntityId Engine::PickEntity(sf::Vector2i pixel, float tolerance) const
    {
        if (!IsInitialized()) throw std::logic_error("Initialize the engine before picking entities.");
        if (!std::isfinite(tolerance) || tolerance < 0) throw std::invalid_argument("Pick tolerance must be finite and nonnegative.");
        EntityId picked = 0;
        int highestLayer = std::numeric_limits<int>::min();
        for (const auto& entry : _scene.Entities())
        {
            const auto& entity = entry.second;
            if (entity.Sprite && !entity.Sprite->Visible) continue;
            const auto layer = entity.Sprite ? entity.Sprite->Layer : 0;
            if (layer < highestLayer) continue;
            const auto center = WorldToPixel(_scene.WorldTransform(entity.Id).transformPoint({0, 0}));
            const bool hit = entity.Sprite ? ContainsPoint(EntityCorners(entity.Id), sf::Vector2f(pixel)) :
                (sf::Vector2f(pixel - center)).length() <= tolerance;
            if (hit) { picked = entity.Id; highestLayer = layer; }
        }
        return picked;
    }
}
