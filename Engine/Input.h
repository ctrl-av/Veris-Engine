#pragma once

#include <SFML/Window/Event.hpp>
#include <array>

namespace Veris
{
    struct ButtonState
    {
        bool Down = false;
        bool Pressed = false;
        bool Released = false;
    };

    class InputState
    {
    public:
        void BeginFrame();
        void HandleEvent(const sf::Event& event);
        const ButtonState& Get(sf::Keyboard::Key key) const;
        const ButtonState& Get(sf::Mouse::Button button) const;
        sf::Vector2i MousePosition() const { return _mousePosition; }
        sf::Vector2i MouseDelta() const { return _mouseDelta; }
        sf::Vector2i RawMouseDelta() const { return _rawMouseDelta; }
        sf::Vector2f WheelDelta() const { return _wheelDelta; }
        bool HasFocus() const { return _focused; }

    private:
        void HandleKeys(const sf::Event& event);
        void HandleMouseButtons(const sf::Event& event);
        void HandlePointer(const sf::Event& event);
        void SetMousePosition(sf::Vector2i position);
        std::array<ButtonState, sf::Keyboard::KeyCount> _keys{};
        std::array<ButtonState, sf::Mouse::ButtonCount> _mouse{};
        sf::Vector2i _mousePosition{};
        sf::Vector2i _mouseDelta{};
        sf::Vector2i _rawMouseDelta{};
        sf::Vector2f _wheelDelta{};
        bool _focused = true;
        bool _hasMousePosition = false;
    };
}
