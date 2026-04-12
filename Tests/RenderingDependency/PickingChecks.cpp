#include "Checks.h"
#include "../../Engine/Engine.h"
#include <SFML/Graphics/Image.hpp>

void VerifyEntityPicking()
{
    Veris::Engine engine;
    engine.Initialize({100, 100});
    auto texture = std::make_shared<sf::Texture>(sf::Image({16, 16}, sf::Color::White));
    const auto back = engine.ActiveScene().CreateEntity("Back");
    auto& backEntity = engine.ActiveScene().Get(back);
    backEntity.Transform.Position = {10, 10};
    backEntity.Sprite = Veris::SpriteComponent{};
    backEntity.Sprite->Texture = texture;
    const auto front = engine.ActiveScene().DuplicateEntity(back);
    auto& frontEntity = engine.ActiveScene().Get(front);
    frontEntity.Sprite->Layer = 2;
    Require(engine.PickEntity({15, 15}) == front, "Picking must choose the topmost visible layer.");
    frontEntity.Sprite->Visible = false;
    Require(engine.PickEntity({15, 15}) == back, "Hidden sprites must not intercept picking.");
    frontEntity.Sprite->Visible = true;
    frontEntity.Transform.Scale = {0, 0};
    Require(engine.PickEntity({15, 15}) == back, "Collapsed sprites must not intercept picking.");
    backEntity.Transform.Position = {200, 200};
    engine.ActiveCamera().SetCenter({200, 200});
    engine.ActiveCamera().SetZoom(2);
    Require(engine.PickEntity({55, 55}) == back, "Picking must use camera world-to-pixel mapping.");
    Require(engine.PickEntity({99, 99}) == 0, "Empty space must produce an empty selection.");
    engine.ActiveScene().DestroyEntity(back);
    Require(engine.PickEntity({55, 55}) == 0, "Deleted entities must disappear from picking.");
}
