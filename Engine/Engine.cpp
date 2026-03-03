#include "pch.h"
#include "Engine.h"
#include <SFML/Config.hpp>
#include <stdexcept>
#include <cmath>
#include <algorithm>

static_assert(SFML_VERSION_MAJOR == 3 && SFML_VERSION_MINOR == 0 && SFML_VERSION_PATCH == 1,
    "Veris requires the pinned SFML 3.0.1 backend.");

namespace Veris
{
    void Engine::Initialize(sf::Vector2u viewportSize)
    {
        if (IsInitialized()) throw std::logic_error("Engine is already initialized.");
        if (viewportSize.x == 0 || viewportSize.y == 0) throw std::invalid_argument("Viewport dimensions must be positive.");
        auto renderTarget = std::make_unique<sf::RenderTexture>(viewportSize);
        renderTarget->clear(BackgroundColor);
        renderTarget->display();
        _renderTarget = std::move(renderTarget);
        _camera = Camera{};
        _camera.Resize(viewportSize);
        _completedFrames = 0;
    }

    void Engine::Update()
    {
        if (!IsInitialized()) throw std::logic_error("Initialize the engine before updating it.");
        try { _scene.Submit(*this); }
        catch (...) { _sprites.clear(); throw; }
        _renderTarget->clear(BackgroundColor);
        _renderTarget->setView(_camera.View());
        std::stable_sort(_sprites.begin(), _sprites.end(), [](const auto& left, const auto& right) { return left.Layer < right.Layer; });
        for (const auto& sprite : _sprites) _renderTarget->draw(sprite.Sprite, sprite.Transform);
        _renderTarget->display();
        _sprites.clear();
        ++_completedFrames;
    }

    void Engine::SubmitSprite(const sf::Sprite& sprite, int layer, const sf::Transform& transform)
    {
        if (!IsInitialized()) throw std::logic_error("Initialize the engine before submitting sprites.");
        if (sprite.getTexture().getSize().x == 0 || sprite.getTexture().getSize().y == 0)
            throw std::invalid_argument("Sprites require a loaded, nonempty texture.");
        _sprites.push_back({layer, sprite, transform});
    }

    void Engine::FixedUpdate(double stepSeconds)
    {
        if (!IsInitialized()) throw std::logic_error("Initialize the engine before simulating it.");
        if (!std::isfinite(stepSeconds) || stepSeconds <= 0) throw std::invalid_argument("Simulation step must be finite and positive.");
        _simulationSeconds += stepSeconds;
        ++_completedFixedUpdates;
    }

    void Engine::Resize(sf::Vector2u viewportSize)
    {
        if (!IsInitialized()) throw std::logic_error("Initialize the engine before resizing it.");
        if (viewportSize.x == 0 || viewportSize.y == 0) throw std::invalid_argument("Viewport dimensions must be positive.");
        if (_renderTarget->getSize() == viewportSize) return;
        auto renderTarget = std::make_unique<sf::RenderTexture>(viewportSize);
        renderTarget->clear(BackgroundColor);
        renderTarget->display();
        _renderTarget = std::move(renderTarget);
        _camera.Resize(viewportSize);
    }

    void Engine::Shutdown() noexcept
    {
        _sprites.clear();
        _scene.Clear();
        _renderTarget.reset();
        _completedFrames = 0;
        _completedFixedUpdates = 0;
        _simulationSeconds = 0;
        _input = InputState{};
        _actions = InputActions{};
        _camera = Camera{};
    }

    const sf::Texture& Engine::GetFrame() const
    {
        if (!IsInitialized()) throw std::logic_error("Initialize the engine before accessing its frame.");
        return _renderTarget->getTexture();
    }

    sf::Vector2f Engine::PixelToWorld(sf::Vector2i pixel) const
    {
        if (!IsInitialized()) throw std::logic_error("Initialize the engine before mapping coordinates.");
        return _renderTarget->mapPixelToCoords(pixel, _camera.View());
    }

    sf::Vector2i Engine::WorldToPixel(sf::Vector2f world) const
    {
        if (!IsInitialized()) throw std::logic_error("Initialize the engine before mapping coordinates.");
        return _renderTarget->mapCoordsToPixel(world, _camera.View());
    }
}
