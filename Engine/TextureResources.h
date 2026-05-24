#pragma once

#include <SFML/Graphics/Texture.hpp>
#include <filesystem>
#include <map>
#include <memory>

namespace Veris
{
    class TextureResources
    {
    public:
        void SetRoot(const std::filesystem::path& contentDirectory);
        void SetCatalog(const std::filesystem::path& manifestPath);
        const std::filesystem::path& AssetPath(const std::string& id) const;
        std::string AssetId(const std::filesystem::path& path) const;
        static std::string NormalizeAssetId(std::string id);
        std::shared_ptr<const sf::Texture> Load(const std::filesystem::path& relativePath);
        void Reload(const std::filesystem::path& relativePath);
        bool Unload(const std::filesystem::path& relativePath);
        void Clear() noexcept { _textures.clear(); }
        void Reset() noexcept { Clear(); _root.clear(); _assetPaths.clear(); _assetIdsByPath.clear(); }
        std::size_t Count() const noexcept { return _textures.size(); }

    private:
        std::filesystem::path Resolve(const std::filesystem::path& relativePath) const;
        void MigrateAssetCache(const std::map<std::string, std::filesystem::path>& paths);
        std::filesystem::path _root;
        std::map<std::filesystem::path, std::shared_ptr<sf::Texture>> _textures;
        std::map<std::string, std::filesystem::path> _assetPaths;
        std::map<std::filesystem::path, std::string> _assetIdsByPath;
    };
}
