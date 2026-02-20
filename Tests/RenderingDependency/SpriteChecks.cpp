#include "Checks.h"
#include "../../Engine/Engine.h"
#include <SFML/Graphics.hpp>
#include <cmath>

namespace
{
    void SubmitTransformFixture(Veris::Engine& engine, const sf::Texture& white);
    void SubmitOrderingFixture(Veris::Engine& engine, const sf::Texture& white);
    void VerifySpritePixels(const sf::Image& image);
    void VerifySpriteQueueReset(Veris::Engine& engine, const sf::Texture& texture);
}

void VerifySpriteRendering(const std::filesystem::path& previewPath)
{
    sf::Texture white(sf::Image({2, 2}, sf::Color::White));
    Veris::Engine engine;
    Reject<std::logic_error>([&] { engine.SubmitSprite(sf::Sprite(white)); });
    engine.Initialize({64, 48});
    SubmitTransformFixture(engine, white);
    SubmitOrderingFixture(engine, white);
    sf::Image atlasImage({2, 2}, sf::Color::Green);
    atlasImage.setPixel({1, 0}, sf::Color::Red);
    const sf::Texture atlas(atlasImage);
    sf::Sprite cropped(atlas, sf::IntRect({1, 0}, {1, 1}));
    cropped.setPosition({34, 20});
    cropped.setScale({6, 6});
    engine.SubmitSprite(cropped);
    engine.Update();
    const auto renderedImage = engine.GetFrame().copyToImage();
    Require(renderedImage.saveToFile(previewPath), "Sprite preview must be saved for visual review.");
    VerifySpritePixels(renderedImage);
    VerifySpriteQueueReset(engine, white);
}

namespace
{
    void SubmitTransformFixture(Veris::Engine& engine, const sf::Texture& white)
    {
        sf::Sprite overlay(white);
        overlay.setPosition({6, 6});
        overlay.setScale({4, 4});
        overlay.setColor({255, 0, 0, 128});
        engine.SubmitSprite(overlay, 10);
        sf::Sprite background(white);
        background.setPosition({2, 2});
        background.setScale({8, 8});
        background.setColor(sf::Color::Blue);
        engine.SubmitSprite(background, -1);
        sf::Sprite rotated(white);
        rotated.setPosition({30, 5});
        rotated.setScale({4, 2});
        rotated.setRotation(sf::degrees(90));
        rotated.setColor(sf::Color::Magenta);
        engine.SubmitSprite(rotated);
    }

    void SubmitOrderingFixture(Veris::Engine& engine, const sf::Texture& white)
    {
        sf::Sprite first(white);
        first.setPosition({20, 20});
        first.setScale({4, 4});
        first.setColor(sf::Color::Green);
        engine.SubmitSprite(first, 5);
        first.setColor(sf::Color::Yellow);
        engine.SubmitSprite(first, 5);
    }

    void VerifySpritePixels(const sf::Image& image)
    {
        Require(image.getPixel({3, 3}) == sf::Color::Blue, "Sprite position, scale, and tint must affect rendered pixels.");
        Require(image.getPixel({18, 3}) == Veris::Engine::BackgroundColor, "Scaled sprites must stay within their transformed bounds.");
        const auto blended = image.getPixel({8, 8});
        Require(std::abs(static_cast<int>(blended.r) - 128) <= 1 && std::abs(static_cast<int>(blended.b) - 127) <= 1 && blended.g == 0,
            "Layer ordering and alpha tint must blend an overlay over its background.");
        Require(image.getPixel({28, 7}) == sf::Color::Magenta && image.getPixel({31, 7}) == Veris::Engine::BackgroundColor,
            "Rotation must change the sprite's pixel coverage.");
        Require(image.getPixel({21, 21}) == sf::Color::Yellow, "Equal layers must preserve submission order.");
        Require(image.getPixel({35, 21}) == sf::Color::Red, "Texture rectangles must select the requested atlas texel.");
    }

    void VerifySpriteQueueReset(Veris::Engine& engine, const sf::Texture& texture)
    {
        engine.Update();
        Require(engine.GetFrame().copyToImage().getPixel({3, 3}) == Veris::Engine::BackgroundColor, "Sprite submissions must last exactly one frame.");
        engine.SubmitSprite(sf::Sprite(texture));
        engine.Shutdown();
        engine.Initialize({4, 4});
        engine.Update();
        Require(engine.GetFrame().copyToImage().getPixel({0, 0}) == Veris::Engine::BackgroundColor, "Shutdown must discard pending sprite submissions.");
        const sf::Texture emptyTexture;
        Reject<std::invalid_argument>([&] { engine.SubmitSprite(sf::Sprite(emptyTexture)); });
    }
}
