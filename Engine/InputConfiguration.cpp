#include "pch.h"
#include "InputConfiguration.h"
#include "JsonValidation.h"
#include <nlohmann/json.hpp>
#include <Windows.h>
#include <fstream>
#include <stdexcept>
#include <unordered_map>

namespace
{
    const std::unordered_map<std::string, sf::Keyboard::Key> KeyNames{
        {"A", sf::Keyboard::Key::A},
        {"B", sf::Keyboard::Key::B},
        {"C", sf::Keyboard::Key::C},
        {"D", sf::Keyboard::Key::D},
        {"E", sf::Keyboard::Key::E},
        {"F", sf::Keyboard::Key::F},
        {"G", sf::Keyboard::Key::G},
        {"H", sf::Keyboard::Key::H},
        {"I", sf::Keyboard::Key::I},
        {"J", sf::Keyboard::Key::J},
        {"K", sf::Keyboard::Key::K},
        {"L", sf::Keyboard::Key::L},
        {"M", sf::Keyboard::Key::M},
        {"N", sf::Keyboard::Key::N},
        {"O", sf::Keyboard::Key::O},
        {"P", sf::Keyboard::Key::P},
        {"Q", sf::Keyboard::Key::Q},
        {"R", sf::Keyboard::Key::R},
        {"S", sf::Keyboard::Key::S},
        {"T", sf::Keyboard::Key::T},
        {"U", sf::Keyboard::Key::U},
        {"V", sf::Keyboard::Key::V},
        {"W", sf::Keyboard::Key::W},
        {"X", sf::Keyboard::Key::X},
        {"Y", sf::Keyboard::Key::Y},
        {"Z", sf::Keyboard::Key::Z},
        {"Num0", sf::Keyboard::Key::Num0},
        {"Num1", sf::Keyboard::Key::Num1},
        {"Num2", sf::Keyboard::Key::Num2},
        {"Num3", sf::Keyboard::Key::Num3},
        {"Num4", sf::Keyboard::Key::Num4},
        {"Num5", sf::Keyboard::Key::Num5},
        {"Num6", sf::Keyboard::Key::Num6},
        {"Num7", sf::Keyboard::Key::Num7},
        {"Num8", sf::Keyboard::Key::Num8},
        {"Num9", sf::Keyboard::Key::Num9},
        {"Escape", sf::Keyboard::Key::Escape},
        {"LControl", sf::Keyboard::Key::LControl},
        {"LShift", sf::Keyboard::Key::LShift},
        {"LAlt", sf::Keyboard::Key::LAlt},
        {"LSystem", sf::Keyboard::Key::LSystem},
        {"RControl", sf::Keyboard::Key::RControl},
        {"RShift", sf::Keyboard::Key::RShift},
        {"RAlt", sf::Keyboard::Key::RAlt},
        {"RSystem", sf::Keyboard::Key::RSystem},
        {"Menu", sf::Keyboard::Key::Menu},
        {"LBracket", sf::Keyboard::Key::LBracket},
        {"RBracket", sf::Keyboard::Key::RBracket},
        {"Semicolon", sf::Keyboard::Key::Semicolon},
        {"Comma", sf::Keyboard::Key::Comma},
        {"Period", sf::Keyboard::Key::Period},
        {"Apostrophe", sf::Keyboard::Key::Apostrophe},
        {"Slash", sf::Keyboard::Key::Slash},
        {"Backslash", sf::Keyboard::Key::Backslash},
        {"Grave", sf::Keyboard::Key::Grave},
        {"Equal", sf::Keyboard::Key::Equal},
        {"Hyphen", sf::Keyboard::Key::Hyphen},
        {"Space", sf::Keyboard::Key::Space},
        {"Enter", sf::Keyboard::Key::Enter},
        {"Backspace", sf::Keyboard::Key::Backspace},
        {"Tab", sf::Keyboard::Key::Tab},
        {"PageUp", sf::Keyboard::Key::PageUp},
        {"PageDown", sf::Keyboard::Key::PageDown},
        {"End", sf::Keyboard::Key::End},
        {"Home", sf::Keyboard::Key::Home},
        {"Insert", sf::Keyboard::Key::Insert},
        {"Delete", sf::Keyboard::Key::Delete},
        {"Add", sf::Keyboard::Key::Add},
        {"Subtract", sf::Keyboard::Key::Subtract},
        {"Multiply", sf::Keyboard::Key::Multiply},
        {"Divide", sf::Keyboard::Key::Divide},
        {"Left", sf::Keyboard::Key::Left},
        {"Right", sf::Keyboard::Key::Right},
        {"Up", sf::Keyboard::Key::Up},
        {"Down", sf::Keyboard::Key::Down},
        {"Numpad0", sf::Keyboard::Key::Numpad0},
        {"Numpad1", sf::Keyboard::Key::Numpad1},
        {"Numpad2", sf::Keyboard::Key::Numpad2},
        {"Numpad3", sf::Keyboard::Key::Numpad3},
        {"Numpad4", sf::Keyboard::Key::Numpad4},
        {"Numpad5", sf::Keyboard::Key::Numpad5},
        {"Numpad6", sf::Keyboard::Key::Numpad6},
        {"Numpad7", sf::Keyboard::Key::Numpad7},
        {"Numpad8", sf::Keyboard::Key::Numpad8},
        {"Numpad9", sf::Keyboard::Key::Numpad9},
        {"F1", sf::Keyboard::Key::F1},
        {"F2", sf::Keyboard::Key::F2},
        {"F3", sf::Keyboard::Key::F3},
        {"F4", sf::Keyboard::Key::F4},
        {"F5", sf::Keyboard::Key::F5},
        {"F6", sf::Keyboard::Key::F6},
        {"F7", sf::Keyboard::Key::F7},
        {"F8", sf::Keyboard::Key::F8},
        {"F9", sf::Keyboard::Key::F9},
        {"F10", sf::Keyboard::Key::F10},
        {"F11", sf::Keyboard::Key::F11},
        {"F12", sf::Keyboard::Key::F12},
        {"F13", sf::Keyboard::Key::F13},
        {"F14", sf::Keyboard::Key::F14},
        {"F15", sf::Keyboard::Key::F15},
        {"Pause", sf::Keyboard::Key::Pause}};
    const std::unordered_map<std::string, sf::Mouse::Button> MouseNames{
        {"Left", sf::Mouse::Button::Left}, {"Right", sf::Mouse::Button::Right}, {"Middle", sf::Mouse::Button::Middle},
        {"Extra1", sf::Mouse::Button::Extra1}, {"Extra2", sf::Mouse::Button::Extra2}};
    const Veris::ActionBindings DefaultBindings{
        {"Quit", {sf::Keyboard::Key::Escape}}, {"MoveUp", {sf::Keyboard::Key::W, sf::Keyboard::Key::Up}},
        {"MoveDown", {sf::Keyboard::Key::S, sf::Keyboard::Key::Down}}, {"MoveLeft", {sf::Keyboard::Key::A, sf::Keyboard::Key::Left}},
        {"MoveRight", {sf::Keyboard::Key::D, sf::Keyboard::Key::Right}}, {"Fire", {sf::Mouse::Button::Left}}};
    Veris::ActionBindings ReadConfiguration(const nlohmann::json& document);
}

