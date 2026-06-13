#include "pch.h"
#include "TextureResources.h"
#include <algorithm>
#include <cwctype>
#include <stdexcept>

namespace Veris
{
    void TextureResources::SetRoot(const std::filesystem::path& contentDirectory)
    {
        auto root = std::filesystem::canonical(contentDirectory);
        if (!std::filesystem::is_directory(root)) throw std::invalid_argument("Texture resource root must be a directory.");
        Clear();
        _assetPaths.clear();
        _assetIdsByPath.clear();
        _root = std::move(root);
    }

    std::shared_ptr<const sf::Texture> TextureResources::Load(const std::filesystem::path& relativePath)
    {
        const auto path = Resolve(relativePath);
        const auto cached = _textures.find(path);
        if (cached != _textures.end()) return cached->second;
        auto texture = std::make_shared<sf::Texture>();
        if (!texture->loadFromFile(path)) throw std::runtime_error("Cannot load texture: " + relativePath.u8string());
        _textures.emplace(path, texture);
        return texture;
    }

    void TextureResources::Reload(const std::filesystem::path& relativePath)
    {
        const auto path = Resolve(relativePath);
        const auto cached = _textures.find(path);
        if (cached == _textures.end()) throw std::out_of_range("Load a texture before reloading it.");
        sf::Texture replacement;
        if (!replacement.loadFromFile(path)) throw std::runtime_error("Cannot reload texture: " + relativePath.u8string());
        if (replacement.getSize() != cached->second->getSize())
            throw std::invalid_argument("Live texture reload must preserve image dimensions; reassign a resized image explicitly to validate atlas references.");
        *cached->second = std::move(replacement);
    }

    bool TextureResources::Unload(const std::filesystem::path& relativePath)
    {
        return _textures.erase(Resolve(relativePath)) != 0;
    }

    std::filesystem::path TextureResources::Resolve(const std::filesystem::path& relativePath) const
    {
        if (_root.empty()) throw std::logic_error("Configure the content directory before loading textures.");
        if (relativePath.empty() || relativePath.is_absolute() || relativePath.has_root_path())
            throw std::invalid_argument("Texture paths must be content-relative.");
        for (const auto& segment : relativePath)
            if (segment == L".." || segment == L".") throw std::invalid_argument("Texture paths cannot traverse directories.");
        const auto path = std::filesystem::weakly_canonical(_root / relativePath);
        auto rootText = _root.native() + L"\\";
        auto pathText = path.native();
        std::transform(rootText.begin(), rootText.end(), rootText.begin(), std::towlower);
        std::transform(pathText.begin(), pathText.end(), pathText.begin(), std::towlower);
        if (pathText.compare(0, rootText.size(), rootText) != 0) throw std::invalid_argument("Texture path escapes the content directory.");
        return std::filesystem::path(pathText);
    }
}
