#include "Checks.h"
#include "../../Engine/Engine.h"
#include "../../Engine/SceneFormat.h"
#include <cmath>

void VerifyPhysics()
{
    Veris::Engine engine;
    engine.Initialize({64, 64});
    auto& scene = engine.ActiveScene();
    const auto floor = scene.CreateEntity("Ground");
    scene.Get(floor).Physics = Veris::PhysicsBody{};
    scene.Get(floor).Physics->Type = Veris::BodyType::Static;
    scene.Get(floor).Physics->HalfSize = {500, 25};
    scene.Get(floor).Transform.Position = {0, 300};
    const auto falling = scene.CreateEntity("Falling box");
    scene.Get(falling).Physics = Veris::PhysicsBody{};
    scene.Get(falling).Physics->GravityScale = 1;
    scene.Get(falling).Physics->HalfSize = {25, 25};
    for (int index = 0; index < 180; ++index) engine.FixedUpdate(1.0 / 60);
    Require(scene.Get(falling).Transform.Position.y > 245 && scene.Get(falling).Transform.Position.y < 252,
        "Box2D gravity and contact resolution must settle a box above static ground in pixel coordinates.");
    Require(scene.Get(floor).Transform.Position == sf::Vector2f{0, 300}, "Static bodies must not move under collision impulses.");
    const auto mass = engine.Physics().Mass(scene, falling);
    Require(std::abs(mass - 0.25f) < 0.001f, "Collider dimensions must convert pixels to meters before density determines mass.");
    engine.Physics().ApplyImpulse(scene, falling, {0, -mass * 5});
    const auto before = scene.Get(falling).Transform.Position.y;
    engine.FixedUpdate(1.0 / 60);
    Require(scene.Get(falling).Transform.Position.y < before - 5, "Applied impulses must change actual simulated motion.");
    const auto moving = scene.CreateEntity("Kinematic circle");
    scene.Get(moving).Physics = Veris::PhysicsBody{};
    scene.Get(moving).Physics->Type = Veris::BodyType::Kinematic;
    scene.Get(moving).Physics->Shape = Veris::ColliderShape::Circle;
    scene.Get(moving).Physics->Velocity = {60, 0};
    scene.Get(moving).Transform.Position = {1000, 0};
    engine.FixedUpdate(1.0 / 60);
    Require(std::abs(scene.Get(moving).Transform.Position.x - 1001) < 0.01f, "Kinematic velocity must use pixels per second.");
    auto restored = Veris::DeserializeScene(Veris::SerializeScene(scene), engine.Textures());
    Require(restored.Matches(scene), "Physics material, geometry and motion must round-trip with scenes.");
    Reject<std::invalid_argument>([&] { scene.SetParent(falling, floor); });
    scene.DestroyEntity(falling);
    engine.Update();
    Require(engine.Physics().BodyCount() == 2, "Removing entities must remove their native bodies without dangling pointers.");
    engine.Shutdown();
    Require(engine.Physics().BodyCount() == 0, "Shutdown must release the entire physics world.");
}
