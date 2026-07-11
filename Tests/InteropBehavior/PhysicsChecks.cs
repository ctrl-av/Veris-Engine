using System.Text.Json.Nodes;
using VerisEditor.Interop;

internal static class PhysicsChecks
{
    public static void Run()
    {
        using var engine = new NativeEngine(128, 128);
        var first = engine.CreateEntity("Solid");
        var second = engine.CreateEntity("Trigger");
        const string body = "{\"type\":2,\"shape\":0,\"halfSize\":[16,16],\"radius\":16,\"offset\":[0,0],\"velocity\":[0,0],\"angularVelocity\":0,\"gravityScale\":0,\"density\":1,\"friction\":0.3,\"restitution\":0,\"fixedRotation\":true,\"bullet\":false}";
        engine.SetPhysics(first, body);
        var trigger = JsonNode.Parse(body)!;
        trigger["type"] = 0; trigger["sensor"] = true;
        engine.SetPhysics(second, trigger.ToJsonString());
        engine.SetTransform(first, new EntityTransform(32, 32, 1, 1, 0));
        engine.SetTransform(second, new EntityTransform(60, 32, 1, 1, 0));
        engine.SetGravity(0, 0);
        engine.FixedUpdate(1.0 / 60);
        var contact = JsonNode.Parse(engine.Contacts())![0]!;
        Require(contact["first"]!.GetValue<ulong>() == first && contact["second"]!.GetValue<ulong>() == second &&
            contact["phase"]!.GetValue<string>() == "begin" && contact["trigger"]!.GetValue<bool>() && contact["point"] == null,
            "Managed contacts must expose ordered IDs, phases, trigger classification and absent sensor manifolds.");
        using var snapshot = engine.CaptureScene();
        var invalid = JsonNode.Parse(body)!;
        invalid["mask"] = 65536;
        Reject(() => engine.SetPhysics(first, invalid.ToJsonString()));
        Reject(() => engine.SetParent(first, second));
        Reject(() => engine.SetTransform(first, new EntityTransform(32, 32, 0, 1, 0)));
        Require(engine.MatchesScene(snapshot), "Rejected physics edits must preserve scene components and transforms.");
        engine.ApplyImpulse(first, -1, 0);
        engine.FixedUpdate(1.0 / 60);
        Require(engine.GetTransform(first).PositionX < 32, "Managed impulse APIs must affect native simulation.");
        engine.RestoreScene(snapshot);
        engine.FixedUpdate(1.0 / 60);
        Require(engine.Contacts().Contains("begin"), "Snapshot restoration must discard prior native contact state.");
        engine.SetPhysicsDebug(true); engine.Update();
        Require(engine.CopyFrame().Where((channel, index) => index % 4 == 1 && channel == 255).Any(), "Managed debug toggles must affect native frame pixels.");
        engine.DestroyEntity(first); engine.Update();
        Require(engine.Contacts().Contains("end"), "Managed deletion must expose contact exit safely.");
    }
    private static void Reject(Action operation)
    {
        try { operation(); throw new InvalidOperationException("Invalid physics edit was accepted."); }
        catch (NativeEngineException) { }
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
