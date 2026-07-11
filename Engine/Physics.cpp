#include "pch.h"
#include "Physics.h"
#include "Scene.h"
#include "JsonValidation.h"
#include <cmath>

namespace
{
    constexpr float DegreesToRadians = b2_pi / 180;
    void ValidateEntity(const Veris::Entity& entity)
    {
        entity.Physics->Validate();
        entity.Transform.Matrix();
        const auto scale = entity.Transform.Scale;
        if (entity.Parent() != 0 || scale.x <= 0 || scale.y <= 0 ||
            (entity.Physics->Shape == Veris::ColliderShape::Circle && scale.x != scale.y))
            throw std::invalid_argument("Physics bodies require root entities and positive scale; circles require uniform scale.");
        const auto& shape = *entity.Physics;
        const auto width = (shape.Shape == Veris::ColliderShape::Box ? shape.HalfSize.x : shape.Radius) * scale.x;
        const auto height = (shape.Shape == Veris::ColliderShape::Box ? shape.HalfSize.y : shape.Radius) * scale.y;
        if (!std::isfinite(width) || !std::isfinite(height) || width < 0.1f || height < 0.1f || width > 1000000 || height > 1000000 ||
            std::abs(entity.Transform.Position.x) > 100000000 || std::abs(entity.Transform.Position.y) > 100000000 ||
            !std::isfinite(shape.Offset.x * scale.x) || !std::isfinite(shape.Offset.y * scale.y) ||
            std::abs(shape.Offset.x * scale.x) > 1000000 || std::abs(shape.Offset.y * scale.y) > 1000000)
            throw std::invalid_argument("Scaled collider dimensions must be 0.1–1000000 pixels; offsets and world positions exceed the supported physics range.");
    }
    sf::Vector2f ReadPhysicsVector(const Veris::Json::Document& vector)
    {
        if (!vector.is_array() || vector.size() != 2) throw std::invalid_argument("Physics vectors require two numbers.");
        return {Veris::Json::Number(vector[0]), Veris::Json::Number(vector[1])};
    }
}

namespace Veris
{
    void PhysicsWorld::Step(Scene& scene, double seconds)
    {
        if (!std::isfinite(seconds) || seconds <= 0) throw std::invalid_argument("Physics steps must be finite and positive.");
        _events.clear();
        _began.clear();
        Synchronize(scene);
        if (_bodies.empty()) return;
        if (seconds > 0.25) throw std::invalid_argument("Physics steps cannot exceed 250 ms; use fixed updates.");
        const auto substeps = static_cast<int>(std::ceil(seconds * 60));
        for (int index = 0; index < substeps; ++index) _world.Step(static_cast<float>(seconds / substeps), 8, 3);
        RefreshContacts();
        WriteTransforms(scene);
    }

    void PhysicsWorld::WriteTransforms(Scene& scene)
    {
        for (auto& entry : _bodies)
        {
            auto& entity = scene.Get(entry.first);
            const auto position = entry.second.Body->GetPosition();
            const auto velocity = entry.second.Body->GetLinearVelocity();
            entity.Transform.Position = {position.x * PixelsPerMeter, position.y * PixelsPerMeter};
            entity.Transform.Rotation = entry.second.Body->GetAngle() / DegreesToRadians;
            entity.Physics->Velocity = {velocity.x * PixelsPerMeter, velocity.y * PixelsPerMeter};
            entity.Physics->AngularVelocity = entry.second.Body->GetAngularVelocity() / DegreesToRadians;
            entry.second.Configuration = *entity.Physics;
            entry.second.Position = entity.Transform.Position;
            entry.second.Rotation = entity.Transform.Rotation;
        }
    }

    void PhysicsWorld::Synchronize(const Scene& scene)
    {
        for (const auto& entry : scene.Entities()) if (entry.second.Physics) ValidateEntity(entry.second);
        for (auto binding = _bodies.begin(); binding != _bodies.end();)
        {
            if (!scene.Contains(binding->first) || !scene.Get(binding->first).Physics)
            { _world.DestroyBody(binding->second.Body); binding = _bodies.erase(binding); }
            else ++binding;
        }
        for (const auto& entry : scene.Entities())
            if (entry.second.Physics) SynchronizeBody(entry.second);
    }

