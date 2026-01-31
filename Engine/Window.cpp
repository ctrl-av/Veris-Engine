#include "pch.h"
#include "Window.h"
#include <SFML/Graphics/Sprite.hpp>
#include <stdexcept>
#include <Windows.h>

namespace Veris
{
    GameWindow::GameWindow(const WindowSettings& settings, const std::string& title)
    {
        if (settings.Size.x == 0 || settings.Size.y == 0) throw std::invalid_argument("Window dimensions must be positive.");
        const sf::VideoMode videoMode(settings.Size);
        if (settings.Mode == WindowMode::Fullscreen && !videoMode.isValid())
            throw std::invalid_argument("The requested fullscreen video mode is not supported.");
        const auto style = settings.Resizable ? sf::Style::Default : sf::Style::Titlebar | sf::Style::Close;
        const auto state = settings.Mode == WindowMode::Fullscreen ? sf::State::Fullscreen : sf::State::Windowed;
        _window.create(videoMode, sf::String::fromUtf8(title.begin(), title.end()), style, state);
        _window.setVerticalSyncEnabled(settings.VerticalSync);
        _window.setFramerateLimit(settings.VerticalSync ? 0 : 120);
        _window.setKeyRepeatEnabled(false);
        _viewportSize = _window.getSize();
        _focused = _window.hasFocus();
    }

    std::optional<sf::Event> GameWindow::PollEvent()
    {
        auto event = _window.pollEvent();
        if (event) HandleEvent(*event);
        return event;
    }

    void GameWindow::HandleEvent(const sf::Event& event)
    {
        if (event.is<sf::Event::Closed>()) { Close(); return; }
        if (event.is<sf::Event::FocusLost>()) _focused = false;
        if (event.is<sf::Event::FocusGained>()) _focused = true;
        const auto resize = event.getIf<sf::Event::Resized>();
        if (!resize) return;
        _drawable = resize->size.x > 0 && resize->size.y > 0;
        if (!_drawable) return;
        _viewportSize = resize->size;
        _window.setView(sf::View(sf::FloatRect({0.f, 0.f}, sf::Vector2f(_viewportSize))));
    }

    void GameWindow::Resize(sf::Vector2u size)
    {
        if (!IsOpen()) throw std::logic_error("Cannot resize a closed window.");
        if (size.x == 0 || size.y == 0) throw std::invalid_argument("Window dimensions must be positive.");
        _window.setSize(size);
        HandleEvent(sf::Event::Resized{_window.getSize()});
    }

    bool GameWindow::CanRender() const
    {
        return IsOpen() && _drawable && !IsIconic(_window.getNativeHandle());
    }

    void GameWindow::Present(const sf::Texture& frame)
    {
        if (!CanRender()) throw std::logic_error("Cannot present a frame to a closed or minimized window.");
        _window.clear();
        _window.draw(sf::Sprite(frame));
        _window.display();
    }
}
