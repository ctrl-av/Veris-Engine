#include "Checks.h"
#include "../../Engine/Engine.h"
#include <SFML/Graphics.hpp>
#include <iostream>
#include <stdexcept>

void Require(bool condition, const char* message)
{
    if (!condition) throw std::runtime_error(message);
}

void VerifyGraphicsDependency()
{
    const sf::RectangleShape rectangle({8.f, 4.f});
    const sf::Image source({2, 2}, sf::Color::Red);
    sf::Image destination({4, 4}, sf::Color::Black);
    Require(destination.copy(source, {1, 1}) && destination.getPixel({1, 1}) == sf::Color::Red &&
        destination.getPixel({0, 0}) == sf::Color::Black && rectangle.getLocalBounds().size == sf::Vector2f{8.f, 4.f},
        "SFML graphics dependency check failed.");
}

void VerifyEngineLifecycle()
{
    Veris::Engine engine;
    Reject<std::logic_error>([&] { engine.Update(); });
    Reject<std::logic_error>([&] { engine.GetFrame(); });
    Reject<std::invalid_argument>([&] { engine.Initialize({0, 8}); });
    Require(!engine.IsInitialized(), "Failed initialization must leave the engine stopped.");
    engine.Initialize({8, 8});
    engine.Update();
    Require(engine.CompletedFrames() == 1 && engine.GetFrame().getSize() == sf::Vector2u{8, 8}, "Update must produce a frame.");
    Require(engine.GetFrame().copyToImage().getPixel({0, 0}) == Veris::Engine::BackgroundColor, "Update must clear the render target.");
    Reject<std::logic_error>([&] { engine.Initialize({16, 16}); });
    Require(engine.CompletedFrames() == 1, "Duplicate initialization must preserve the active session.");
    engine.Shutdown();
    engine.Shutdown();
    Require(!engine.IsInitialized() && engine.CompletedFrames() == 0, "Shutdown must release state.");
    Reject<std::logic_error>([&] { engine.Update(); });
    engine.Initialize({4, 4});
    engine.Update();
    Require(engine.CompletedFrames() == 1, "Reinitialization must begin a new session.");
}

int wmain(int, wchar_t* arguments[])
{
    try
    {
        VerifyScripts(std::filesystem::absolute(arguments[0]).parent_path() / "Script fixtures");
        VerifyGameplayApi(std::filesystem::absolute(arguments[0]).parent_path() / "Gameplay fixtures");
        VerifyAnimations();
        VerifyPhysics();
        VerifyContacts(std::filesystem::absolute(arguments[0]).parent_path() / "physics-debug.png");
        VerifyAudio(std::filesystem::absolute(arguments[0]).parent_path() / "Audio fixtures");
        VerifyTextureResources(std::filesystem::absolute(arguments[0]).parent_path() / L"Resource fixtures" );
        VerifyEntityPicking();
        VerifySceneHierarchy();
        VerifySceneRendering();
        VerifyCameras();
        VerifyGraphicsDependency();
        VerifyEngineLifecycle();
        VerifyWindowManagement();
        VerifyFrameTiming();
        VerifyKeyboardTransitions();
        VerifyMouseAndFocus();
        VerifyInputFocusLoss();
        VerifyActionAggregation();
        VerifyActionRebindingAndQuickTaps();
        VerifySpriteRendering(std::filesystem::absolute(arguments[0]).parent_path() / "sprite-rendering.png");
        std::cout << "PASS: Native lifecycle, rendering, resources, animation, audio, Box2D simulation, contacts, triggers, and debug geometry.\n";
        return 0;
    }
    catch (const std::exception& exception)
    {
        std::cerr << exception.what() << '\n';
        return 1;
    }
}
