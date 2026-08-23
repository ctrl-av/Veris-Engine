using System.IO;
using VerisEditor.GameProject;
using VerisEditor.Interop;

internal static class TemplateChecks
{
    public static void Run()
    {
        var parent = Path.Combine(AppContext.BaseDirectory, "TemplateCheck-" + Guid.NewGuid().ToString("N"));
        try
        {
            var template = ProjectTemplate.Load(Path.Combine(AppContext.BaseDirectory, "ProjectTemplates", "TopDownProject", "template.xml"));
            var file = ProjectFiles.Create("Starter", parent, template);
            var project = ProjectFiles.Open(file);
            var root = Path.GetDirectoryName(file)!;
            using var engine = new NativeEngine(1280, 720);
            engine.SetContentRoot(Path.Combine(root, project.ContentDirectory));
            engine.SetScriptRoot(Path.Combine(root, project.GameCodeDirectory));
            engine.LoadAssetCatalog(Path.Combine(root, ".Veris", "assets.json"));
            engine.ConfigureInput(root); engine.LoadSceneFile(project.StartupScene!);
            var player = engine.EntityIds().Single(id => engine.EntityName(id) == "Player");
            engine.NamedInput("Key:D", true);
            for (var frame = 0; frame < 240; frame++) engine.GameStep(1.0 / 60);
            var position = engine.GetTransform(player);
            Require(position.PositionX > 1200 && position.PositionX < 1240, "Starter movement must reach and collide with the arena wall.");
            engine.ReleaseInput(); engine.SetTransform(player, new EntityTransform(1050, 550, 1, 1, 0));
            engine.GameStep(1.0 / 60); engine.GameStep(1.0 / 60);
            Require(engine.ScriptLog().Contains("Goal reached"), "The starter goal must react to an actual physics trigger.");
            engine.Update();
            Require(engine.CopyFrame().Length == 1280 * 720 * 4, "Copied template textures, animation and scripts must render together.");
        }
        finally { if (Directory.Exists(parent)) Directory.Delete(parent, true); }
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
