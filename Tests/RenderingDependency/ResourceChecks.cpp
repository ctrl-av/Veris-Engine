#include "Checks.h"
#include "../../Engine/Engine.h"
#include <SFML/Graphics/Image.hpp>
#include <fstream>

void VerifyTextureResources(const std::filesystem::path& fixtureDirectory)
{
    std::filesystem::create_directories(fixtureDirectory);
    const auto texturePath = fixtureDirectory / "texture.png";
    Require(sf::Image({2, 2}, sf::Color::Red).saveToFile(texturePath), "Resource fixture must be saved.");
    Veris::TextureResources resources;
    Reject<std::logic_error>([&] { resources.Load("texture.png"); });
    resources.SetRoot(fixtureDirectory);
    auto texture = resources.Load("texture.png");
    Require(resources.Load("TEXTURE.PNG") == texture && resources.Count() == 1, "Windows path aliases must share one cached texture.");
    Reject<std::invalid_argument>([&] { resources.Load("../outside.png"); });
    Reject<std::invalid_argument>([&] { resources.Load(texturePath); });
    Reject<std::runtime_error>([&] { resources.Load("missing.png"); });
    Require(resources.Count() == 1, "Failed loads must not publish cache entries.");
    Require(sf::Image({2, 2}, sf::Color::Blue).saveToFile(texturePath), "Replacement fixture must be saved.");
    resources.Reload("texture.png");
    Require(resources.Load("texture.png") == texture && texture->copyToImage().getPixel({0, 0}) == sf::Color::Blue,
        "Reload must update existing resource handles without replacing their identity.");
    std::ofstream(texturePath, std::ios::binary | std::ios::trunc) << "invalid image";
    Reject<std::runtime_error>([&] { resources.Reload("texture.png"); });
    Require(texture->copyToImage().getPixel({0, 0}) == sf::Color::Blue, "Failed reloads must preserve the last good texture.");
    Require(resources.Unload("texture.png") && resources.Count() == 0, "Unload must remove cache ownership.");
    Require(texture->getSize() == sf::Vector2u{2, 2}, "Outstanding component references must survive cache eviction.");
    resources.Reset();
    Reject<std::logic_error>([&] { resources.Load("texture.png"); });
    std::filesystem::remove(texturePath);
    std::filesystem::remove(fixtureDirectory);
}
