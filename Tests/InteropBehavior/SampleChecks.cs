using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VerisEditor.GameProject;
using VerisEditor.Interop;

internal static class SampleChecks
{
    public static void Run()
    {
        var file = Path.Combine(AppContext.BaseDirectory, "Samples", "BeaconRun", "BeaconRun.veris");
        var project = ProjectFiles.Open(file); var root = Path.GetDirectoryName(file)!;
        using var engine = new NativeEngine(960, 640);
        engine.SetContentRoot(Path.Combine(root, project.ContentDirectory)); engine.SetScriptRoot(Path.Combine(root, project.GameCodeDirectory));
        engine.LoadAssetCatalog(Path.Combine(root, ".Veris", "assets.json")); engine.ConfigureInput(root);
        engine.ConfigureSimulation(project.GravityX, project.GravityY, 0); engine.LoadSceneFile(project.StartupScene!);
        engine.GameStep(1.0 / 60); SaveFrame(engine, "sample-title.png");
        engine.NamedInput("Key:Enter", true); engine.GameStep(1.0 / 60); engine.ReleaseInput(); engine.GameStep(1.0 / 60);
        SaveFrame(engine, "sample-arena.png");
        var player = Find(engine, "Player");
        var signals = engine.EntityIds().Where(id => engine.PrefabSource(id) == "Prefabs/signal.verisprefab").ToArray();
        Require(signals.Length == 3, "The sample must spawn three signal prefab instances.");
        foreach (var signal in signals)
        {
            var location = engine.GetTransform(signal);
            engine.SetTransform(player, new EntityTransform(location.PositionX, location.PositionY, 1, 1, 0));
            engine.GameStep(1.0 / 60); engine.GameStep(1.0 / 60);
        }
        Require(Find(engine, "Victory screen") != 0 && engine.ScriptLog().Length == 0, "Collecting all signals through physics contacts must win without script failures. " + engine.ScriptLog() + " Entities: " + string.Join(",", engine.EntityIds().Select(engine.EntityName)));
        engine.NamedInput("Key:R", true); engine.GameStep(1.0 / 60); engine.ReleaseInput(); engine.GameStep(1.0 / 60);
        player = Find(engine, "Player");
        var drone = engine.GetTransform(Find(engine, "Drone A"));
        engine.SetTransform(player, new EntityTransform(drone.PositionX, drone.PositionY, 1, 1, 0));
        engine.GameStep(1.0 / 60); engine.GameStep(1.0 / 60);
        Require(Find(engine, "Defeat screen") != 0, "Drone contact must produce the sample's defeat state.");
        engine.NamedInput("Key:R", true); engine.GameStep(1.0 / 60); engine.ReleaseInput(); engine.GameStep(1.0 / 60);
        player = Find(engine, "Player"); engine.SetTransform(player, new EntityTransform(100, 120, 1, 1, 0));
        for (var frame = 0; frame < 2110; frame++) engine.GameStep(1.0 / 60);
        Require(Find(engine, "Defeat screen") != 0 && engine.ScriptLog().Length == 0, "The time limit must lead to defeat and repeated restarts must remain valid.");
        engine.Update();
    }
    private static void SaveFrame(NativeEngine engine, string name)
    {
        engine.Update(); var pixels = engine.CopyFrame();
        for (var index = 0; index < pixels.Length; index += 4) (pixels[index], pixels[index + 2]) = (pixels[index + 2], pixels[index]);
        var bitmap = BitmapSource.Create(960, 640, 96, 96, PixelFormats.Bgra32, null, pixels, 960 * 4);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(AppContext.BaseDirectory, name)); encoder.Save(file);
    }
    private static ulong Find(NativeEngine engine, string name)
    {
        foreach (var id in engine.EntityIds()) if (engine.EntityName(id) == name) return id;
        return 0;
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
