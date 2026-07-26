#pragma once

#include "InputActions.h"
#include <filesystem>

namespace Veris
{
    void SetNamedInput(InputState& input, const std::string& binding, bool down);
    void LoadInputConfiguration(const std::filesystem::path& projectDirectory, InputActions& actions);
}
