#pragma once

#include <SFML/Graphics/RenderTexture.hpp>
#include <cstdint>
#include <memory>

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
        void Shutdown() noexcept;
        bool IsInitialized() const noexcept { return _renderTarget != nullptr; }
        std::uint64_t CompletedFrames() const noexcept { return _completedFrames; }
        const sf::Texture& GetFrame() const;

    private:
        std::unique_ptr<sf::RenderTexture> _renderTarget;
        std::uint64_t _completedFrames = 0;
    };
}
