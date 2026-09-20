using System.IO;
using System.Text.Json;
using VerisEditor.Assets;
using VerisEditor.GameProject;
using VerisEditor.Interop;

namespace VerisEditor.Build
{
    public static class ProjectExporter
    {
        private static readonly string[] RequiredRuntime = { "VerisRuntime.exe", "sfml-graphics-3.dll", "sfml-window-3.dll", "sfml-system-3.dll", "sfml-audio-3.dll", "msvcp140.dll", "vcruntime140.dll", "vcruntime140_1.dll" };

        public static string Export(string projectFile, string destination, string runtimeDirectory)
        {
            var project = ProjectFiles.Open(projectFile);
            if (project.StartupScene == null) throw new InvalidDataException("Configure a starting scene before exporting.");
            var root = Path.GetDirectoryName(Path.GetFullPath(projectFile))!;
            RejectLinkedAncestors(root); RejectLinkedAncestors(runtimeDirectory);
            destination = Path.TrimEndingDirectorySeparator(Path.GetFullPath(destination));
            RejectContainedDestination(root, destination); RejectContainedDestination(runtimeDirectory, destination);
            if (Directory.Exists(destination) || File.Exists(destination)) throw new IOException("Export destination already exists.");
            ValidateRuntime(runtimeDirectory);
            var parent = Path.GetDirectoryName(destination)!; RejectLinkedAncestors(parent); Directory.CreateDirectory(parent);
            var staged = ContentPaths.Resolve(parent, ".veris-export-" + Guid.NewGuid().ToString("N"));
            try
            {
                StageProject(root, staged, project, runtimeDirectory);
                ValidateContent(staged, project);
                WriteManifest(staged, project.ProjectId);
                ProjectContent.PublishDirectory(staged, destination);
                return Path.Combine(destination, "Game.exe");
            }
            finally { if (Directory.Exists(staged)) Directory.Delete(staged, true); }
        }

        private static void StageProject(string root, string staged, ProjectDocument project, string runtime)
        {
            Directory.CreateDirectory(staged);
            ProjectContent.CopyTree(ContentPaths.Resolve(root, project.ContentDirectory), Path.Combine(staged, project.ContentDirectory));
            ProjectContent.CopyTree(ContentPaths.Resolve(root, project.GameCodeDirectory), Path.Combine(staged, project.GameCodeDirectory));
            Directory.CreateDirectory(Path.Combine(staged, ".Veris"));
            foreach (var name in new[] { "assets.json", "input-actions.json" })
            {
                var source = ContentPaths.Resolve(root, ".Veris/" + name);
                if (File.Exists(source)) ProjectContent.CopyFile(source, Path.Combine(staged, ".Veris", name));
            }
            File.WriteAllText(Path.Combine(staged, "game.veris"), project.Serialize());
            ProjectContent.CopyFile(Path.Combine(runtime, "VerisRuntime.exe"), Path.Combine(staged, "Game.exe"));
            foreach (var dll in Directory.EnumerateFiles(runtime, "*.dll")) ProjectContent.CopyFile(dll, Path.Combine(staged, Path.GetFileName(dll)));
            ProjectContent.CopyTree(Path.Combine(runtime, "licenses"), Path.Combine(staged, "licenses"));
        }

        private static void ValidateContent(string root, ProjectDocument project)
        {
            var content = ContentPaths.Resolve(root, project.ContentDirectory);
            var catalog = new AssetCatalog(content, root, NativeEngine.ValidateTextureFile);
            foreach (var asset in catalog.Assets)
            {
                var path = ContentPaths.Resolve(content, asset.Path);
                if (!ProjectContent.Sha256(path).Equals(asset.Sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Asset changed since import; reimport before exporting: " + asset.Path);
                if (asset.Kind == "audio") NativeEngine.ValidateAudioFile(path); else NativeEngine.ValidateTextureFile(path);
            }
            using var engine = new NativeEngine(1, 1);
            engine.SetContentRoot(content); engine.SetScriptRoot(ContentPaths.Resolve(root, project.GameCodeDirectory));
            engine.LoadAssetCatalog(catalog.ManifestPath); engine.ConfigureInput(root);
            foreach (var file in Directory.EnumerateFiles(content, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(content, file);
                if (Path.GetExtension(file).Equals(".verisscene", StringComparison.OrdinalIgnoreCase)) engine.LoadSceneFile(relative);
                else if (Path.GetExtension(file).Equals(".verisprefab", StringComparison.OrdinalIgnoreCase)) { engine.DeserializeScene("{\"formatVersion\":1,\"entities\":[]}"); engine.InstantiatePrefab(relative, 0, 0); }
                else continue;
                ValidateScripts(engine);
            }
            engine.LoadSceneFile(project.StartupScene!); ValidateScripts(engine);
        }

        private static void ValidateScripts(NativeEngine engine)
        {
            using var scene = JsonDocument.Parse(engine.SerializeScene());
            foreach (var entity in scene.RootElement.GetProperty("entities").EnumerateArray())
                if (entity.TryGetProperty("script", out var script) && script.GetProperty("enabled").GetBoolean())
                    engine.SetScript(entity.GetProperty("id").GetUInt64(), script.GetRawText());
        }

        private static void ValidateRuntime(string runtime)
        {
            if ((File.GetAttributes(runtime) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Runtime distribution cannot be linked.");
            foreach (var file in RequiredRuntime)
                if (!File.Exists(Path.Combine(runtime, file)) || (File.GetAttributes(Path.Combine(runtime, file)) & FileAttributes.ReparsePoint) != 0) throw new FileNotFoundException("Export runtime is incomplete. Build and publish the editor with scripts/PublishEditor.ps1.", file);
            foreach (var file in Directory.EnumerateFiles(runtime, "*.dll"))
                if (Path.GetFileName(file).EndsWith("-d-3.dll", StringComparison.OrdinalIgnoreCase) || (File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Export requires an unlinked Release runtime distribution.");
        }

        private static void RejectLinkedAncestors(string path)
        {
            for (var directory = new DirectoryInfo(Path.GetFullPath(path)); directory != null; directory = directory.Parent)
                if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Export paths cannot follow linked directories.");
        }

        private static void RejectContainedDestination(string source, string destination)
        {
            var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(source));
            if (destination.Equals(root, StringComparison.OrdinalIgnoreCase) || destination.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Export must be outside the source project and runtime directories.");
        }

        private static void WriteManifest(string root, Guid projectId)
        {
            var paths = Directory.GetFiles(root, "*", SearchOption.AllDirectories); Array.Sort(paths, StringComparer.OrdinalIgnoreCase);
            var files = new List<Dictionary<string, object>>();
            foreach (var path in paths) files.Add(new() { ["path"] = Path.GetRelativePath(root, path).Replace('\\', '/'), ["bytes"] = new FileInfo(path).Length, ["sha256"] = ProjectContent.Sha256(path) });
            File.WriteAllText(Path.Combine(root, "export-manifest.json"), JsonSerializer.Serialize(new { formatVersion = 1, projectId, files }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
