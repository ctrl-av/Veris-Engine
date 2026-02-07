#include "Checks.h"
#include "../../Engine/Input.h"

void VerifyKeyboardTransitions()
{
    Veris::InputState input;
    input.HandleEvent(sf::Event::KeyPressed{sf::Keyboard::Key::A});
    Require(input.Get(sf::Keyboard::Key::A).Pressed && input.Get(sf::Keyboard::Key::A).Down, "A new key press must set pressed and held state.");
    input.BeginFrame();
    input.HandleEvent(sf::Event::KeyPressed{sf::Keyboard::Key::A});
    Require(!input.Get(sf::Keyboard::Key::A).Pressed && input.Get(sf::Keyboard::Key::A).Down, "Repeated press events must not retrigger an edge.");
    input.HandleEvent(sf::Event::KeyReleased{sf::Keyboard::Key::A});
    Require(input.Get(sf::Keyboard::Key::A).Released && !input.Get(sf::Keyboard::Key::A).Down, "A release must end the held state.");
    input.BeginFrame();
    Require(!input.Get(sf::Keyboard::Key::A).Released, "Release edges must last one frame.");
    input.HandleEvent(sf::Event::KeyPressed{sf::Keyboard::Key::A});
    input.HandleEvent(sf::Event::KeyReleased{sf::Keyboard::Key::A});
    Require(input.Get(sf::Keyboard::Key::A).Pressed && input.Get(sf::Keyboard::Key::A).Released, "Quick taps must preserve both edges.");
    input.HandleEvent(sf::Event::KeyPressed{sf::Keyboard::Key::Unknown});
    Require(!input.Get(sf::Keyboard::Key::Unknown).Down, "Unhandled platform keys must remain inactive.");
}

void VerifyMouseAndFocus()
{
    Veris::InputState input;
    input.HandleEvent(sf::Event::MouseMoved{{10, 20}});
    input.HandleEvent(sf::Event::MouseMoved{{14, 25}});
    input.HandleEvent(sf::Event::MouseButtonPressed{sf::Mouse::Button::Left, {14, 25}});
    input.HandleEvent(sf::Event::MouseWheelScrolled{sf::Mouse::Wheel::Vertical, 0.5f, {14, 25}});
    Require(input.MouseDelta() == sf::Vector2i{4, 5} && input.WheelDelta().y == 0.5f, "Mouse movement and fractional wheel input must accumulate.");
    input.BeginFrame();
    Require(input.MouseDelta() == sf::Vector2i{} && input.WheelDelta() == sf::Vector2f{} && input.Get(sf::Mouse::Button::Left).Down,
        "Frame boundaries must clear deltas while preserving held buttons.");
    input.HandleEvent(sf::Event::MouseButtonReleased{sf::Mouse::Button::Left, {14, 25}});
    input.HandleEvent(sf::Event::MouseMovedRaw{{3, -2}});
    Require(input.Get(sf::Mouse::Button::Left).Released && input.RawMouseDelta() == sf::Vector2i{3, -2}, "Mouse releases and raw motion must be observable.");
}

void VerifyInputFocusLoss()
{
    Veris::InputState input;
    input.HandleEvent(sf::Event::MouseButtonPressed{sf::Mouse::Button::Left, {14, 25}});
    input.HandleEvent(sf::Event::KeyPressed{sf::Keyboard::Key::A});
    input.HandleEvent(sf::Event::FocusLost{});
    Require(!input.Get(sf::Mouse::Button::Left).Down && input.Get(sf::Mouse::Button::Left).Released, "Focus loss must release held buttons.");
    Require(!input.Get(sf::Keyboard::Key::A).Down && input.Get(sf::Keyboard::Key::A).Released, "Focus loss must release held keys.");
    input.HandleEvent(sf::Event::KeyPressed{sf::Keyboard::Key::B});
    Require(!input.Get(sf::Keyboard::Key::B).Down, "Unfocused windows must ignore gameplay presses.");
    input.HandleEvent(sf::Event::FocusGained{});
    input.HandleEvent(sf::Event::MouseMoved{{100, 100}});
    Require(input.MouseDelta() == sf::Vector2i{}, "Focus restoration must not produce a stale pointer jump.");
}
