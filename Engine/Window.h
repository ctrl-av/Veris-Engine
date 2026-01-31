#pragma once

#include <SFML/Graphics/RenderWindow.hpp>
#include <SFML/Window/Event.hpp>
#include <optional>
#include <string>

namespace Veris
{
    enum class WindowMode { Windowed, Fullscreen };

    struct WindowSettings
    {
        sf::Vector2u Size{1280, 720};
        WindowMode Mode = WindowMode::Windowed;
        bool Resizable = true;
        bool VerticalSync = true;
    };

    class GameWindow
    {
    public:
        GameWindow(const WindowSettings& settings, const std::string& title);
        std::optional<sf::Event> PollEvent();
        void HandleEvent(const sf::Event& event);
        void Resize(sf::Vector2u size);
        void Present(const sf::Texture& frame);
        void Close() { _window.close(); }
        void SetVisible(bool visible) { _window.setVisible(visible); }
        bool IsOpen() const { return _window.isOpen(); }
        bool HasFocus() const { return _focused; }
        bool CanRender() const;
        sf::Vector2u ViewportSize() const { return _viewportSize; }

    private:
        sf::RenderWindow _window;
        sf::Vector2u _viewportSize;
        bool _focused = false;
        bool _drawable = true;
    };
}
