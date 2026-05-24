#include "pch.h"
#include "TextureResources.h"
#include "JsonValidation.h"
#include <algorithm>
#include <cctype>
#include <fstream>
#include <iterator>

namespace Veris
{
    std::string TextureResources::NormalizeAssetId(std::string id)
    {
        if (id.size() != 36) throw std::invalid_argument("Asset identifiers must be nonempty GUIDs.");
        bool nonzero = false;
        for (std::size_t index = 0; index < id.size(); ++index)
        {
            const bool separator = index == 8 || index == 13 || index == 18 || index == 23;
            if (separator ? id[index] != '-' : !std::isxdigit(static_cast<unsigned char>(id[index]))) throw std::invalid_argument("Asset identifiers must be GUIDs.");
            if (!separator && id[index] != '0') nonzero = true;
            id[index] = static_cast<char>(std::tolower(static_cast<unsigned char>(id[index])));
        }
        if (!nonzero) throw std::invalid_argument("Asset identifiers cannot be empty GUIDs.");
        return id;
    }

    const std::filesystem::path& TextureResources::AssetPath(const std::string& id) const
    {
        const auto asset = _assetPaths.find(NormalizeAssetId(id));
        if (asset == _assetPaths.end()) throw std::invalid_argument("Asset identifier is not registered in this project: " + id);
        return asset->second;
    }

    std::string TextureResources::AssetId(const std::filesystem::path& path) const
    {
        const auto asset = _assetIdsByPath.find(Resolve(path));
        return asset == _assetIdsByPath.end() ? "" : asset->second;
    }

    void TextureResources::SetCatalog(const std::filesystem::path& manifestPath)
    {
        std::map<std::string, std::filesystem::path> paths;
        std::map<std::filesystem::path, std::string> identities;
        if (std::filesystem::exists(manifestPath))
        {
            if (std::filesystem::file_size(manifestPath) > 16 * 1024 * 1024) throw std::invalid_argument("Asset manifest cannot exceed 16 MiB.");
            std::ifstream stream(manifestPath, std::ios::binary);
            if (!stream) throw std::runtime_error("Cannot read the asset manifest.");
            const std::string text((std::istreambuf_iterator<char>(stream)), std::istreambuf_iterator<char>());
            const auto manifest = Json::Parse(text);
            Json::Fields(manifest, {"formatVersion", "assets"});
            if (Json::Integer(manifest.at("formatVersion")) != 1 || !manifest.at("assets").is_array()) throw std::invalid_argument("Unsupported asset manifest format.");
            for (const auto& asset : manifest.at("assets"))
            {
                Json::Fields(asset, {"id", "path", "kind", "sha256"});
                const auto id = NormalizeAssetId(asset.at("id").get<std::string>());
                const auto path = std::filesystem::u8path(asset.at("path").get<std::string>());
                const auto hash = asset.at("sha256").get<std::string>();
                if (asset.at("kind") != "texture" || hash.size() != 64 || !std::all_of(hash.begin(), hash.end(), [](unsigned char digit) { return std::isxdigit(digit); }))
                    throw std::invalid_argument("Asset entries require a supported kind and SHA-256 digest.");
                if (!paths.emplace(id, path).second || !identities.emplace(Resolve(path), id).second) throw std::invalid_argument("Asset identifiers and paths must be unique.");
            }
        }
        MigrateAssetCache(paths);
        _assetPaths.swap(paths);
        _assetIdsByPath.swap(identities);
    }

    void TextureResources::MigrateAssetCache(const std::map<std::string, std::filesystem::path>& paths)
    {
        for (const auto& asset : _assetPaths)
        {
            const auto oldPath = Resolve(asset.second);
            const auto cached = _textures.find(oldPath);
            if (cached == _textures.end()) continue;
            const auto replacement = paths.find(asset.first);
            if (replacement == paths.end()) { _textures.erase(cached); continue; }
            const auto newPath = Resolve(replacement->second);
            if (newPath == oldPath) continue;
            const auto texture = cached->second;
            _textures.erase(cached);
            _textures[newPath] = texture;
        }
    }
}
