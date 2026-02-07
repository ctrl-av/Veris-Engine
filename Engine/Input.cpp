#include "pch.h"
#include "Input.h"

namespace
{
    const Veris::ButtonState UnhandledKey{};
    void Press(Veris::ButtonState& button);
    void Release(Veris::ButtonState& button);
    template<std::size_t Count> void BeginButtonsFrame(std::array<Veris::ButtonState, Count>& buttons);
    template<std::size_t Count> void ReleaseButtons(std::array<Veris::ButtonState, Count>& buttons);
}

namespace Veris
{
    void InputState::BeginFrame()
    {
        BeginButtonsFrame(_keys);
        BeginButtonsFrame(_mouse);
        _mouseDelta = {};
        _rawMouseDelta = {};
        _wheelDelta = {};
    }

    void InputState::HandleEvent(const sf::Event& event)
    {
        if (event.is<sf::Event::FocusGained>()) _focused = true;
        if (event.is<sf::Event::FocusLost>())
        {
            _focused = false;
            ReleaseButtons(_keys);
            ReleaseButtons(_mouse);
            _mouseDelta = {}; _rawMouseDelta = {}; _wheelDelta = {};
            _hasMousePosition = false;
        }
        if (!_focused) return;
        HandleKeys(event);
        HandleMouseButtons(event);
        HandlePointer(event);
    }

    const ButtonState& InputState::Get(sf::Keyboard::Key key) const
    {
        if (key == sf::Keyboard::Key::Unknown) return UnhandledKey;
        return _keys.at(static_cast<std::size_t>(key));
    }

    const ButtonState& InputState::Get(sf::Mouse::Button button) const
    {
        return _mouse.at(static_cast<std::size_t>(button));
    }

    void InputState::HandleKeys(const sf::Event& event)
    {
        if (const auto key = event.getIf<sf::Event::KeyPressed>(); key && key->code != sf::Keyboard::Key::Unknown)
            Press(_keys.at(static_cast<std::size_t>(key->code)));
        if (const auto key = event.getIf<sf::Event::KeyReleased>(); key && key->code != sf::Keyboard::Key::Unknown)
            Release(_keys.at(static_cast<std::size_t>(key->code)));
    }

    void InputState::HandleMouseButtons(const sf::Event& event)
    {
        if (const auto mouse = event.getIf<sf::Event::MouseButtonPressed>())
        {
            SetMousePosition(mouse->position);
            Press(_mouse.at(static_cast<std::size_t>(mouse->button)));
        }
        if (const auto mouse = event.getIf<sf::Event::MouseButtonReleased>())
        {
            SetMousePosition(mouse->position);
            Release(_mouse.at(static_cast<std::size_t>(mouse->button)));
        }
    }

    void InputState::HandlePointer(const sf::Event& event)
    {
        if (const auto move = event.getIf<sf::Event::MouseMoved>()) SetMousePosition(move->position);
        if (const auto raw = event.getIf<sf::Event::MouseMovedRaw>()) _rawMouseDelta += raw->delta;
        if (const auto wheel = event.getIf<sf::Event::MouseWheelScrolled>())
        {
            SetMousePosition(wheel->position);
            if (wheel->wheel == sf::Mouse::Wheel::Vertical) _wheelDelta.y += wheel->delta;
            else _wheelDelta.x += wheel->delta;
        }
    }

    void InputState::SetMousePosition(sf::Vector2i position)
    {
        if (_hasMousePosition) _mouseDelta += position - _mousePosition;
        _mousePosition = position;
        _hasMousePosition = true;
    }
}

namespace
{
    void Press(Veris::ButtonState& button)
    {
        button.Pressed |= !button.Down;
        button.Down = true;
    }

    void Release(Veris::ButtonState& button)
    {
        button.Released |= button.Down;
        button.Down = false;
    }

    template<std::size_t Count> void BeginButtonsFrame(std::array<Veris::ButtonState, Count>& buttons)
    {
        for (auto& button : buttons) { button.Pressed = false; button.Released = false; }
    }

    template<std::size_t Count> void ReleaseButtons(std::array<Veris::ButtonState, Count>& buttons)
    {
        for (auto& button : buttons) { Release(button); button.Pressed = false; }
    }
}
