using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using VerisEditor;
using VerisEditor.Build;
using VerisEditor.GameProject;
using VerisEditor.Interop;

internal static class Program
{
    private static readonly string Root = Path.Combine(AppContext.BaseDirectory, "Workflow-" + Guid.NewGuid().ToString("N"));
    private static string Runtime = "";
    private static int _nextProject;
    private static int Main(string[] arguments)
    {
        if (arguments.Length != 1) { Console.Error.WriteLine("Usage: Workflow <packaged Release runtime directory>"); return 1; }
        Runtime = Path.GetFullPath(arguments[0]);
        Directory.CreateDirectory(Root);
        var checks = new Action[] { Export_RejectsMissingStart, Export_RejectsChangedAssets, Export_RejectsMissingTexture,
            Export_RejectsScriptSyntax, Export_RejectsContainedDestination, Export_VerifiesManifestAndExcludesEditorCache,
            Template_RejectsUnexpectedPayloadRoot, Content_RejectsRecursiveCopy, Content_PublishesAfterTemporaryFileLock, Settings_RejectsInvalidNumbers,
            Input_RejectsDuplicateFieldsAndBindings, Physics_RejectsPartialSettingsUpdate, Export_SampleGame };
        try
        {
            foreach (var check in checks) { check(); Console.WriteLine("PASS: " + check.Method.Name); }
            Console.WriteLine($"PASS: {checks.Length} workflow business-rule and state-transition checks."); return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
        finally { if (Directory.Exists(Root)) Directory.Delete(Root, true); }
    }

    private static string Create(string templateName = "TopDownProject")
    {
        var template = ProjectTemplate.Load(Path.Combine(AppContext.BaseDirectory, "ProjectTemplates", templateName, "template.xml"));
        return ProjectFiles.Create("Project " + ++_nextProject, Root, template);
    }
    private static void Export_RejectsMissingStart()
    {
        var file = Create("EmptyProject"); var destination = Path.Combine(Root, "Missing start");
        Reject<InvalidDataException>(() => ProjectExporter.Export(file, destination, Runtime));
        Require(!Directory.Exists(destination), "A rejected project must not publish output.");
    }
    private static void Export_RejectsChangedAssets()
    {
        var file = Create(); var texture = Path.Combine(Path.GetDirectoryName(file)!, "Content", "Textures", "player.png");
        File.WriteAllText(texture, "changed asset"); var destination = Path.Combine(Root, "Changed asset");
        Reject<InvalidDataException>(() => ProjectExporter.Export(file, destination, Runtime));
        Require(File.ReadAllText(texture) == "changed asset" && !Directory.Exists(destination) && !Directory.GetDirectories(Root, ".veris-export-*").Any(),
            "Rejected exports must preserve source edits and remove staging directories.");
    }
    private static void Export_RejectsMissingTexture()
    {
        var file = Create(); File.Delete(Path.Combine(Path.GetDirectoryName(file)!, "Content", "Textures", "player.png"));
        var destination = Path.Combine(Root, "Missing texture");
        Reject<IOException>(() => ProjectExporter.Export(file, destination, Runtime));
        Require(!Directory.Exists(destination), "Missing referenced textures must prevent publication.");
    }
    private static void Export_RejectsScriptSyntax()
    {
        var file = Create(); File.WriteAllText(Path.Combine(Path.GetDirectoryName(file)!, "GameCode", "player.lua"), "return ???");
        var destination = Path.Combine(Root, "Invalid script");
        Reject<NativeEngineException>(() => ProjectExporter.Export(file, destination, Runtime));
        Require(!Directory.Exists(destination), "Invalid enabled Lua scripts must prevent publication.");
    }
    private static void Export_RejectsContainedDestination()
    {
        var file = Create(); var destination = Path.Combine(Path.GetDirectoryName(file)!, "Content", "Export");
        Reject<InvalidDataException>(() => ProjectExporter.Export(file, destination, Runtime));
        Require(!Directory.Exists(destination), "Export must not recursively copy its own output.");
    }
    private static void Export_VerifiesManifestAndExcludesEditorCache()
    {
        var file = Create(); File.WriteAllText(Path.Combine(Path.GetDirectoryName(file)!, ".Veris", "editor-cache.json"), "private editor state");
        var destination = Path.Combine(Root, "Verified export");
        ProjectExporter.Export(file, destination, Runtime);
        Require(!File.Exists(Path.Combine(destination, ".Veris", "editor-cache.json")), "Editor-only metadata must not ship with games.");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(destination, "export-manifest.json")));
        foreach (var record in manifest.RootElement.GetProperty("files").EnumerateArray())
        {
            var path = Path.Combine(destination, record.GetProperty("path").GetString()!);
            using var stream = File.OpenRead(path);
            Require(new FileInfo(path).Length == record.GetProperty("bytes").GetInt64() && Convert.ToHexString(SHA256.HashData(stream)).Equals(record.GetProperty("sha256").GetString(), StringComparison.OrdinalIgnoreCase),
                "Every exported manifest record must describe the actual shipped bytes.");
        }
        var before = File.ReadAllBytes(Path.Combine(destination, "game.veris"));
        Reject<IOException>(() => ProjectExporter.Export(file, destination, Runtime));
        Require(before.SequenceEqual(File.ReadAllBytes(Path.Combine(destination, "game.veris"))), "Overwrite rejection must preserve the prior export.");
    }
    private static void Template_RejectsUnexpectedPayloadRoot()
    {
        var payload = Path.Combine(Root, "Invalid payload"); Directory.CreateDirectory(payload); File.WriteAllText(Path.Combine(payload, "outside.txt"), "invalid");
        var template = new ProjectTemplate { TemplateId = "invalid", ProjectType = "Invalid", Description = "Invalid payload", ProjectFile = "project.veris", IconPath = "", ScreenshotPath = "", Folders = new[] { "Content", "GameCode" }, PayloadDirectory = payload };
        Reject<InvalidDataException>(() => ProjectFiles.Create("Invalid template project", Root, template));
        Require(!Directory.Exists(Path.Combine(Root, "Invalid template project")) && !Directory.GetDirectories(Root, ".veris-create-*").Any(), "Invalid template payloads must not publish partial projects.");
    }
    private static void Content_RejectsRecursiveCopy()
    {
        var source = Path.Combine(Root, "Copy source"); Directory.CreateDirectory(source);
        Reject<InvalidDataException>(() => ProjectContent.CopyTree(source, Path.Combine(source, "Nested copy")));
        Require(!Directory.Exists(Path.Combine(source, "Nested copy")), "Self-copy rejection must occur before writing output.");
    }
    private static void Settings_RejectsInvalidNumbers()
    {
        var file = Create(); var original = File.ReadAllText(file);
        foreach (var mutation in new Action<JsonNode>[] { root => root["gravityY"] = 10001, root => root["masterVolume"] = -1, root => root["windowWidth"] = 8193 })
        {
            var project = JsonNode.Parse(original)!; mutation(project);
            Reject<InvalidDataException>(() => ProjectDocument.Parse(project.ToJsonString()));
        }
        Require(File.ReadAllText(file) == original, "Rejected project settings must leave saved state intact.");
    }
    private static void Content_PublishesAfterTemporaryFileLock()
    {
        var source = Path.Combine(Root, "Locked staging"); var destination = Path.Combine(Root, "Published directory");
        Directory.CreateDirectory(source); var file = Path.Combine(source, "content.txt"); File.WriteAllText(file, "complete content");
        using var locked = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
        var release = Task.Run(async () => { await Task.Delay(150); locked.Dispose(); });
        try { ProjectContent.PublishDirectory(source, destination); }
        finally { release.GetAwaiter().GetResult(); }
        Require(!Directory.Exists(source) && File.ReadAllText(Path.Combine(destination, "content.txt")) == "complete content", "Temporary Windows file locks must not leave partially published content.");
    }
    private static void Input_RejectsDuplicateFieldsAndBindings()
    {
        Reject<NativeEngineException>(() => NativeEngine.ValidateInputJson("{\"formatVersion\":1,\"actions\":{},\"actions\":{}}"));
        Reject<NativeEngineException>(() => NativeEngine.ValidateInputJson("{\"formatVersion\":1,\"actions\":{\"Fire\":[\"Key:A\",\"Key:A\"]}}"));
    }
    private static void Physics_RejectsPartialSettingsUpdate()
    {
        using var engine = new NativeEngine(8, 8); var id = engine.CreateEntity("Gravity body");
        engine.SetPhysics(id, "{\"type\":2,\"shape\":0,\"halfSize\":[16,16],\"radius\":16,\"offset\":[0,0],\"velocity\":[0,0],\"angularVelocity\":0,\"gravityScale\":1,\"density\":1,\"friction\":0.3,\"restitution\":0,\"fixedRotation\":true,\"bullet\":false}");
        engine.ConfigureSimulation(0, 0, 100); engine.GameStep(.1);
        Require(engine.GetTransform(id).PositionY == 0, "Zero configured gravity must prevent falling.");
        engine.ConfigureSimulation(0, 10, 100); engine.GameStep(.1); var before = engine.GetTransform(id).PositionY;
        Reject<NativeEngineException>(() => engine.ConfigureSimulation(0, 0, 101)); engine.GameStep(.1);
        Require(engine.GetTransform(id).PositionY > before + 10, "Invalid audio settings must not partially overwrite working gravity.");
    }
    private static void Export_SampleGame()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "Samples", "BeaconRun", "BeaconRun.veris");
        var destination = Path.Combine(Root, "Beacon Run"); ProjectExporter.Export(source, destination, Runtime);
        Require(File.Exists(Path.Combine(destination, "Content", "Scenes", "win.verisscene")) && File.Exists(Path.Combine(destination, "Content", "Prefabs", "signal.verisprefab")), "Sample export must retain destination scenes and runtime-spawned prefabs.");
    }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
