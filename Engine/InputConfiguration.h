#pragma once

#include "InputActions.h"
#include <filesystem>
#include <string_view>

namespace Veris
{
    void ValidateInputConfiguration(std::string_view json);
    void SetNamedInput(InputState& input, const std::string& binding, bool down);
    void LoadInputConfiguration(const std::filesystem::path& projectDirectory, InputActions& actions);
}
