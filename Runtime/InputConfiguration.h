#pragma once

#include "../Engine/InputActions.h"
#include <filesystem>

namespace Veris
{
    void LoadInputConfiguration(const std::filesystem::path& projectDirectory, InputActions& actions);
}
