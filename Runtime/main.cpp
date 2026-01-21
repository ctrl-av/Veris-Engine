#include "../Engine/Engine.h"
#include "Project.h"
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
    void RunFrames(sf::RenderWindow& window, Veris::Engine& engine, std::uint64_t frameLimit);
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
        engine.Initialize(project.WindowSize);
        sf::RenderWindow window(sf::VideoMode(project.WindowSize), sf::String::fromUtf8(project.Name.begin(), project.Name.end()), sf::Style::Titlebar | sf::Style::Close);
        window.setVerticalSyncEnabled(true);
        std::cout << "Loaded " << project.Name << " from " << project.ProjectFile.u8string() << '\n';
        RunFrames(window, engine, frameLimit);
        std::cout << "Completed " << engine.CompletedFrames() << " frames.\n";
        engine.Shutdown();
        return 0;
    }

    void RunFrames(sf::RenderWindow& window, Veris::Engine& engine, std::uint64_t frameLimit)
    {
        while (window.isOpen() && (frameLimit == 0 || engine.CompletedFrames() < frameLimit))
        {
            while (const auto event = window.pollEvent())
                if (event->is<sf::Event::Closed>()) window.close();
            if (!window.isOpen()) break;
            engine.Update();
            window.clear(Veris::Engine::BackgroundColor);
            window.draw(sf::Sprite(engine.GetFrame()));
            window.display();
        }
    }
}
