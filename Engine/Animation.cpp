#include "pch.h"
#include "Animation.h"
#include "JsonValidation.h"
#include <cmath>

namespace Veris
{
    void SpriteAnimation::Validate(sf::Vector2u size) const
    {
        if (Frames.empty() || Frames.size() > 4096) throw std::invalid_argument("Animations require between 1 and 4096 frames.");
        for (const auto& frame : Frames)
        {
            const auto& rectangle = frame.Rectangle;
            if (!std::isfinite(frame.Seconds) || frame.Seconds < 0.001 || frame.Seconds > 3600 ||
                rectangle.position.x < 0 || rectangle.position.y < 0 || rectangle.size.x <= 0 || rectangle.size.y <= 0 ||
                static_cast<std::uint64_t>(rectangle.position.x) + rectangle.size.x > size.x ||
                static_cast<std::uint64_t>(rectangle.position.y) + rectangle.size.y > size.y)
                throw std::invalid_argument("Animation frames require in-bounds atlas rectangles and durations between 1 ms and 1 hour.");
        }
    }

    void SpriteAnimation::Advance(double seconds)
    {
        if (!std::isfinite(seconds) || seconds <= 0) throw std::invalid_argument("Animation step must be finite and positive.");
        if (!Playing || Frames.empty()) return;
        double duration = 0;
        for (const auto& frame : Frames) duration += frame.Seconds;
        if (Loop) seconds = std::fmod(seconds, duration);
        Elapsed += seconds;
        while (Elapsed + 1e-12 >= Frames.at(Frame).Seconds)
        {
            Elapsed -= Frames[Frame].Seconds;
            if (Frame + 1 < Frames.size()) ++Frame;
            else if (Loop) Frame = 0;
            else { Elapsed = 0; Playing = false; break; }
        }
    }

    std::string SerializeAnimation(const SpriteAnimation& animation)
    {
        auto frames = Json::Document::array();
        for (const auto& frame : animation.Frames)
            frames.push_back({{"rectangle", {frame.Rectangle.position.x, frame.Rectangle.position.y, frame.Rectangle.size.x, frame.Rectangle.size.y}}, {"seconds", frame.Seconds}});
        return Json::Document{{"frames", frames}, {"loop", animation.Loop}, {"playing", animation.Playing}}.dump();
    }

    SpriteAnimation DeserializeAnimation(const std::string& json, sf::Vector2u textureSize)
    {
        const auto document = Json::Parse(json);
        Json::Fields(document, {"frames", "loop", "playing"});
        SpriteAnimation animation;
        animation.Loop = document.at("loop").get<bool>();
        animation.Playing = document.at("playing").get<bool>();
        if (!document.at("frames").is_array() || document.at("frames").size() > 4096) throw std::invalid_argument("Animation frames must be an array with at most 4096 entries.");
        for (const auto& frame : document.at("frames"))
        {
            Json::Fields(frame, {"rectangle", "seconds"});
            const auto& rectangle = frame.at("rectangle");
            if (!rectangle.is_array() || rectangle.size() != 4) throw std::invalid_argument("Animation rectangles require four integers.");
            animation.Frames.push_back({{{Json::Integer(rectangle[0]), Json::Integer(rectangle[1])}, {Json::Integer(rectangle[2]), Json::Integer(rectangle[3])}}, Json::Number(frame.at("seconds"))});
        }
        animation.Validate(textureSize);
        return animation;
    }
}
