#include "pch.h"
#include "Prefabs.h"
#include "Engine.h"
#include "SceneFormat.h"
#include "JsonValidation.h"
#include <fstream>
#include <iterator>

namespace
{
    using namespace Veris;
    std::pair<Scene, EntityId> ReadPrefab(const std::filesystem::path& relative, TextureResources& textures)
    {
        const auto path = textures.ContentPath(relative);
        if (path.extension() != L".verisprefab" || std::filesystem::file_size(path) > 16 * 1024 * 1024)
            throw std::invalid_argument("Prefabs require content-relative .verisprefab files of at most 16 MiB.");
        std::ifstream stream(path, std::ios::binary);
        if (!stream) throw std::runtime_error("Cannot read prefab: " + relative.u8string());
        const std::string json((std::istreambuf_iterator<char>(stream)), std::istreambuf_iterator<char>());
        const auto document = Json::Parse(json);
        Json::Fields(document, {"formatVersion", "root", "scene"});
        if (Json::Integer(document.at("formatVersion")) != 1 || !document.at("root").is_number_unsigned()) throw std::invalid_argument("Unsupported prefab format or invalid root.");
        auto scene = DeserializeScene(document.at("scene").dump(), textures);
        const auto root = document.at("root").get<EntityId>();
        if (scene.Get(root).Parent() != 0 || scene.Subtree(root).size() != scene.Entities().size()) throw std::invalid_argument("A prefab must contain one complete rooted subtree.");
        return {std::move(scene), root};
    }

    void CopyPrefabEntities(Scene& destination, EntityId root, const Scene& source, EntityId sourceRoot)
    {
        std::map<EntityId, EntityId> copies{{sourceRoot, root}};
        const auto subtree = source.Subtree(sourceRoot);
        for (const auto id : subtree)
        {
            if (id != sourceRoot) copies.emplace(id, destination.CreateEntity(source.Get(id).Name));
            auto& entity = destination.Get(copies.at(id));
            entity.CopyStateFrom(source.Get(id));
            entity.PrefabSource.clear();
        }
        for (const auto id : subtree)
            if (id != sourceRoot) destination.SetParent(copies.at(id), copies.at(source.Get(id).Parent()));
    }

    void ValidatePrefabCandidate(const Scene& scene)
    {
        if (scene.Entities().size() > 100000) throw std::invalid_argument("Prefab operation exceeds the scene entity limit.");
        PhysicsWorld validation;
        validation.Synchronize(scene);
    }
}

namespace Veris
{
    std::string SerializePrefab(const Scene& source, EntityId root)
    {
        Scene prefab;
        const auto copy = prefab.CreateEntity(source.Get(root).Name);
        CopyPrefabEntities(prefab, copy, source, root);
        prefab.Get(copy).Transform.Position = {0, 0};
        ValidatePrefabCandidate(prefab);
        const auto json = Json::Document{{"formatVersion", 1}, {"root", copy}, {"scene", Json::Parse(SerializeScene(prefab))}}.dump(2);
        if (json.size() > 16 * 1024 * 1024) throw std::invalid_argument("Prefab documents cannot exceed 16 MiB.");
        return json;
    }

    EntityId Engine::InstantiatePrefab(const std::filesystem::path& path, sf::Vector2f position)
    {
        TransformComponent placement; placement.Position = position; placement.Matrix();
        auto [source, sourceRoot] = ReadPrefab(path, _textures);
        if (_scene.Entities().size() > 100000 - source.Entities().size()) throw std::invalid_argument("Prefab operation exceeds the scene entity limit.");
        Scene candidate = _scene;
        const auto root = candidate.CreateEntity(source.Get(sourceRoot).Name);
        CopyPrefabEntities(candidate, root, source, sourceRoot);
        candidate.Get(root).Transform.Position = position;
        candidate.Get(root).PrefabSource = path.generic_u8string();
        ValidatePrefabCandidate(candidate);
        _scene.Replace(std::move(candidate));
        return root;
    }

    void Engine::ReapplyPrefab(EntityId root, const std::filesystem::path& path)
    {
        if (_scene.Get(root).PrefabSource.empty()) throw std::invalid_argument("Select a linked prefab root before reapplying its source.");
        const auto placement = _scene.Get(root).Transform;
        const auto parent = _scene.Get(root).Parent();
        auto [source, sourceRoot] = ReadPrefab(path, _textures);
        Scene candidate = _scene;
        candidate.DestroyEntity(root);
        candidate.ImportEntity(root, source.Get(sourceRoot).Name);
        CopyPrefabEntities(candidate, root, source, sourceRoot);
        candidate.Get(root).Transform = placement;
        candidate.Get(root).PrefabSource = path.generic_u8string();
        candidate.SetParent(root, parent);
        ValidatePrefabCandidate(candidate);
        _scene.Replace(std::move(candidate));
        _physics.Clear(); _audio.Clear(); _scripts.Clear();
    }
}
