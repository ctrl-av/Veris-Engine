#include "Checks.h"
#include "../../Engine/Engine.h"
#include "../../Engine/SceneFormat.h"
#include <fstream>

void VerifyScripts(const std::filesystem::path& root)
{
    std::filesystem::create_directories(root);
    std::ofstream(root / "good.lua") << "local count=0; return {start=function() print('started') end,update=function(dt) count=count+1; print(count,dt) end}";
    std::ofstream(root / "loop.lua") << "return {update=function() while true do end end}";
    std::ofstream(root / "memory.lua") << "return {update=function() local s=string.rep('x',8000000) end}";
    std::ofstream(root / "bad.lua") << "not valid Lua";
    Veris::Engine engine;
    engine.Initialize({8, 8}); engine.Scripts().SetRoot(root);
    auto& scene = engine.ActiveScene();
    const auto good = scene.CreateEntity("Good"); scene.Get(good).Script = Veris::ScriptComponent{"good.lua"};
    const auto loop = scene.CreateEntity("Loop"); scene.Get(loop).Script = Veris::ScriptComponent{"loop.lua"};
    const auto memory = scene.CreateEntity("Memory"); scene.Get(memory).Script = Veris::ScriptComponent{"memory.lua"};
    const auto bad = scene.CreateEntity("Syntax"); scene.Get(bad).Script = Veris::ScriptComponent{"bad.lua"};
    engine.FixedUpdate(0.01); engine.FixedUpdate(0.02);
    Require(engine.Scripts().Diagnostics().size() == 6, "Start must run once, update state must persist, and failures must be isolated and reported once.");
    Require(engine.Scripts().Diagnostics()[2].find("budget") != std::string::npos, "Infinite loops must hit the instruction budget.");
    const auto json = Veris::SerializeScene(scene);
    Require(Veris::DeserializeScene(json, engine.Textures()).Get(good).Script->Path == "good.lua", "Script assignments must survive scene round trips.");
    Reject<std::invalid_argument>([&] { engine.Scripts().Validate({"../escape.lua"}); });
    scene.DestroyEntity(good); engine.FixedUpdate(0.01);
    Require(engine.Scripts().InstanceCount() == 3, "Deleted entities must release their Lua states.");
    for (const auto name : {"good.lua", "loop.lua", "memory.lua", "bad.lua"}) std::filesystem::remove(root / name);
    std::filesystem::remove(root);
}
