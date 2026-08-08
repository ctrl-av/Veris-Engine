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
    };

    RuntimeProject LoadProject(const std::filesystem::path& projectFile);
}
