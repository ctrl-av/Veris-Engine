#include "Checks.h"
#include "../../Engine/Engine.h"
#include "../../Engine/SceneFormat.h"
#include <SFML/Graphics/Image.hpp>
#include <cmath>

namespace
{
    Veris::EntityId AddBody(Veris::Scene& scene, Veris::BodyType type, sf::Vector2f position)
    {
        const auto id = scene.CreateEntity("Contact fixture");
        scene.Get(id).Transform.Position = position;
        scene.Get(id).Physics = Veris::PhysicsBody{};
        scene.Get(id).Physics->Type = type;
        return id;
    }

    void VerifyContactLifecycle()
    {
        Veris::Engine engine;
        engine.Initialize({128, 128});
        auto& scene = engine.ActiveScene();
        const auto first = AddBody(scene, Veris::BodyType::Static, {32, 32});
        const auto second = AddBody(scene, Veris::BodyType::Dynamic, {60, 32});
        engine.FixedUpdate(1.0 / 60);
        auto contacts = engine.Physics().Contacts();
        Require(contacts.size() == 1 && contacts[0].Phase == Veris::ContactPhase::Begin && !contacts[0].Trigger && contacts[0].HasPoint,
            "Solid contacts must expose a begin event with physical contact geometry.");
        Require(contacts[0].First == first && contacts[0].Second == second && contacts[0].Normal.x > 0.9f,
            "Contact IDs and normals must use canonical entity ordering.");
        engine.FixedUpdate(1.0 / 60);
        Require(engine.Physics().Contacts().size() == 1 && engine.Physics().Contacts()[0].Phase == Veris::ContactPhase::Stay,
            "Persistent contacts must emit one stay event per fixed update.");
        scene.Get(second).Transform.Position = {200, 32};
        engine.FixedUpdate(1.0 / 60);
        Require(engine.Physics().Contacts().size() == 1 && engine.Physics().Contacts()[0].Phase == Veris::ContactPhase::End,
            "Separating bodies must emit one end event.");
        engine.FixedUpdate(1.0 / 60);
        Require(engine.Physics().Contacts().empty(), "Ended contacts must not remain in subsequent fixed frames.");
        scene.Get(second).Transform.Position = {60, 32};
        engine.FixedUpdate(1.0 / 60);
        scene.DestroyEntity(second);
        engine.Update();
        Require(engine.Physics().Contacts().back().Phase == Veris::ContactPhase::End && engine.Physics().BodyCount() == 1,
            "Removing a touching entity must end its contact and release its body safely.");
    }

    void VerifyTriggersAndFiltering()
    {
        Veris::Engine engine;
        engine.Initialize({128, 128});
        auto& scene = engine.ActiveScene();
        const auto sensor = AddBody(scene, Veris::BodyType::Static, {60, 32});
        scene.Get(sensor).Physics->Sensor = true;
        const auto actor = AddBody(scene, Veris::BodyType::Dynamic, {0, 32});
        scene.Get(actor).Physics->Velocity = {60, 0};
        int began = 0;
        int ended = 0;
        for (int frame = 0; frame < 120; ++frame)
        {
            engine.FixedUpdate(1.0 / 60);
            for (const auto& contact : engine.Physics().Contacts())
            {
                Require(contact.Trigger && !contact.HasPoint, "Trigger events must not invent solid contact points.");
                if (contact.Phase == Veris::ContactPhase::Begin) ++began;
                if (contact.Phase == Veris::ContactPhase::End) ++ended;
            }
        }
        Require(began == 1 && ended == 1 && std::abs(scene.Get(actor).Transform.Position.x - 120) < 0.1f,
            "Triggers must report entry/exit without applying collision response.");
        scene.Get(actor).Physics->Mask = 0;
        scene.Get(actor).Transform.Position = {60, 32};
        engine.FixedUpdate(1.0 / 60);
        Require(engine.Physics().Contacts().empty(), "Collision masks must suppress trigger contacts.");
        scene.Get(actor).Physics->Group = -2;
        scene.Get(sensor).Physics->Group = -2;
        scene.Get(actor).Physics->Mask = 65535;
        engine.FixedUpdate(1.0 / 60);
        Require(engine.Physics().Contacts().empty(), "Matching negative collision groups must suppress contacts.");
        auto restored = Veris::DeserializeScene(Veris::SerializeScene(scene), engine.Textures());
        Require(restored.Get(sensor).Physics->Sensor && restored.Get(actor).Physics->Group == -2, "Trigger/filter configuration must survive scene loading.");
    }

    void VerifyDebugGeometry(const std::filesystem::path& imagePath)
    {
        Veris::Engine engine;
        engine.Initialize({128, 96});
        auto& scene = engine.ActiveScene();
        AddBody(scene, Veris::BodyType::Static, {32, 32});
        const auto circle = AddBody(scene, Veris::BodyType::Dynamic, {80, 48});
        scene.Get(circle).Physics->Shape = Veris::ColliderShape::Circle;
        scene.Get(circle).Physics->Sensor = true;
        engine.Update();
        const auto original = engine.GetFrame().copyToImage();
        engine.PhysicsDebugEnabled = true;
        engine.Update();
        const auto debug = engine.GetFrame().copyToImage();
        unsigned changed = 0;
        for (unsigned y = 0; y < 96; ++y)
            for (unsigned x = 0; x < 128; ++x) if (original.getPixel({x, y}) != debug.getPixel({x, y})) ++changed;
        Require(changed > 100 && debug.saveToFile(imagePath), "Debug mode must render actual box and circle outlines into the native frame.");
        engine.PhysicsDebugEnabled = false;
        engine.Update();
        Require(engine.GetFrame().copyToImage().getPixel({16, 32}) == Veris::Engine::BackgroundColor, "Disabling debugging must remove its geometry from rendering.");
    }
}

void VerifyContacts(const std::filesystem::path& imagePath)
{
    VerifyContactLifecycle();
    VerifyTriggersAndFiltering();
    VerifyDebugGeometry(imagePath);
}
