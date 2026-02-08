#include "pch.h"
#include "InputActions.h"
#include <algorithm>
#include <set>
#include <stdexcept>

namespace Veris
{
    void InputActions::SetBindings(ActionBindings bindings)
    {
        for (const auto& [name, buttons] : bindings)
        {
            if (name.empty() || !std::all_of(name.begin(), name.end(), [](char character) {
                return (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z') ||
                    (character >= '0' && character <= '9') || character == '_' || character == '-'; }))
                throw std::invalid_argument("Action names must contain ASCII letters, digits, underscores, or hyphens.");
            if (buttons.empty()) throw std::invalid_argument("Actions must contain at least one input binding.");
            std::set<InputBinding> unique;
            for (const auto& button : buttons)
                if (!unique.insert(button).second || (std::holds_alternative<sf::Keyboard::Key>(button) && std::get<sf::Keyboard::Key>(button) == sf::Keyboard::Key::Unknown))
                    throw std::invalid_argument("Action contains a duplicate or unhandled input binding.");
        }
        _bindings = std::move(bindings);
        _states.clear();
        for (const auto& action : _bindings) _states.emplace(action.first, ButtonState{});
    }

    void InputActions::Update(const InputState& input)
    {
        for (const auto& [name, buttons] : _bindings)
        {
            bool down = false, pressed = false, released = false;
            for (const auto& binding : buttons)
            {
                const auto& button = std::visit([&input](auto buttonCode) -> const ButtonState& { return input.Get(buttonCode); }, binding);
                down |= button.Down;
                pressed |= button.Pressed;
                released |= button.Released;
            }
            auto& action = _states.at(name);
            const bool previouslyDown = action.Down;
            action.Down = down;
            action.Pressed = !previouslyDown && (down || pressed);
            action.Released = (previouslyDown && !down) || (!previouslyDown && !down && pressed && released);
        }
    }
}
