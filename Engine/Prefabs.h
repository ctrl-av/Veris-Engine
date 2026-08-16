#pragma once
#include "Scene.h"
#include <string>

namespace Veris
{
    std::string SerializePrefab(const Scene& scene, EntityId root);
}
