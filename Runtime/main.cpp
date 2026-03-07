#include "../Engine/Engine.h"
#include "../Engine/FrameClock.h"
#include "Project.h"
#include "InputConfiguration.h"
#include <SFML/Graphics.hpp>
#include <algorithm>
#include <iostream>
#include <stdexcept>
#include <string_view>
#include <utility>

namespace
{
    std::pair<std::filesystem::path, std::uint64_t> ReadArguments(int count, wchar_t* arguments[]);
    int RunProject(const Veris::RuntimeProject& project, std::uint64_t frameLimit);
    void RunFrames(Veris::GameWindow& window, Veris::Engine& engine, std::uint64_t frameLimit);
}

int wmain(int count, wchar_t* arguments[])
{
    try
    {
        if (count == 2 && std::wstring_view(arguments[1]) == L"--help")
        {
            std::cout << "Usage: VerisRuntime.exe <project.veris> [--frames positive-count]\n";
            return 0;
        }
        const auto [projectFile, frameLimit] = ReadArguments(count, arguments);
        return RunProject(Veris::LoadProject(projectFile), frameLimit);
    }
    catch (const std::exception& exception)
    {
        std::cerr << "Veris runtime error: " << exception.what() << '\n';
        return 1;
    }
}

namespace
{
    std::pair<std::filesystem::path, std::uint64_t> ReadArguments(int count, wchar_t* arguments[])
    {
        if (count != 2 && count != 4) throw std::runtime_error("Usage: VerisRuntime.exe <project.veris> [--frames positive-count]");
        if (count == 2) return {arguments[1], 0};
        const std::wstring frameText(arguments[3]);
        if (std::wstring_view(arguments[2]) != L"--frames" || frameText.empty() ||
            !std::all_of(frameText.begin(), frameText.end(), [](wchar_t character) { return character >= L'0' && character <= L'9'; }))
            throw std::runtime_error("--frames requires a positive integer.");
        const auto frameLimit = std::stoull(frameText);
        if (frameLimit == 0) throw std::runtime_error("--frames requires a positive integer.");
        return {arguments[1], frameLimit};
    }

    int RunProject(const Veris::RuntimeProject& project, std::uint64_t frameLimit)
    {
        Veris::Engine engine;
        engine.Initialize(project.Window.Size);
        engine.Textures().SetRoot(project.ContentDirectory);
        Veris::LoadInputConfiguration(project.ProjectFile.parent_path(), engine.Actions());
        Veris::GameWindow window(project.Window, project.Name);
        std::cout << "Loaded " << project.Name << " from " << project.ProjectFile.u8string() << '\n';
        RunFrames(window, engine, frameLimit);
        std::cout << "Completed " << engine.CompletedFrames() << " frames.\n";
        engine.Shutdown();
        return 0;
    }

    void RunFrames(Veris::GameWindow& window, Veris::Engine& engine, std::uint64_t frameLimit)
    {
        Veris::FrameClock clock;
        const auto fixedUpdate = [&engine](double step) { engine.FixedUpdate(step); };
        if (!window.HasFocus()) engine.Input().HandleEvent(sf::Event::FocusLost{});
        while (window.IsOpen() && (frameLimit == 0 || engine.CompletedFrames() < frameLimit))
        {
            engine.Input().BeginFrame();
            while (const auto event = window.PollEvent()) engine.Input().HandleEvent(*event);
            engine.Actions().Update(engine.Input());
            if (engine.Actions().Contains("Quit") && engine.Actions().Get("Quit").Pressed) window.Close();
            if (!window.IsOpen()) break;
            if (!window.HasFocus() || !window.CanRender()) clock.Pause(); else clock.Resume();
            if (!window.CanRender()) { sf::sleep(sf::milliseconds(10)); continue; }
            clock.Tick(fixedUpdate);
            engine.Resize(window.ViewportSize());
            engine.Update();
            window.Present(engine.GetFrame());
        }
    }
}
