using System.Text.Json.Nodes;
using VerisEditor.Interop;

internal static class SceneFormatChecks
{
    public static void Run()
    {
        using var engine = new NativeEngine(32, 32);
        var parent = engine.CreateEntity("Parent 世界");
        var child = engine.CreateEntity("Child");
        engine.SetParent(child, parent);
        engine.SetTransform(child, new EntityTransform(3, 4, -2, 5, 45));
        var json = engine.SerializeScene();
        using var original = engine.CaptureScene();
        engine.DestroyEntity(parent);
        engine.DeserializeScene(json);
        Require(engine.MatchesScene(original), "Scene round trips must preserve identifiers, Unicode, hierarchy, and transforms.");
        foreach (var mutation in new Action<JsonNode>[]
        {
            root => root["formatVersion"] = 2,
            root => root["extra"] = true,
            root => root["entities"]![1]!["id"] = parent,
            root => root["entities"]![0]!["id"] = -1,
            root => root["entities"]![0]!["parent"] = child,
            root => root["entities"]![1]!["parent"] = 999,
            root => root["entities"]![0]!["transform"]!["scale"] = new JsonArray(1)
        })
        {
            var invalid = JsonNode.Parse(json)!;
            mutation(invalid);
            try { engine.DeserializeScene(invalid.ToJsonString()); throw new Exception("Invalid scenes must fail."); }
            catch (NativeEngineException) { }
            Require(engine.MatchesScene(original), "Failed scene loads must preserve the complete active scene.");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
