#include "Checks.h"
#include "../../Engine/Engine.h"
#include "../../Engine/Prefabs.h"
#include "../../Engine/SceneFormat.h"
#include <fstream>

void VerifyPrefabs(const std::filesystem::path& root)
{
    std::filesystem::create_directories(root);
    Veris::Engine engine; engine.Initialize({32, 32}); engine.Textures().SetRoot(root); engine.Scripts().SetRoot(root);
    auto& scene = engine.ActiveScene();
    const auto source = scene.CreateEntity("Actor");
    scene.Get(source).Transform.Position = {10, 20}; scene.Get(source).Physics = Veris::PhysicsBody{};
    const auto child = scene.CreateEntity("Visual"); scene.SetParent(child, source);
    const auto path = root / "actor.verisprefab";
    std::ofstream(path) << Veris::SerializePrefab(scene, source);
    const auto first = engine.InstantiatePrefab("actor.verisprefab", {100, 200});
    const auto second = engine.InstantiatePrefab("actor.verisprefab", {200, 200});
    Require(first != second && scene.Subtree(first).size() == 2 && scene.Subtree(second).size() == 2 && scene.Get(first).Transform.Position == sf::Vector2f{100, 200},
        "Prefab instantiation must allocate independent IDs, remap hierarchy and apply requested placement.");
    scene.Get(first).Name = "Override";
    scene.Get(source).Name = "Updated";
    const auto added = scene.CreateEntity("New child"); scene.SetParent(added, source);
    const auto revision = Veris::SerializePrefab(scene, source);
    std::ofstream(path) << revision;
    Require(scene.Get(first).Name == "Override" && scene.Subtree(first).size() == 2, "Source edits must not silently overwrite existing instances.");
    const auto oldChild = scene.Subtree(first)[1];
    engine.ReapplyPrefab(first, "actor.verisprefab");
    Require(scene.Get(first).Name == "Updated" && scene.Subtree(first).size() == 3 && !scene.Contains(oldChild) && scene.Get(first).Transform.Position == sf::Vector2f{100, 200},
        "Explicit reapply must preserve root identity/placement and replace components and children.");
    const auto before = Veris::SerializeScene(scene);
    std::ofstream(path) << "invalid prefab";
    Reject<std::exception>([&] { engine.InstantiatePrefab("actor.verisprefab", {0, 0}); });
    Reject<std::exception>([&] { engine.ReapplyPrefab(first, "actor.verisprefab"); });
    Require(Veris::SerializeScene(scene) == before, "Failed prefab operations must preserve the complete scene.");
    std::filesystem::remove(path);
    Require(Veris::DeserializeScene(before, engine.Textures()).Get(first).PrefabSource == "actor.verisprefab", "Saved instances must remain loadable when their source file is missing.");
    std::ofstream(path) << revision;
    std::ofstream(root / "spawn.lua") << "return {start=function() local id=engine.prefab('actor.verisprefab',300,400); assert(id>0) end}";
    const auto spawner = scene.CreateEntity("Spawner"); scene.Get(spawner).Script = Veris::ScriptComponent{"spawn.lua"};
    const auto count = scene.Entities().size(); engine.FixedUpdate(1.0 / 60);
    Require(scene.Entities().size() == count + 3 && engine.Scripts().Diagnostics().empty(), "Lua must spawn complete prefab subtrees through the gameplay API.");
    std::filesystem::remove(path); std::filesystem::remove(root / "spawn.lua"); std::filesystem::remove(root);
}
