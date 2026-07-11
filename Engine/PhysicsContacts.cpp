#include "pch.h"
#include "Physics.h"
#include "JsonValidation.h"
#include <algorithm>

namespace Veris
{
    PhysicsWorld::PhysicsWorld() { _world.SetContactListener(this); }
    PhysicsWorld::~PhysicsWorld() { _world.SetContactListener(nullptr); }

    PhysicsContact PhysicsWorld::DescribeContact(b2Contact& contact) const
    {
        const auto first = static_cast<std::uint64_t>(contact.GetFixtureA()->GetBody()->GetUserData().pointer);
        const auto second = static_cast<std::uint64_t>(contact.GetFixtureB()->GetBody()->GetUserData().pointer);
        PhysicsContact description{std::min(first, second), std::max(first, second), ContactPhase::Begin,
            contact.GetFixtureA()->IsSensor() || contact.GetFixtureB()->IsSensor()};
        if (!description.Trigger && contact.GetManifold()->pointCount > 0)
        {
            b2WorldManifold manifold;
            contact.GetWorldManifold(&manifold);
            const auto direction = first < second ? 1.f : -1.f;
            description.Normal = {direction * manifold.normal.x, direction * manifold.normal.y};
            description.Point = {manifold.points[0].x * PixelsPerMeter, manifold.points[0].y * PixelsPerMeter};
            description.HasPoint = true;
        }
        return description;
    }

    void PhysicsWorld::BeginContact(b2Contact* contact)
    {
        const auto description = DescribeContact(*contact);
        const ContactPair pair{description.First, description.Second};
        _contacts[pair] = description;
        _began.insert(pair);
        _events.push_back(description);
    }

    void PhysicsWorld::EndContact(b2Contact* contact)
    {
        const auto first = static_cast<std::uint64_t>(contact->GetFixtureA()->GetBody()->GetUserData().pointer);
        const auto second = static_cast<std::uint64_t>(contact->GetFixtureB()->GetBody()->GetUserData().pointer);
        const auto found = _contacts.find({std::min(first, second), std::max(first, second)});
        if (found == _contacts.end()) return;
        auto ended = found->second;
        ended.Phase = ContactPhase::End;
        _events.push_back(ended);
        _contacts.erase(found);
    }

    void PhysicsWorld::RefreshContacts()
    {
        for (auto contact = _world.GetContactList(); contact; contact = contact->GetNext())
        {
            if (!contact->IsTouching()) continue;
            auto description = DescribeContact(*contact);
            const ContactPair pair{description.First, description.Second};
            _contacts[pair] = description;
            if (_began.count(pair) != 0) continue;
            description.Phase = ContactPhase::Stay;
            _events.push_back(description);
        }
    }

    std::string PhysicsWorld::ContactsJson() const
    {
        auto events = Json::Document::array();
        for (const auto& contact : _events)
            events.push_back({{"first", contact.First}, {"second", contact.Second},
                {"phase", contact.Phase == ContactPhase::Begin ? "begin" : contact.Phase == ContactPhase::Stay ? "stay" : "end"},
                {"trigger", contact.Trigger}, {"normal", {contact.Normal.x, contact.Normal.y}},
                {"point", contact.HasPoint ? Json::Document{contact.Point.x, contact.Point.y} : Json::Document(nullptr)}});
        return events.dump(2);
    }
}
