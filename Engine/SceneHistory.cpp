#include "pch.h"
#include "Scene.h"
#include <algorithm>

namespace
{
    bool SameSprite(const std::optional<Veris::SpriteComponent>& left, const std::optional<Veris::SpriteComponent>& right)
    {
        if (left.has_value() != right.has_value()) return false;
        if (!left) return true;
        const bool sameTexture = left->TextureAssetId.empty() && right->TextureAssetId.empty() ? left->TexturePath == right->TexturePath : left->TextureAssetId == right->TextureAssetId;
        return sameTexture && left->Texture == right->Texture && left->TextureRectangle == right->TextureRectangle &&
            left->Origin == right->Origin && left->Tint == right->Tint && left->Layer == right->Layer && left->Visible == right->Visible;
    }
}

namespace Veris
{
    void Scene::Replace(Scene replacement)
    {
        _entities.swap(replacement._entities);
        _nextId = std::max(_nextId, replacement._nextId);
    }

    bool Scene::Matches(const Scene& other) const
    {
        if (_entities.size() != other._entities.size()) return false;
        for (const auto& entry : _entities)
        {
            const auto found = other._entities.find(entry.first);
            if (found == other._entities.end()) return false;
            const auto& left = entry.second;
            const auto& right = found->second;
            if (left.Animation.has_value() != right.Animation.has_value()) return false;
            if (left.Animation && SerializeAnimation(*left.Animation) != SerializeAnimation(*right.Animation)) return false;
            if (left.Audio.has_value() != right.Audio.has_value()) return false;
            if (left.Audio && SerializeAudio(*left.Audio) != SerializeAudio(*right.Audio)) return false;
            if (left.Physics.has_value() != right.Physics.has_value()) return false;
            if (left.Physics && SerializePhysics(*left.Physics) != SerializePhysics(*right.Physics)) return false;
            if (left.Name != right.Name || left.Parent() != right.Parent() || left.Transform.Position != right.Transform.Position ||
                left.Transform.Scale != right.Transform.Scale || left.Transform.Rotation != right.Transform.Rotation || !SameSprite(left.Sprite, right.Sprite)) return false;
        }
        return true;
    }
}
