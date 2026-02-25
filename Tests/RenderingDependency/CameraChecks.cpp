#include "Checks.h"
#include "../../Engine/Engine.h"
#include <SFML/Graphics/Image.hpp>
#include <cmath>
#include <limits>

void VerifyCameras()
{
    Veris::Engine engine;
    Reject<std::logic_error>([&] { engine.PixelToWorld({0, 0}); });
    engine.Initialize({100, 80});
    Require((engine.PixelToWorld({50, 40}) - sf::Vector2f{50, 40}).length() < 0.001f, "Default camera must preserve pixel coordinates.");
    engine.ActiveCamera().SetCenter({200, 100});
    engine.ActiveCamera().SetZoom(2);
    Require((engine.PixelToWorld({50, 40}) - sf::Vector2f{200, 100}).length() < 0.001f, "Camera center must map to the viewport center.");
    const auto right = engine.PixelToWorld({100, 40});
    Require(std::abs(right.x - 225) < 0.001f, "Zoom must reduce visible world width.");
    engine.ActiveCamera().SetRotation(90);
    const auto rotated = engine.PixelToWorld({100, 40});
    Require(std::abs(rotated.x - 200) < 0.001f && std::abs(rotated.y - 125) < 0.001f, "Camera rotation must affect coordinate mapping.");
    const auto pixel = engine.WorldToPixel(rotated);
    Require(std::abs(pixel.x - 100) <= 1 && std::abs(pixel.y - 40) <= 1, "Camera mappings must round-trip.");
    engine.Resize({200, 160});
    Require((engine.PixelToWorld({100, 80}) - sf::Vector2f{200, 100}).length() < 0.001f, "Resize must preserve a positioned camera center.");
    Reject<std::invalid_argument>([&] { engine.ActiveCamera().SetZoom(0); });
    Reject<std::invalid_argument>([&] { engine.ActiveCamera().SetRotation(std::numeric_limits<float>::infinity()); });
    const sf::Texture white(sf::Image({2, 2}, sf::Color::White));
    sf::Sprite sprite(white);
    sprite.setPosition({200, 100});
    engine.SubmitSprite(sprite);
    engine.Update();
    Require(engine.GetFrame().copyToImage().getPixel({101, 79}) == sf::Color::White, "Rendering must use the active camera transform.");
    engine.Shutdown();
    engine.Initialize({8, 8});
    Require(engine.PixelToWorld({0, 0}).length() < 0.001f, "New sessions must reset the camera.");
}
