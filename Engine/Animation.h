#pragma once
#include <SFML/Graphics/Rect.hpp>
#include <SFML/System/Vector2.hpp>
#include <string>
#include <vector>

namespace Veris
{
    struct AnimationFrame
    {
        sf::IntRect Rectangle;
        double Seconds;
    };

    struct SpriteAnimation
    {
        std::vector<AnimationFrame> Frames;
        bool Loop = true;
        bool Playing = true;
        double Elapsed = 0;
        std::size_t Frame = 0;
        void Validate(sf::Vector2u textureSize) const;
        void Advance(double seconds);
    };

    std::string SerializeAnimation(const SpriteAnimation& animation);
    SpriteAnimation DeserializeAnimation(const std::string& json, sf::Vector2u textureSize);
}
