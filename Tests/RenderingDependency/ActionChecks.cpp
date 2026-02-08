#include "Checks.h"
#include "../../Engine/InputActions.h"

void VerifyActionAggregation()
{
    Veris::InputState input;
    Veris::InputActions actions;
    actions.SetBindings({{"Move", {sf::Keyboard::Key::W, sf::Keyboard::Key::Up}}});
    input.HandleEvent(sf::Event::KeyPressed{sf::Keyboard::Key::W});
    actions.Update(input);
    Require(actions.Get("Move").Pressed && actions.Get("Move").Down, "The first binding must press its action.");
    input.BeginFrame();
    input.HandleEvent(sf::Event::KeyPressed{sf::Keyboard::Key::Up});
    input.HandleEvent(sf::Event::KeyReleased{sf::Keyboard::Key::W});
    actions.Update(input);
    Require(actions.Get("Move").Down && !actions.Get("Move").Pressed && !actions.Get("Move").Released,
        "An alternate held binding must preserve the action without extra edges.");
    input.BeginFrame();
    input.HandleEvent(sf::Event::KeyReleased{sf::Keyboard::Key::Up});
    actions.Update(input);
    Require(actions.Get("Move").Released && !actions.Get("Move").Down, "The final binding release must release the action.");
}

void VerifyActionRebindingAndQuickTaps()
{
    Veris::InputState input;
    Veris::InputActions actions;
    actions.SetBindings({{"Fire", {sf::Mouse::Button::Left}}});
    input.HandleEvent(sf::Event::MouseButtonPressed{sf::Mouse::Button::Left, {0, 0}});
    input.HandleEvent(sf::Event::MouseButtonReleased{sf::Mouse::Button::Left, {0, 0}});
    actions.Update(input);
    Require(actions.Get("Fire").Pressed && actions.Get("Fire").Released && !actions.Get("Fire").Down, "Action mapping must preserve quick mouse taps.");
    Reject<std::invalid_argument>([&] { actions.SetBindings({{"Fire", {sf::Keyboard::Key::Unknown}}}); });
    Require(actions.Contains("Fire") && actions.Get("Fire").Pressed, "Invalid rebinding must preserve the active configuration.");
    Reject<std::invalid_argument>([&] { actions.SetBindings({{"Fire", {sf::Keyboard::Key::A, sf::Keyboard::Key::A}}}); });
    actions.SetBindings({{"Fire", {sf::Keyboard::Key::Space}}});
    input.HandleEvent(sf::Event::KeyPressed{sf::Keyboard::Key::Space});
    actions.Update(input);
    Require(actions.Get("Fire").Down && actions.Get("Fire").Pressed, "Rebinding must activate the replacement control.");
    input.HandleEvent(sf::Event::FocusLost{});
    actions.Update(input);
    Require(actions.Get("Fire").Released && !actions.Get("Fire").Down, "Focus loss must release mapped actions.");
}
