#pragma once
#include <box2d/box2d.h>
#include <SFML/System/Vector2.hpp>
#include <map>
#include <string>
#include <cstdint>
#include <set>
#include <vector>
#include <SFML/Graphics/RenderTarget.hpp>

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
        bool Sensor = false;
        std::uint16_t Category = 1;
        std::uint16_t Mask = 65535;
        std::int16_t Group = 0;
        void Validate() const;
        bool SameSettings(const PhysicsBody& other) const;
    };

    std::string SerializePhysics(const PhysicsBody& body);
    PhysicsBody DeserializePhysics(const std::string& json);

    enum class ContactPhase { Begin, Stay, End };
    struct PhysicsContact
    {
        std::uint64_t First;
        std::uint64_t Second;
        ContactPhase Phase;
        bool Trigger;
        sf::Vector2f Normal{};
        sf::Vector2f Point{};
        bool HasPoint = false;
    };

    class PhysicsWorld : private b2ContactListener
    {
    public:
        PhysicsWorld();
        ~PhysicsWorld();
        static constexpr float PixelsPerMeter = 100;
        void Synchronize(const Scene& scene);
        void Step(Scene& scene, double seconds);
        void SetGravity(sf::Vector2f acceleration);
        sf::Vector2f Gravity() const { const auto gravity = _world.GetGravity(); return {gravity.x, gravity.y}; }
        void ApplyImpulse(const Scene& scene, std::uint64_t entity, sf::Vector2f impulse);
        float Mass(const Scene& scene, std::uint64_t entity);
        void Clear() noexcept;
        std::size_t BodyCount() const { return _bodies.size(); }
        const std::vector<PhysicsContact>& Contacts() const { return _events; }
        std::string ContactsJson() const;
        void DrawDebug(sf::RenderTarget& target) const;

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
        void BeginContact(b2Contact* contact) override;
        void EndContact(b2Contact* contact) override;
        void RefreshContacts();
        PhysicsContact DescribeContact(b2Contact& contact) const;
        using ContactPair = std::pair<std::uint64_t, std::uint64_t>;
        b2World _world{b2Vec2{0, 9.81f}};
        std::map<std::uint64_t, BodyBinding> _bodies;
        std::map<ContactPair, PhysicsContact> _contacts;
        std::set<ContactPair> _began;
        std::vector<PhysicsContact> _events;
    };
}
