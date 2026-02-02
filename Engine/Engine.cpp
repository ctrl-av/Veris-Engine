#include "pch.h"
#include "Engine.h"
#include <SFML/Config.hpp>
#include <stdexcept>
#include <cmath>

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
        _completedFrames = 0;
    }

    void Engine::Update()
    {
        if (!IsInitialized()) throw std::logic_error("Initialize the engine before updating it.");
        _renderTarget->clear(BackgroundColor);
        _renderTarget->display();
        ++_completedFrames;
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
    }

    void Engine::Shutdown() noexcept
    {
        _renderTarget.reset();
        _completedFrames = 0;
        _completedFixedUpdates = 0;
        _simulationSeconds = 0;
    }

    const sf::Texture& Engine::GetFrame() const
    {
        if (!IsInitialized()) throw std::logic_error("Initialize the engine before accessing its frame.");
        return _renderTarget->getTexture();
    }
}
