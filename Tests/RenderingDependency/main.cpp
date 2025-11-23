#include <SFML/Graphics.hpp>
#include <iostream>
#include <stdexcept>

int main()
{
    try
    {
        const sf::RectangleShape rectangle({8.f, 4.f});
        const sf::Image source({2, 2}, sf::Color::Red);
        sf::Image destination({4, 4}, sf::Color::Black);
        if (!destination.copy(source, {1, 1}) || destination.getPixel({1, 1}) != sf::Color::Red ||
            destination.getPixel({0, 0}) != sf::Color::Black || rectangle.getLocalBounds().size != sf::Vector2f{8.f, 4.f})
        {
            throw std::runtime_error("SFML graphics dependency smoke check failed.");
        }
        std::cout << "PASS: SFML graphics headers, import libraries, and runtime DLLs work.\n";
        return 0;
    }
    catch (const std::exception& exception)
    {
        std::cerr << exception.what() << '\n';
        return 1;
    }
}
