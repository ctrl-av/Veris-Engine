#include "Checks.h"
#include "../../Engine/Engine.h"
#include "../../Engine/SceneFormat.h"
#include <fstream>

void VerifySceneLoading(const std::filesystem::path& root)
{
    std::filesystem::create_directories(root);
    Veris::Scene first;
    const auto firstId = first.CreateEntity("First");
    first.Get(firstId).Script = Veris::ScriptComponent{"transition.lua"};
    Veris::Scene second;
    const auto secondId = second.CreateEntity("Second");
    second.Get(secondId).Script = Veris::ScriptComponent{"arrival.lua"};
    std::ofstream(root / "first.verisscene") << Veris::SerializeScene(first);
    std::ofstream(root / "second.verisscene") << Veris::SerializeScene(second);
    std::ofstream(root / "transition.lua") << "return {update=function() engine.transition('second.verisscene'); assert(engine.find('First')==self) end}";
    std::ofstream(root / "arrival.lua") << "return {start=function() print('arrived') end}";
    std::ofstream(root / "bad.verisscene") << "invalid json";
    Veris::Engine engine; engine.Initialize({8, 8}); engine.Textures().SetRoot(root); engine.Scripts().SetRoot(root);
    engine.LoadSceneFile("first.verisscene");
    Require(engine.ActiveScene().Get(firstId).Name == "First", "Scene files must populate the active engine scene.");
    engine.FixedUpdate(1.0 / 60);
    Require(engine.CurrentScene() == "second.verisscene" && engine.ActiveScene().Get(secondId).Name == "Second" && engine.Scripts().InstanceCount() == 0,
        "Lua transitions must commit after callbacks finish and release old interpreter state.");
    engine.FixedUpdate(1.0 / 60);
    Require(engine.Scripts().Diagnostics().back().find("arrived") != std::string::npos, "The destination scene must start its own scripts on the next tick.");
    const auto before = Veris::SerializeScene(engine.ActiveScene());
    Reject<std::exception>([&] { engine.LoadSceneFile("bad.verisscene"); });
    Reject<std::exception>([&] { engine.LoadSceneFile("missing.verisscene"); });
    Reject<std::invalid_argument>([&] { engine.RequestScene("../outside.verisscene"); });
    Require(Veris::SerializeScene(engine.ActiveScene()) == before && engine.CurrentScene() == "second.verisscene", "Failed scene loading must preserve active entities and scene identity.");
    engine.RequestScene("first.verisscene");
    Reject<std::logic_error>([&] { engine.RequestScene("second.verisscene"); });
    engine.FixedUpdate(1.0 / 60);
    Require(engine.CurrentScene() == "first.verisscene", "Only the first valid transition request in a tick may win.");
    for (const auto name : {"first.verisscene", "second.verisscene", "bad.verisscene", "transition.lua", "arrival.lua"}) std::filesystem::remove(root / name);
    std::filesystem::remove(root);
}
