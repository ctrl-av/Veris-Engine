using System.Diagnostics;
using System.IO;
using VerisEditor.Build;
using VerisEditor.GameProject;

internal static class ExportChecks
{
    public static void Run(string runtime)
    {
        var root = Path.Combine(AppContext.BaseDirectory, "ExportCheck-" + Guid.NewGuid().ToString("N"));
        try
        {
            var template = ProjectTemplate.Load(Path.Combine(AppContext.BaseDirectory, "ProjectTemplates", "TopDownProject", "template.xml"));
            var projectFile = ProjectFiles.Create("Export source", root, template);
            var output = Path.Combine(root, "Standalone game");
            var executable = ProjectExporter.Export(projectFile, output, runtime);
            if (!File.Exists(Path.Combine(output, "export-manifest.json")) || File.Exists(Path.Combine(output, "VerisEditor.dll"))) throw new Exception("Export must contain an integrity manifest and no editor binary.");
            var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add("--frames"); start.ArgumentList.Add("2");
            using var process = Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000)) { process.Kill(true); throw new Exception("Exported game timed out."); }
            if (process.ExitCode != 0 || !stdout.GetAwaiter().GetResult().Contains("Completed 2 frames")) throw new Exception("Exported game failed: " + stderr.GetAwaiter().GetResult());
            try { ProjectExporter.Export(projectFile, output, runtime); throw new Exception("Export overwrote an existing destination."); }
            catch (IOException) { }
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
