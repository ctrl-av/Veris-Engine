#pragma once

#include <SFML/Graphics/RenderTexture.hpp>
#include <cstdint>
#include <memory>
#include "Input.h"
#include "InputActions.h"
#include "Camera.h"
#include <SFML/Graphics/Sprite.hpp>
#include <utility>
#include <vector>

namespace Veris
{
    class Engine
    {
    public:
        inline static const sf::Color BackgroundColor{24, 26, 32};

        Engine() = default;
        Engine(const Engine&) = delete;
        Engine& operator=(const Engine&) = delete;

        void Initialize(sf::Vector2u viewportSize);
        void Update();
        void FixedUpdate(double stepSeconds);
        void SubmitSprite(const sf::Sprite& sprite, int layer = 0);
        void Resize(sf::Vector2u viewportSize);
        void Shutdown() noexcept;
        bool IsInitialized() const noexcept { return _renderTarget != nullptr; }
        std::uint64_t CompletedFrames() const noexcept { return _completedFrames; }
        std::uint64_t CompletedFixedUpdates() const noexcept { return _completedFixedUpdates; }
        double SimulationSeconds() const noexcept { return _simulationSeconds; }
        InputState& Input() { return _input; }
        InputActions& Actions() { return _actions; }
        Camera& ActiveCamera() { return _camera; }
        sf::Vector2f PixelToWorld(sf::Vector2i pixel) const;
        sf::Vector2i WorldToPixel(sf::Vector2f world) const;
        const sf::Texture& GetFrame() const;

    private:
        std::unique_ptr<sf::RenderTexture> _renderTarget;
        std::uint64_t _completedFrames = 0;
        std::uint64_t _completedFixedUpdates = 0;
        double _simulationSeconds = 0;
        InputState _input;
        InputActions _actions;
        Camera _camera;
        std::vector<std::pair<int, sf::Sprite>> _sprites;
    };
}
