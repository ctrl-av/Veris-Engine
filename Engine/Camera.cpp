#include "pch.h"
#include "Camera.h"
#include <cmath>
#include <stdexcept>

namespace Veris
{
    void Camera::Resize(sf::Vector2u viewportSize)
    {
        if (viewportSize.x == 0 || viewportSize.y == 0) throw std::invalid_argument("Camera viewport dimensions must be positive.");
        _viewportSize = viewportSize;
        _view.setSize(sf::Vector2f(viewportSize) / _zoom);
        if (_followViewport) _view.setCenter(sf::Vector2f(viewportSize) / 2.f);
    }

    void Camera::SetCenter(sf::Vector2f center)
    {
        if (!std::isfinite(center.x) || !std::isfinite(center.y)) throw std::invalid_argument("Camera center must be finite.");
        _view.setCenter(center);
        _followViewport = false;
    }

    void Camera::SetZoom(float zoom)
    {
        if (!std::isfinite(zoom) || zoom < 0.001f || zoom > 1000.f) throw std::invalid_argument("Camera zoom must be between 0.001 and 1000.");
        _zoom = zoom;
        _view.setSize(sf::Vector2f(_viewportSize) / zoom);
    }

    void Camera::SetRotation(float degrees)
    {
        if (!std::isfinite(degrees)) throw std::invalid_argument("Camera rotation must be finite.");
        _view.setRotation(sf::degrees(degrees));
    }
}
