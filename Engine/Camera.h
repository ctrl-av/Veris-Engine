#pragma once

#include <SFML/Graphics/View.hpp>

namespace Veris
{
    class Camera
    {
    public:
        void Resize(sf::Vector2u viewportSize);
        void SetCenter(sf::Vector2f center);
        void SetZoom(float zoom);
        void SetRotation(float degrees);
        const sf::View& View() const noexcept { return _view; }
        float Zoom() const noexcept { return _zoom; }

    private:
        sf::View _view;
        sf::Vector2u _viewportSize{1, 1};
        float _zoom = 1;
        bool _followViewport = true;
    };
}
