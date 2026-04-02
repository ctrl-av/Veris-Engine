using VerisEditor.Interop;

internal static class HierarchyChecks
{
    public static void Run()
    {
        using var engine = new NativeEngine(16, 16);
        var parent = engine.CreateEntity("Parent");
        var child = engine.CreateEntity("Child");
        engine.SetParent(child, parent);
        engine.SetTransform(child, new EntityTransform(3, 4, 2, 2, 15));
        engine.RenameEntity(child, "Renamed 世界");
        Require(engine.EntityParent(child) == parent && engine.EntityName(child) == "Renamed 世界", "Hierarchy edits must round-trip through native state.");
        try { engine.SetParent(parent, child); throw new Exception("Cycles must be rejected."); }
        catch (NativeEngineException) { }
        Require(engine.EntityParent(parent) == 0, "Cycle rejection must preserve parenting.");
        var duplicate = engine.DuplicateEntity(parent);
        Require(engine.EntityCount == 4 && engine.EntityName(duplicate) == "Parent Copy", "Duplicate must copy the complete subtree.");
        var copiedChild = engine.EntityIds().Single(id => engine.EntityParent(id) == duplicate);
        Require(engine.GetTransform(copiedChild).PositionX == 3 && copiedChild != child, "Subtree copies must retain components and use fresh identifiers.");
        engine.DestroyEntity(parent);
        Require(engine.EntityCount == 2 && engine.EntityParent(copiedChild) == duplicate, "Deleting the original must preserve the independent copy.");
        engine.SetParent(copiedChild, 0);
        engine.DestroyEntity(duplicate);
        Require(engine.EntityCount == 1, "Detached descendants must survive deletion of the old parent.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
