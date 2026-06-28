#pragma once

#include <SFML/Graphics/Sprite.hpp>
#include <SFML/Graphics/Transform.hpp>
#include <cstdint>
#include <map>
#include <memory>
#include <optional>
#include <string>
#include <utility>
#include <vector>
#include "Animation.h"
#include "Audio.h"

namespace Veris
{
    class Engine;
    using EntityId = std::uint64_t;

    struct TransformComponent
    {
        sf::Vector2f Position{};
        sf::Vector2f Scale{1, 1};
        float Rotation = 0;
        sf::Transform Matrix() const;
    };

    struct SpriteComponent
    {
        std::string TexturePath;
        std::string TextureAssetId;
        std::shared_ptr<const sf::Texture> Texture;
        std::optional<sf::IntRect> TextureRectangle;
        sf::Vector2f Origin{};
        sf::Color Tint = sf::Color::White;
        int Layer = 0;
        bool Visible = true;
    };

    struct Entity
    {
        const EntityId Id;
        std::string Name;
        TransformComponent Transform;
        std::optional<SpriteComponent> Sprite;
        std::optional<SpriteAnimation> Animation;
        std::optional<AudioSource> Audio;
        Entity(EntityId id, std::string name) : Id(id), Name(std::move(name)) {}
        EntityId Parent() const noexcept { return _parent; }

    private:
        friend class Scene;
        EntityId _parent = 0;
    };

    class Scene
    {
    public:
        EntityId CreateEntity(std::string name);
        EntityId ImportEntity(EntityId id, std::string name);
        void DestroyEntity(EntityId id);
        EntityId DuplicateEntity(EntityId id);
        void RenameEntity(EntityId id, std::string name);
        void SetParent(EntityId child, EntityId parent);
        Entity& Get(EntityId id) { return _entities.at(id); }
        const Entity& Get(EntityId id) const { return _entities.at(id); }
        bool Contains(EntityId id) const { return _entities.find(id) != _entities.end(); }
        const std::map<EntityId, Entity>& Entities() const noexcept { return _entities; }
        sf::Transform WorldTransform(EntityId id) const;
        void Submit(Engine& engine) const;
        void AdvanceAnimations(double seconds);
        void Clear() { _entities.clear(); }
        void Replace(Scene replacement);
        bool Matches(const Scene& other) const;

    private:
        std::vector<EntityId> Subtree(EntityId id) const;
        EntityId _nextId = 1;
        std::map<EntityId, Entity> _entities;
    };
}
