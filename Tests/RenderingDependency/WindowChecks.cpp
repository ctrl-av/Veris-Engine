#include "Checks.h"
#include "../../Engine/Engine.h"
#include "../../Engine/Window.h"

namespace { void VerifyWindowFocusAndRestore(Veris::GameWindow& window, Veris::Engine& engine); }

void VerifyWindowManagement()
{
    Veris::WindowSettings settings{{32, 32}, Veris::WindowMode::Windowed, true, false};
    Veris::GameWindow window(settings, "Veris window checks");
    window.SetVisible(false);
    while (window.PollEvent()) { }
    window.Resize({48, 40});
    Veris::Engine engine;
    engine.Initialize({32, 32});
    engine.Update();
    engine.Resize(window.ViewportSize());
    Require(engine.GetFrame().getSize() == sf::Vector2u{48, 40}, "Window resize must resize the rendered viewport.");
    Require(engine.CompletedFrames() == 1, "Resizing must preserve the active engine session.");
    window.Present(engine.GetFrame());
    VerifyWindowFocusAndRestore(window, engine);
}

namespace
{
    void VerifyWindowFocusAndRestore(Veris::GameWindow& window, Veris::Engine& engine)
    {
        window.HandleEvent(sf::Event::FocusLost{});
        Require(!window.HasFocus(), "Focus loss must update window state.");
        window.HandleEvent(sf::Event::FocusGained{});
        Require(window.HasFocus(), "Focus gain must update window state.");
        window.HandleEvent(sf::Event::Resized{{0, 0}});
        Require(!window.CanRender(), "Zero-sized windows must suspend presentation.");
        Reject<std::logic_error>([&] { window.Present(engine.GetFrame()); });
        window.HandleEvent(sf::Event::Resized{{48, 40}});
        Require(window.CanRender(), "Restoring a window must resume presentation.");
        window.HandleEvent(sf::Event::Closed{});
        Require(!window.IsOpen(), "Close events must release the native window.");
        Reject<std::logic_error>([&] { window.Resize({48, 40}); });
    }
}
