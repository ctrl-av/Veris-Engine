#pragma once
#include <box2d/box2d.h>
#include <SFML/System/Vector2.hpp>
#include <map>
#include <string>
#include <cstdint>

namespace Veris
{
    class Scene;
    struct Entity;
    enum class BodyType { Static, Kinematic, Dynamic };
    enum class ColliderShape { Box, Circle };

    struct PhysicsBody
    {
        BodyType Type = BodyType::Dynamic;
        ColliderShape Shape = ColliderShape::Box;
        sf::Vector2f HalfSize{16, 16};
        sf::Vector2f Offset{};
        float Radius = 16;
        sf::Vector2f Velocity{};
        float AngularVelocity = 0;
        float GravityScale = 0;
        float Density = 1;
        float Friction = 0.3f;
        float Restitution = 0;
        bool FixedRotation = true;
        bool Bullet = false;
        void Validate() const;
        bool SameSettings(const PhysicsBody& other) const;
    };

    std::string SerializePhysics(const PhysicsBody& body);
    PhysicsBody DeserializePhysics(const std::string& json);

    class PhysicsWorld
    {
    public:
        static constexpr float PixelsPerMeter = 100;
        void Synchronize(const Scene& scene);
        void Step(Scene& scene, double seconds);
        void SetGravity(sf::Vector2f acceleration);
        sf::Vector2f Gravity() const { const auto gravity = _world.GetGravity(); return {gravity.x, gravity.y}; }
        void ApplyImpulse(const Scene& scene, std::uint64_t entity, sf::Vector2f impulse);
        float Mass(const Scene& scene, std::uint64_t entity);
        void Clear() noexcept;
        std::size_t BodyCount() const { return _bodies.size(); }

    private:
        struct BodyBinding
        {
            b2Body* Body;
            PhysicsBody Configuration;
            sf::Vector2f Position;
            sf::Vector2f Scale;
            float Rotation;
        };
        b2Body* CreateBody(const Entity& entity);
        void SynchronizeBody(const Entity& entity);
        void WriteTransforms(Scene& scene);
        void CreateFixture(b2Body& body, const Entity& entity);
        b2World _world{b2Vec2{0, 9.81f}};
        std::map<std::uint64_t, BodyBinding> _bodies;
    };
}
