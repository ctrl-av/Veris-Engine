#include "Checks.h"
#include "../../Engine/Engine.h"
#include <SFML/Graphics/Image.hpp>

void VerifySceneHierarchy()
{
    Veris::Scene scene;
    const auto parent = scene.CreateEntity("Parent");
    const auto child = scene.CreateEntity("Child");
    const auto grandchild = scene.CreateEntity("Grandchild");
    scene.Get(parent).Transform.Position = {10, 20};
    scene.Get(parent).Transform.Scale = {2, 2};
    scene.Get(child).Transform.Position = {3, 4};
    scene.SetParent(child, parent);
    scene.SetParent(grandchild, child);
    Require(scene.WorldTransform(child).transformPoint({0, 0}) == sf::Vector2f{16, 28}, "World transforms must compose parent scale and translation.");
    Reject<std::invalid_argument>([&] { scene.SetParent(parent, grandchild); });
    Reject<std::out_of_range>([&] { scene.SetParent(child, 999); });
    Require(scene.Get(child).Parent() == parent, "Invalid parenting must preserve the previous relationship.");
    scene.SetParent(child, 0);
    Require(scene.WorldTransform(child).transformPoint({0, 0}) == sf::Vector2f{3, 4}, "Detaching must preserve the local transform.");
    scene.SetParent(child, parent);
    scene.DestroyEntity(parent);
    Require(scene.Entities().empty(), "Destroy must remove descendants and their components.");
    const auto replacement = scene.CreateEntity("Replacement");
    Require(replacement != parent && !scene.Contains(child), "Destroyed identifiers must never alias new entities.");
    scene.Clear();
    Require(scene.CreateEntity("After clear") > replacement, "Clear must preserve identifier monotonicity.");
}

void VerifySceneRendering()
{
    Veris::Engine engine;
    engine.Initialize({16, 16});
    const auto id = engine.ActiveScene().CreateEntity("Sprite");
    auto& entity = engine.ActiveScene().Get(id);
    entity.Transform.Position = {4, 4};
    entity.Transform.Scale = {3, 3};
    entity.Sprite = Veris::SpriteComponent{};
    entity.Sprite->Texture = std::make_shared<sf::Texture>(sf::Image({2, 2}, sf::Color::Green));
    engine.Update();
    Require(engine.GetFrame().copyToImage().getPixel({5, 5}) == sf::Color::Green, "Scene sprite components must render through entity transforms.");
    entity.Sprite->Visible = false;
    engine.Update();
    Require(engine.GetFrame().copyToImage().getPixel({5, 5}) == Veris::Engine::BackgroundColor, "Hidden components must stop rendering.");
    entity.Sprite.reset();
    engine.Update();
    engine.Shutdown();
    Require(engine.ActiveScene().Entities().empty(), "Shutdown must release scene entities and texture ownership.");
}
