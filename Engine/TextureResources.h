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
        std::shared_ptr<const sf::Texture> Load(const std::filesystem::path& relativePath);
        void Reload(const std::filesystem::path& relativePath);
        bool Unload(const std::filesystem::path& relativePath);
        void Clear() noexcept { _textures.clear(); }
        void Reset() noexcept { Clear(); _root.clear(); }
        std::size_t Count() const noexcept { return _textures.size(); }

    private:
        std::filesystem::path Resolve(const std::filesystem::path& relativePath) const;
        std::filesystem::path _root;
        std::map<std::filesystem::path, std::shared_ptr<sf::Texture>> _textures;
    };
}
