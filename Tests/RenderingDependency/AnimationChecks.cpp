#include "Checks.h"
#include "../../Engine/Engine.h"
#include "../../Engine/SceneFormat.h"
#include <SFML/Graphics/Image.hpp>

void VerifyAnimations()
{
    sf::Image atlas({2, 1}, sf::Color::Red);
    atlas.setPixel({1, 0}, sf::Color::Blue);
    auto texture = std::make_shared<sf::Texture>(atlas);
    Veris::Engine engine;
    engine.Initialize({8, 8});
    auto& scene = engine.ActiveScene();
    const auto id = scene.CreateEntity("Animated");
    scene.Get(id).Sprite = Veris::SpriteComponent{};
    scene.Get(id).Sprite->Texture = texture;
    scene.Get(id).Sprite->TexturePath = "atlas.png";
    scene.Get(id).Animation = Veris::DeserializeAnimation(R"({"frames":[{"rectangle":[0,0,1,1],"seconds":0.1},{"rectangle":[1,0,1,1],"seconds":0.2}],"loop":true,"playing":true})", {2, 1});
    engine.FixedUpdate(0.11);
    engine.Update();
    Require(scene.Get(id).Animation->Frame == 1 && engine.GetFrame().copyToImage().getPixel({0, 0}) == sf::Color::Blue, "Fixed updates must change rendered atlas frames.");
    engine.FixedUpdate(0.2);
    Require(scene.Get(id).Animation->Frame == 0, "Looping clips must wrap while preserving leftover time.");
    auto& animation = *scene.Get(id).Animation;
    animation.Playing = false;
    engine.FixedUpdate(1);
    Require(animation.Frame == 0, "Paused animations must retain their frame.");
    animation.Playing = true; animation.Loop = false;
    engine.FixedUpdate(4);
    Require(animation.Frame == 1 && !animation.Playing, "Non-looping clips must stop on the last frame.");
    auto copy = Veris::DeserializeAnimation(Veris::SerializeAnimation(animation), {2, 1});
    Require(copy.Frames.size() == 2 && !copy.Loop && !copy.Playing, "Animation configuration must round-trip.");
    Reject<std::invalid_argument>([&] { copy.Validate({1, 1}); });
    Reject<std::invalid_argument>([&] { copy.Advance(-1); });
    const auto duplicate = scene.DuplicateEntity(id);
    Require(scene.Get(duplicate).Animation->Frames.size() == 2, "Duplication must retain animation components.");
}