void Veris::LoadInputConfiguration(const std::filesystem::path& projectDirectory, InputActions& actions)
{
    const auto configurationFile = projectDirectory / ".Veris" / "input-actions.json";
    if (!std::filesystem::exists(configurationFile)) { actions.SetBindings(DefaultBindings); return; }
    for (const auto& path : {configurationFile.parent_path(), configurationFile})
    {
        const auto attributes = GetFileAttributesW(path.c_str());
        if (attributes == INVALID_FILE_ATTRIBUTES || (attributes & FILE_ATTRIBUTE_REPARSE_POINT))
            throw std::runtime_error("Input configuration paths must be accessible and cannot be linked.");
    }
    std::ifstream configurationStream(configurationFile, std::ios::binary);
    if (!configurationStream) throw std::runtime_error("Cannot read the input configuration file.");
    if (std::filesystem::file_size(configurationFile) > 1024 * 1024) throw std::invalid_argument("Input configuration cannot exceed 1 MiB.");
    const std::string json((std::istreambuf_iterator<char>(configurationStream)), std::istreambuf_iterator<char>());
    actions.SetBindings(ReadConfiguration(Json::Parse(json)));
}

namespace
{
    Veris::InputBinding ReadBinding(const std::string& binding)
    {
        const auto separator = binding.find(':');
        const auto device = binding.substr(0, separator);
        const auto name = separator == std::string::npos ? std::string{} : binding.substr(separator + 1);
        if (device == "Key" && KeyNames.find(name) != KeyNames.end()) return KeyNames.at(name);
        if (device == "Mouse" && MouseNames.find(name) != MouseNames.end()) return MouseNames.at(name);
        throw std::runtime_error("Unknown input binding: " + binding);
    }

    std::vector<Veris::InputBinding> ReadBindings(const nlohmann::json& buttons)
    {
        if (!buttons.is_array()) throw std::runtime_error("Action bindings must be arrays.");
        std::vector<Veris::InputBinding> bindings;
        for (const auto& button : buttons)
        {
            if (!button.is_string()) throw std::runtime_error("Action bindings must be strings.");
            bindings.push_back(ReadBinding(button.get<std::string>()));
        }
        return bindings;
    }

    Veris::ActionBindings ReadConfiguration(const nlohmann::json& document)
    {
        if (!document.is_object() || document.size() != 2 || !document.contains("formatVersion") || !document.contains("actions"))
            throw std::runtime_error("Input configuration requires formatVersion and actions fields.");
        if (!document.at("formatVersion").is_number_integer() || document.at("formatVersion") != 1 || !document.at("actions").is_object())
            throw std::runtime_error("Unsupported input configuration version or invalid actions object.");
        Veris::ActionBindings bindings;
        for (auto action = document.at("actions").begin(); action != document.at("actions").end(); ++action)
            bindings.emplace(action.key(), ReadBindings(action.value()));
        return bindings;
    }
}

void Veris::SetNamedInput(InputState& input, const std::string& binding, bool down)
{
    if (binding.rfind("Key:", 0) == 0)
    {
        const auto key = KeyNames.at(binding.substr(4));
        if (down) { sf::Event::KeyPressed event{}; event.code = key; input.HandleEvent(event); }
        else { sf::Event::KeyReleased event{}; event.code = key; input.HandleEvent(event); }
        return;
    }
    if (binding.rfind("Mouse:", 0) != 0) throw std::invalid_argument("Input device must be Key or Mouse.");
    const auto button = MouseNames.at(binding.substr(6));
    if (down) { sf::Event::MouseButtonPressed event{}; event.button = button; input.HandleEvent(event); }
    else { sf::Event::MouseButtonReleased event{}; event.button = button; input.HandleEvent(event); }
}

void Veris::ValidateInputConfiguration(std::string_view json)
{
    if (json.size() > 1024 * 1024) throw std::invalid_argument("Input configuration cannot exceed 1 MiB.");
    InputActions candidate;
    candidate.SetBindings(ReadConfiguration(Json::Parse(json)));
}
