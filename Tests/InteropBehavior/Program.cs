using VerisEditor.Interop;

try
{
    AbiChecks.Run();
    HierarchyChecks.Run();
    SceneFormatChecks.Run();
    PhysicsChecks.Run();
    TemplateChecks.Run();
    SampleChecks.Run();
    if (args.Length == 2 && args[0] == "--runtime") ExportChecks.Run(args[1]);
    Reject<NativeEngineException>(() => new NativeEngine(0, 8));
    for (var session = 0; session < 4; session++)
    {
        using var engine = new NativeEngine(8, 8);
        var entity = engine.CreateEntity("Player 世界");
        Require(engine.EntityCount == 1, "Managed entity creation must mutate the native scene.");
        Require(engine.EntityIds().Single() == entity && engine.EntityName(entity) == "Player 世界", "Entity enumeration and UTF-8 names must round-trip.");
        engine.SetTransform(entity, new EntityTransform(2, 3, 1, 1, 90));
        Reject<NativeEngineException>(() => engine.SetTransform(entity, new EntityTransform(float.NaN, 0, 1, 1, 0)));
        var transform = engine.GetTransform(entity);
        Require(transform.PositionX == 2 && transform.PositionY == 3 && transform.Rotation == 90, "Rejected transforms must preserve native state and ABI layout.");
        engine.Update();
        var rgba = engine.CopyFrame();
        Require(rgba.Length == 8 * 8 * 4 && rgba[0] == 24 && rgba[3] == 255, "Caller-owned frame copy must contain native RGBA pixels.");
        engine.Resize(4, 2);
        engine.Update();
        Require(engine.CopyFrame().Length == 4 * 2 * 4, "Resize must update frame transfer size.");
        Reject<NativeEngineException>(() => engine.Resize(0, 2));
        Task.Run(() => Reject<NativeEngineException>(() => engine.Update())).GetAwaiter().GetResult();
        engine.DestroyEntity(entity);
        Require(engine.EntityCount == 0, "Managed deletion must remove the native entity.");
        Reject<NativeEngineException>(() => engine.DestroyEntity(entity));
        engine.Dispose();
        Reject<ObjectDisposedException>(() => engine.Update());
    }
    Console.WriteLine("PASS: Managed/native ownership, scene mutation, frame transfer, thread affinity, and error propagation.");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception);
    return 1;
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static bool Reject<T>(Action action) where T : Exception
{
    try { action(); }
    catch (T exception)
    {
        Require(!string.IsNullOrWhiteSpace(exception.Message), "Failures must include actionable diagnostics.");
        return true;
    }
    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}
