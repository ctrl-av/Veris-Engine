using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VerisEditor.GameProject;
using VerisEditor.Interop;
using VerisEditor.Workspace;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 1) throw new ArgumentException("Usage: Performance <report.json>");
            var file = Path.Combine(AppContext.BaseDirectory, "Samples", "BeaconRun", "BeaconRun.veris");
            var project = ProjectFiles.Open(file);
            var frames = new[] { MeasureFrames(file, project, 960, 640, 16.667), MeasureFrames(file, project, 1920, 1080, 33.334) };
            var sessions = MeasureSessions(file, project);
            var report = new
            {
                measuredUtc = DateTimeOffset.UtcNow, os = Environment.OSVersion.ToString(),
                processor = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"), logicalProcessors = Environment.ProcessorCount,
                framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                workload = "Beacon Run arena; 120 warmup + 600 measured fixed ticks with render/readback/BGRA/WPF upload; 20 lifecycle cycles",
                frames, sessions,
                passed = frames.All(frame => frame.Passed) && sessions.Passed
            };
            var output = Path.GetFullPath(args[0]);
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            File.WriteAllText(output, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
            Console.WriteLine(File.ReadAllText(output));
            return report.passed ? 0 : 1;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }

    private static NativeEngine OpenArena(string file, ProjectDocument project, uint width, uint height)
    {
        var engine = new NativeEngine(width, height);
        try
        {
            var root = Path.GetDirectoryName(file)!;
            engine.SetContentRoot(Path.Combine(root, project.ContentDirectory));
            engine.SetScriptRoot(Path.Combine(root, project.GameCodeDirectory));
            engine.LoadAssetCatalog(Path.Combine(root, ".Veris", "assets.json"));
            engine.ConfigureInput(root);
            engine.ConfigureSimulation(project.GravityX, project.GravityY, 0);
            engine.LoadSceneFile("Scenes/arena.verisscene");
            var player = engine.EntityIds().First(id => engine.EntityName(id) == "Player");
            engine.SetTransform(player, new EntityTransform(100, 120, 1, 1, 0));
            return engine;
        }
        catch { engine.Dispose(); throw; }
    }

    private static FrameMeasurement MeasureFrames(string file, ProjectDocument project, int width, int height, double budget)
    {
        using var engine = OpenArena(file, project, (uint)width, (uint)height);
        var pixels = new byte[width * height * 4];
        var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        for (var tick = 0; tick < 120; tick++) Draw(engine, pixels, bitmap);
        var elapsed = new double[600];
        var simulation = new double[600];
        var rendering = new double[600];
        var transfer = new double[600];
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var tick = 0; tick < elapsed.Length; tick++)
        {
            var start = Stopwatch.GetTimestamp();
            engine.GameStep(1.0 / 60);
            var stepped = Stopwatch.GetTimestamp();
            engine.Update();
            var rendered = Stopwatch.GetTimestamp();
            Upload(engine, pixels, bitmap);
            var end = Stopwatch.GetTimestamp();
            simulation[tick] = Stopwatch.GetElapsedTime(start, stepped).TotalMilliseconds;
            rendering[tick] = Stopwatch.GetElapsedTime(stepped, rendered).TotalMilliseconds;
            transfer[tick] = Stopwatch.GetElapsedTime(rendered, end).TotalMilliseconds;
            elapsed[tick] = Stopwatch.GetElapsedTime(start, end).TotalMilliseconds;
        }
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        if (engine.ScriptLog().Length != 0) throw new InvalidOperationException(engine.ScriptLog());
        if (!engine.EntityIds().Any(id => engine.EntityName(id) == "Player"))
            throw new InvalidOperationException("The profiling workload left the arena before measurement completed.");
        Array.Sort(elapsed); Array.Sort(simulation); Array.Sort(rendering); Array.Sort(transfer);
        return new FrameMeasurement(width, height, budget, elapsed[299], elapsed[569], elapsed[^1],
            simulation[569], rendering[569], transfer[569], allocated / 600.0,
            elapsed[569] <= budget && allocated / 600.0 <= 1024);
    }

    private static void Draw(NativeEngine engine, byte[] pixels, WriteableBitmap bitmap)
    {
        engine.GameStep(1.0 / 60);
        engine.Update();
        Upload(engine, pixels, bitmap);
    }

    private static void Upload(NativeEngine engine, byte[] pixels, WriteableBitmap bitmap)
    {
        engine.CopyFrame(pixels);
        for (var pixel = 0; pixel < pixels.Length; pixel += 4)
            (pixels[pixel], pixels[pixel + 2]) = (pixels[pixel + 2], pixels[pixel]);
        bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight), pixels, bitmap.PixelWidth * 4, 0);
    }

    private static SessionMeasurement MeasureSessions(string file, ProjectDocument project)
    {
        using var process = Process.GetCurrentProcess();
        CycleSessions(file, project, 3);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); process.Refresh();
        var memory = process.PrivateMemorySize64;
        var handles = process.HandleCount;
        var timer = Stopwatch.StartNew();
        CycleSessions(file, project, 20);
        timer.Stop();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); process.Refresh();
        var growth = process.PrivateMemorySize64 - memory;
        var handleGrowth = process.HandleCount - handles;
        return new SessionMeasurement(20, timer.Elapsed.TotalMilliseconds / 20, growth, handleGrowth,
            growth <= 32 * 1024 * 1024 && handleGrowth <= 8 && timer.Elapsed.TotalMilliseconds / 20 <= 2000);
    }

    private static void CycleSessions(string file, ProjectDocument project, int count)
    {
        for (var cycle = 0; cycle < count; cycle++)
        {
            using (var engine = OpenArena(file, project, 960, 640))
            {
                for (var tick = 0; tick < 30; tick++) engine.GameStep(1.0 / 60);
                engine.LoadSceneFile("Scenes/title.verisscene");
                engine.Update();
                if (engine.ScriptLog().Length != 0) throw new InvalidOperationException(engine.ScriptLog());
            }
            using var workspace = new EditorWorkspace(project, file);
            var revision = workspace.History.Revision;
            workspace.PlayStartingScene(); workspace.Pause(); workspace.StepPlay(); workspace.StopPlay();
            if (workspace.IsPlayingSession || workspace.History.Revision != revision || workspace.IsDirty)
                throw new InvalidOperationException("Repeated playback must release its runtime and preserve editing state.");
        }
    }

    private sealed record FrameMeasurement(int Width, int Height, double BudgetP95Ms, double P50Ms, double P95Ms,
        double MaxMs, double SimulationP95Ms, double RenderP95Ms, double TransferP95Ms, double ManagedBytesPerFrame, bool Passed);
    private sealed record SessionMeasurement(int Cycles, double MeanCycleMs, long PrivateBytesGrowth, int HandleGrowth, bool Passed);
}
