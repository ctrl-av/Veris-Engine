#include "pch.h"
#include "Scene.h"
#include "Engine.h"
#include <cmath>
#include <limits>
#include <stdexcept>
#include <vector>

namespace Veris
{
    EntityId Scene::CreateEntity(std::string name)
    {
        if (name.empty()) throw std::invalid_argument("Entity names cannot be empty.");
        if (_nextId == std::numeric_limits<EntityId>::max()) throw std::overflow_error("Entity identifiers exhausted.");
        const auto id = _nextId;
        _entities.emplace(id, Entity(id, std::move(name)));
        ++_nextId;
        return id;
    }

    void Scene::DestroyEntity(EntityId id)
    {
        Get(id);
        std::vector<EntityId> subtree{id};
        for (std::size_t index = 0; index < subtree.size(); ++index)
            for (const auto& entry : _entities)
                if (entry.second.Parent() == subtree[index]) subtree.push_back(entry.first);
        for (const auto descendant : subtree) _entities.erase(descendant);
    }

    void Scene::SetParent(EntityId child, EntityId parent)
    {
        auto& entity = Get(child);
        for (auto ancestor = parent; ancestor != 0; ancestor = Get(ancestor).Parent())
            if (ancestor == child) throw std::invalid_argument("Entity parenting cannot create cycles.");
        entity._parent = parent;
    }

    sf::Transform Scene::WorldTransform(EntityId id) const
    {
        sf::Transform world;
        for (auto ancestor = id; ancestor != 0; ancestor = Get(ancestor).Parent())
            world = Get(ancestor).Transform.Matrix() * world;
        return world;
    }

    void Scene::Submit(Engine& engine) const
    {
        for (const auto& entry : _entities)
        {
            const auto& entity = entry.second;
            if (!entity.Sprite || !entity.Sprite->Visible) continue;
            const auto& component = *entity.Sprite;
            if (!component.Texture) throw std::invalid_argument("Sprite entity requires a texture: " + entity.Name);
            sf::Sprite sprite(*component.Texture);
            if (component.TextureRectangle) sprite.setTextureRect(*component.TextureRectangle);
            sprite.setOrigin(component.Origin);
            sprite.setColor(component.Tint);
            engine.SubmitSprite(sprite, component.Layer, WorldTransform(entity.Id));
        }
    }

    sf::Transform TransformComponent::Matrix() const
    {
        if (!std::isfinite(Position.x) || !std::isfinite(Position.y) || !std::isfinite(Scale.x) ||
            !std::isfinite(Scale.y) || !std::isfinite(Rotation)) throw std::invalid_argument("Entity transforms must be finite.");
        sf::Transform transform;
        transform.translate(Position).rotate(sf::degrees(Rotation)).scale(Scale);
        return transform;
    }
}
