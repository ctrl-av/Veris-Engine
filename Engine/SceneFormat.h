#pragma once

#include "Scene.h"
#include "TextureResources.h"
#include <string_view>

namespace Veris
{
    std::string SerializeScene(const Scene& scene);
    Scene DeserializeScene(std::string_view json, TextureResources& textures);
}
