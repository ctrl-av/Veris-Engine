#pragma once

#include "Input.h"
#include <map>
#include <string>
#include <variant>
#include <vector>

namespace Veris
{
    using InputBinding = std::variant<sf::Keyboard::Key, sf::Mouse::Button>;
    using ActionBindings = std::map<std::string, std::vector<InputBinding>>;

    class InputActions
    {
    public:
        void SetBindings(ActionBindings bindings);
        void Update(const InputState& input);
        bool Contains(const std::string& name) const { return _states.find(name) != _states.end(); }
        const ButtonState& Get(const std::string& name) const { return _states.at(name); }

    private:
        ActionBindings _bindings;
        std::map<std::string, ButtonState> _states;
    };
}
