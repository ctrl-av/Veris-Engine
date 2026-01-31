#pragma once

#include <SFML/System/Vector2.hpp>
#include "../Engine/Window.h"
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
        WindowSettings Window;
    };

    RuntimeProject LoadProject(const std::filesystem::path& projectFile);
}
