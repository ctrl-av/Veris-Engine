#include "Checks.h"
#include "../../Engine/Engine.h"
#include "../../Engine/InputConfiguration.h"
#include <fstream>
#include <cmath>

void VerifyGameplayApi(const std::filesystem::path& root)
{
    std::filesystem::create_directories(root);
    std::ofstream(root / "player.lua") << R"(
local child
return {
 start=function()
   engine.set_position(self,10,20)
   child=engine.create('Temporary')
   assert(engine.find('Temporary')==child)
   engine.camera(20,30,2,0)
 end,
 update=function(dt)
   local held=engine.action('MoveRight')
   if held then engine.set_velocity(self,60,0) end
   if child then engine.destroy(child); child=nil end
   local x,y=engine.position(self)
   assert(x>=10 and y==20)
 end
})";
    Veris::Engine engine;
    engine.Initialize({32, 32}); engine.Scripts().SetRoot(root);
    Veris::LoadInputConfiguration(root, engine.Actions());
    Veris::SetNamedInput(engine.Input(), "Key:D", true); engine.Actions().Update(engine.Input());
    auto& scene = engine.ActiveScene();
    const auto player = scene.CreateEntity("Player");
    scene.Get(player).Physics = Veris::PhysicsBody{}; scene.Get(player).Script = Veris::ScriptComponent{"player.lua"};
    engine.FixedUpdate(1.0 / 60);
    Require(std::abs(scene.Get(player).Transform.Position.x - 11) < 0.01f && scene.Entities().size() == 1,
        "Lua input, position, velocity, creation and deletion must mutate actual engine state.");
    Require(engine.Scripts().Diagnostics().empty() && engine.ActiveCamera().Zoom() == 2, "Valid native API calls must succeed without script diagnostics.");
    std::ofstream(root / "invalid.lua") << "return {update=function() engine.set_velocity(self,0/0,0) end}";
    scene.Get(player).Script = Veris::ScriptComponent{"invalid.lua"};
    engine.FixedUpdate(1.0 / 60);
    Require(engine.Scripts().Diagnostics().back().find("finite") != std::string::npos && std::isfinite(scene.Get(player).Physics->Velocity.x),
        "Invalid Lua API arguments must become isolated script errors without corrupting components.");
    std::filesystem::remove(root / "player.lua"); std::filesystem::remove(root / "invalid.lua"); std::filesystem::remove(root);
}
