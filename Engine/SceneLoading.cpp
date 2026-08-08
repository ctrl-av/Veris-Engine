#include "pch.h"
#include "Engine.h"
#include "SceneFormat.h"
#include <fstream>
#include <iterator>

namespace Veris
{
    Scene Engine::ReadSceneFile(const std::filesystem::path& relativePath)
    {
        const auto path = _textures.ContentPath(relativePath);
        if (path.extension() != L".verisscene") throw std::invalid_argument("Scenes require content-relative .verisscene paths.");
        if (std::filesystem::file_size(path) > 16 * 1024 * 1024) throw std::invalid_argument("Scene documents cannot exceed 16 MiB.");
        std::ifstream stream(path, std::ios::binary);
        if (!stream) throw std::runtime_error("Cannot read scene: " + relativePath.u8string());
        const std::string json((std::istreambuf_iterator<char>(stream)), std::istreambuf_iterator<char>());
        return DeserializeScene(json, _textures);
    }

    void Engine::LoadSceneFile(const std::filesystem::path& relativePath)
    {
        if (!IsInitialized()) throw std::logic_error("Initialize the engine before loading scenes.");
        ReplaceLoadedScene(ReadSceneFile(relativePath), relativePath.generic_u8string());
    }

    void Engine::RequestScene(const std::filesystem::path& relativePath)
    {
        if (_pendingScene) throw std::logic_error("A scene transition is already queued for this fixed frame.");
        auto candidate = std::make_unique<Scene>(ReadSceneFile(relativePath));
        _pendingScenePath = relativePath.generic_u8string();
        _pendingScene = std::move(candidate);
    }

    void Engine::CommitSceneTransition()
    {
        if (!_pendingScene) return;
        auto scene = std::move(_pendingScene);
        const auto path = _pendingScenePath;
        ReplaceLoadedScene(std::move(*scene), path);
    }

    void Engine::ReplaceLoadedScene(Scene scene, std::string path)
    {
        _physics.Clear(); _audio.Clear(); _scripts.Clear(); _sprites.clear();
        _scene.Replace(std::move(scene));
        _camera = Camera{}; _camera.Resize(_renderTarget->getSize());
        _currentScene = std::move(path);
        _pendingScene.reset(); _pendingScenePath.clear();
    }
}
