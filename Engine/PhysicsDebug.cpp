#include "pch.h"
#include "Physics.h"
#include <SFML/Graphics/VertexArray.hpp>
#include <cmath>

namespace
{
    sf::Vector2f Pixels(b2Vec2 point) { return {point.x * Veris::PhysicsWorld::PixelsPerMeter, point.y * Veris::PhysicsWorld::PixelsPerMeter}; }
    void DrawSegment(sf::RenderTarget& target, sf::Vector2f start, sf::Vector2f end, sf::Color color)
    {
        const sf::Vertex vertices[]{{start, color}, {end, color}};
        target.draw(vertices, 2, sf::PrimitiveType::Lines);
    }
    void DrawFixture(sf::RenderTarget& target, const b2Body& body, const b2Fixture& fixture, sf::Color color)
    {
        sf::VertexArray outline(sf::PrimitiveType::LineStrip);
        if (fixture.GetType() == b2Shape::e_polygon)
        {
            const auto& polygon = *static_cast<const b2PolygonShape*>(fixture.GetShape());
            for (int index = 0; index <= polygon.m_count; ++index)
                outline.append({Pixels(body.GetWorldPoint(polygon.m_vertices[index % polygon.m_count])), color});
        }
        else if (fixture.GetType() == b2Shape::e_circle)
        {
            const auto& circle = *static_cast<const b2CircleShape*>(fixture.GetShape());
            for (int index = 0; index <= 48; ++index)
            {
                const auto angle = index * 2 * b2_pi / 48;
                outline.append({Pixels(body.GetWorldPoint(circle.m_p + circle.m_radius * b2Vec2{std::cos(angle), std::sin(angle)})), color});
            }
        }
        target.draw(outline);
    }
}

namespace Veris
{
    void PhysicsWorld::DrawDebug(sf::RenderTarget& target) const
    {
        for (const auto& entry : _bodies)
        {
            const auto& body = *entry.second.Body;
            for (auto fixture = body.GetFixtureList(); fixture; fixture = fixture->GetNext())
            {
                const auto color = fixture->IsSensor() ? sf::Color::Cyan : body.GetType() == b2_staticBody ? sf::Color::Green :
                    body.GetType() == b2_kinematicBody ? sf::Color{80, 160, 255} : sf::Color::Yellow;
                DrawFixture(target, body, *fixture, color);
            }
            const auto origin = Pixels(body.GetPosition());
            DrawSegment(target, origin, origin + Pixels(0.1f * body.GetLinearVelocity()), sf::Color::White);
        }
        for (const auto& entry : _contacts)
            if (entry.second.HasPoint) DrawSegment(target, entry.second.Point, entry.second.Point + entry.second.Normal * 16.f, sf::Color::Red);
    }
}