    void PhysicsWorld::SynchronizeBody(const Entity& entity)
    {
        auto found = _bodies.find(entity.Id);
        if (found != _bodies.end() && (!found->second.Configuration.SameSettings(*entity.Physics) || found->second.Scale != entity.Transform.Scale))
        { _world.DestroyBody(found->second.Body); _bodies.erase(found); found = _bodies.end(); }
        if (found == _bodies.end())
        {
            _bodies.emplace(entity.Id, BodyBinding{CreateBody(entity), *entity.Physics, entity.Transform.Position, entity.Transform.Scale, entity.Transform.Rotation});
            return;
        }
        auto& binding = found->second;
        if (binding.Position != entity.Transform.Position || binding.Rotation != entity.Transform.Rotation)
            binding.Body->SetTransform({entity.Transform.Position.x / PixelsPerMeter, entity.Transform.Position.y / PixelsPerMeter}, entity.Transform.Rotation * DegreesToRadians);
        if (binding.Configuration.Velocity != entity.Physics->Velocity)
            binding.Body->SetLinearVelocity({entity.Physics->Velocity.x / PixelsPerMeter, entity.Physics->Velocity.y / PixelsPerMeter});
        if (binding.Configuration.AngularVelocity != entity.Physics->AngularVelocity) binding.Body->SetAngularVelocity(entity.Physics->AngularVelocity * DegreesToRadians);
        binding.Configuration = *entity.Physics;
        binding.Position = entity.Transform.Position;
        binding.Rotation = entity.Transform.Rotation;
    }

    b2Body* PhysicsWorld::CreateBody(const Entity& entity)
    {
        const auto& configuration = *entity.Physics;
        b2BodyDef definition;
        definition.type = static_cast<b2BodyType>(configuration.Type);
        definition.position.Set(entity.Transform.Position.x / PixelsPerMeter, entity.Transform.Position.y / PixelsPerMeter);
        definition.angle = entity.Transform.Rotation * DegreesToRadians;
        definition.linearVelocity.Set(configuration.Velocity.x / PixelsPerMeter, configuration.Velocity.y / PixelsPerMeter);
        definition.angularVelocity = configuration.AngularVelocity * DegreesToRadians;
        definition.gravityScale = configuration.GravityScale;
        definition.fixedRotation = configuration.FixedRotation;
        definition.bullet = configuration.Bullet;
        definition.userData.pointer = static_cast<std::uintptr_t>(entity.Id);
        auto body = _world.CreateBody(&definition);
        CreateFixture(*body, entity);
        return body;
    }

    void PhysicsWorld::CreateFixture(b2Body& body, const Entity& entity)
    {
        const auto& configuration = *entity.Physics;
        const auto scale = entity.Transform.Scale;
        b2PolygonShape box;
        if (configuration.Shape == ColliderShape::Box)
            box.SetAsBox(configuration.HalfSize.x * scale.x / PixelsPerMeter, configuration.HalfSize.y * scale.y / PixelsPerMeter,
                {configuration.Offset.x * scale.x / PixelsPerMeter, configuration.Offset.y * scale.y / PixelsPerMeter}, 0);
        b2CircleShape circle;
        circle.m_radius = configuration.Radius * scale.x / PixelsPerMeter;
        circle.m_p.Set(configuration.Offset.x * scale.x / PixelsPerMeter, configuration.Offset.y * scale.y / PixelsPerMeter);
        b2FixtureDef fixture;
        fixture.shape = configuration.Shape == ColliderShape::Box ? static_cast<b2Shape*>(&box) : static_cast<b2Shape*>(&circle);
        fixture.density = configuration.Density; fixture.friction = configuration.Friction; fixture.restitution = configuration.Restitution;
        fixture.isSensor = configuration.Sensor;
        fixture.filter.categoryBits = configuration.Category;
        fixture.filter.maskBits = configuration.Mask;
        fixture.filter.groupIndex = configuration.Group;
        body.CreateFixture(&fixture);
    }

    void PhysicsWorld::SetGravity(sf::Vector2f acceleration)
    {
        if (!std::isfinite(acceleration.x) || !std::isfinite(acceleration.y) || std::abs(acceleration.x) > 10000 || std::abs(acceleration.y) > 10000)
            throw std::invalid_argument("Gravity must be finite and within ±10000 meters per second squared per axis.");
        _world.SetGravity({acceleration.x, acceleration.y});
        for (auto& entry : _bodies) entry.second.Body->SetAwake(true);
    }
    void PhysicsWorld::ApplyImpulse(const Scene& scene, std::uint64_t entity, sf::Vector2f impulse)
    {
        if (!std::isfinite(impulse.x) || !std::isfinite(impulse.y) || std::abs(impulse.x) > 1000000 || std::abs(impulse.y) > 1000000)
            throw std::invalid_argument("Impulse components must be finite and within ±1000000 kg m/s.");
        Synchronize(scene);
        _bodies.at(entity).Body->ApplyLinearImpulseToCenter({impulse.x, impulse.y}, true);
    }
    float PhysicsWorld::Mass(const Scene& scene, std::uint64_t entity) { Synchronize(scene); return _bodies.at(entity).Body->GetMass(); }
    void PhysicsWorld::Clear() noexcept
    {
        _world.SetContactListener(nullptr);
        for (auto& entry : _bodies) _world.DestroyBody(entry.second.Body);
        _bodies.clear();
        _contacts.clear();
        _events.clear();
        _began.clear();
        _world.SetContactListener(this);
    }

