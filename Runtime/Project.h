#pragma once

#include <SFML/System/Vector2.hpp>
#include <filesystem>
#include <string>

namespace Veris
{
    struct RuntimeProject
    {
        std::filesystem::path ProjectFile;
        std::string Name;
        std::filesystem::path ContentDirectory;
        std::filesystem::path GameCodeDirectory;
        sf::Vector2u WindowSize;
    };

    RuntimeProject LoadProject(const std::filesystem::path& projectFile);
}
