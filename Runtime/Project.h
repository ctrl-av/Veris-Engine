#pragma once

#include <SFML/System/Vector2.hpp>
#include "../Engine/Window.h"
#include <filesystem>
#include <string>
#include <optional>

namespace Veris
{
    struct RuntimeProject
    {
        std::filesystem::path ProjectFile;
        std::string Name;
        std::filesystem::path ContentDirectory;
        std::filesystem::path GameCodeDirectory;
        WindowSettings Window;
        std::optional<std::filesystem::path> StartupScene;
        sf::Vector2f Gravity{0, 9.81f};
        float MasterVolume = 100;
    };

    RuntimeProject LoadProject(const std::filesystem::path& projectFile);
}