    void PhysicsBody::Validate() const
    {
        if (Type < BodyType::Static || Type > BodyType::Dynamic || Shape < ColliderShape::Box || Shape > ColliderShape::Circle ||
            !std::isfinite(HalfSize.x) || !std::isfinite(HalfSize.y) || HalfSize.x <= 0 || HalfSize.y <= 0 ||
            !std::isfinite(Radius) || Radius <= 0 || !std::isfinite(Offset.x) || !std::isfinite(Offset.y) ||
            !std::isfinite(Velocity.x) || !std::isfinite(Velocity.y) || !std::isfinite(AngularVelocity) || !std::isfinite(GravityScale) ||
            !std::isfinite(Density) || Density <= 0 || !std::isfinite(Friction) || Friction < 0 ||
            !std::isfinite(Restitution) || Restitution < 0 || Restitution > 1)
            throw std::invalid_argument("Physics components require valid types, positive dimensions/density, finite motion, nonnegative friction and restitution in [0,1].");
        if (Density < 0.000001f || Density > 1000000 || Friction > 100 || std::abs(GravityScale) > 1000 ||
            std::abs(Velocity.x) > 1000000 || std::abs(Velocity.y) > 1000000 || std::abs(AngularVelocity) > 360000)
            throw std::invalid_argument("Physics material or motion exceeds supported simulation ranges.");
    }
    bool PhysicsBody::SameSettings(const PhysicsBody& other) const
    {
        return Type == other.Type && Shape == other.Shape && HalfSize == other.HalfSize && Offset == other.Offset && Radius == other.Radius &&
            GravityScale == other.GravityScale && Density == other.Density && Friction == other.Friction && Restitution == other.Restitution && FixedRotation == other.FixedRotation && Bullet == other.Bullet &&
            Sensor == other.Sensor && Category == other.Category && Mask == other.Mask && Group == other.Group;
    }

    std::string SerializePhysics(const PhysicsBody& body)
    {
        body.Validate();
        return Json::Document{{"type", static_cast<int>(body.Type)}, {"shape", static_cast<int>(body.Shape)}, {"halfSize", {body.HalfSize.x, body.HalfSize.y}},
            {"radius", body.Radius}, {"offset", {body.Offset.x, body.Offset.y}}, {"velocity", {body.Velocity.x, body.Velocity.y}}, {"angularVelocity", body.AngularVelocity},
            {"gravityScale", body.GravityScale}, {"density", body.Density}, {"friction", body.Friction}, {"restitution", body.Restitution}, {"fixedRotation", body.FixedRotation}, {"bullet", body.Bullet},
            {"sensor", body.Sensor}, {"category", body.Category}, {"mask", body.Mask}, {"group", body.Group}}.dump();
    }
    PhysicsBody DeserializePhysics(const std::string& json)
    {
        const auto document = Json::Parse(json);
        Json::Fields(document, {"type", "shape", "halfSize", "radius", "offset", "velocity", "angularVelocity", "gravityScale", "density", "friction", "restitution", "fixedRotation", "bullet"}, {"sensor", "category", "mask", "group"});
        PhysicsBody body;
        body.Type = static_cast<BodyType>(Json::Integer(document.at("type"))); body.Shape = static_cast<ColliderShape>(Json::Integer(document.at("shape")));
        body.HalfSize = ReadPhysicsVector(document.at("halfSize")); body.Offset = ReadPhysicsVector(document.at("offset")); body.Velocity = ReadPhysicsVector(document.at("velocity"));
        body.Radius = Json::Number(document.at("radius")); body.AngularVelocity = Json::Number(document.at("angularVelocity"));
        body.GravityScale = Json::Number(document.at("gravityScale")); body.Density = Json::Number(document.at("density"));
        body.Friction = Json::Number(document.at("friction")); body.Restitution = Json::Number(document.at("restitution"));
        body.FixedRotation = document.at("fixedRotation").get<bool>(); body.Bullet = document.at("bullet").get<bool>();
        body.Sensor = document.value("sensor", false);
        const auto category = document.contains("category") ? Json::Integer(document.at("category")) : 1;
        const auto mask = document.contains("mask") ? Json::Integer(document.at("mask")) : 65535;
        const auto group = document.contains("group") ? Json::Integer(document.at("group")) : 0;
        if (category < 0 || category > 65535 || mask < 0 || mask > 65535 || group < -32768 || group > 32767)
            throw std::invalid_argument("Physics categories/masks require unsigned 16-bit values; groups require signed 16-bit values.");
        body.Category = static_cast<std::uint16_t>(category); body.Mask = static_cast<std::uint16_t>(mask); body.Group = static_cast<std::int16_t>(group);
        body.Validate();
        return body;
    }
}
