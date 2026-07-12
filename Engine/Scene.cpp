#include "pch.h"
#include "Scene.h"
#include "Engine.h"
#include <cmath>
#include <limits>
#include <stdexcept>
#include <vector>

namespace
{
    void ValidateEntityName(const std::string& name)
    {
        if (name.find_first_not_of(" \t\r\n") == std::string::npos || name.find('\0') != std::string::npos)
            throw std::invalid_argument("Entity names must contain visible text and no null characters.");
    }
}

namespace Veris
{
    EntityId Scene::CreateEntity(std::string name)
    {
        return ImportEntity(_nextId, std::move(name));
    }

    EntityId Scene::ImportEntity(EntityId id, std::string name)
    {
        ValidateEntityName(name);
        if (id == 0 || id == std::numeric_limits<EntityId>::max() || Contains(id)) throw std::invalid_argument("Entity IDs must be unique, nonzero, and below the maximum integer.");
        _entities.emplace(id, Entity(id, std::move(name)));
        _nextId = std::max(_nextId, id + 1);
        return id;
    }

    void Scene::DestroyEntity(EntityId id)
    {
        for (const auto descendant : Subtree(id)) _entities.erase(descendant);
    }

    std::vector<EntityId> Scene::Subtree(EntityId id) const
    {
        Get(id);
        std::vector<EntityId> subtree{id};
        for (std::size_t index = 0; index < subtree.size(); ++index)
            for (const auto& entry : _entities)
                if (entry.second.Parent() == subtree[index]) subtree.push_back(entry.first);
        return subtree;
    }

    EntityId Scene::DuplicateEntity(EntityId id)
    {
        const auto subtree = Subtree(id);
        std::map<EntityId, EntityId> copies;
        EntityId root = 0;
        try
        {
            for (const auto sourceId : subtree)
            {
                const auto& source = Get(sourceId);
                const auto copyId = CreateEntity(source.Name + (sourceId == id ? " Copy" : ""));
                if (sourceId == id) root = copyId;
                auto& copy = Get(copyId);
                copy._parent = sourceId == id ? source.Parent() : copies.at(source.Parent());
                copy.Transform = source.Transform;
                copy.Sprite = source.Sprite;
                copy.Animation = source.Animation;
                copy.Audio = source.Audio;
                copy.Physics = source.Physics;
                copy.Script = source.Script;
                copies.emplace(sourceId, copyId);
            }
        }
        catch (...) { if (root != 0) DestroyEntity(root); throw; }
        return root;
    }

    void Scene::RenameEntity(EntityId id, std::string name)
    {
        ValidateEntityName(name);
        Get(id).Name = std::move(name);
    }

    void Scene::SetParent(EntityId child, EntityId parent)
    {
        auto& entity = Get(child);
        if (entity.Physics && parent != 0) throw std::invalid_argument("Physics bodies must remain root entities.");
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
            if (entity.Animation)
            {
                entity.Animation->Validate(component.Texture->getSize());
                sprite.setTextureRect(entity.Animation->Frames.at(entity.Animation->Frame).Rectangle);
            }
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

    void Scene::AdvanceAnimations(double seconds)
    {
        for (auto& entry : _entities)
            if (entry.second.Animation) entry.second.Animation->Advance(seconds);
    }
}
