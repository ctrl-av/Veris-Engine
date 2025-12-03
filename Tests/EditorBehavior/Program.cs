using VerisEditor;

internal static class Program
{
    private static readonly string TestDirectory = Path.Combine(AppContext.BaseDirectory, "BehaviorChecks", Guid.NewGuid().ToString("N"));

    private static int Main()
    {
        Directory.CreateDirectory(TestDirectory);
        try
        {
            Logging_AppendsMessagesAndExceptionDetails();
            Logging_UnwritableDestinationDoesNotCrash();
            Console.WriteLine("PASS: Editor behavior checks.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            Directory.Delete(TestDirectory, true);
        }
    }

    private static void Logging_AppendsMessagesAndExceptionDetails()
    {
        EditorLog.Initialize(Path.Combine(TestDirectory, "Logs"));
        EditorLog.Info("Project selected.");
        EditorLog.Error("Template failed.", new IOException("Invalid template descriptor."));
        var log = File.ReadAllText(EditorLog.FilePath!);
        Assert(log.Contains("[INFO] Project selected."), "Logging must append informational messages.");
        Assert(log.Contains("[ERROR] Template failed.") && log.Contains("System.IO.IOException: Invalid template descriptor."),
            "Logging must preserve exception type and detail.");
    }

    private static void Logging_UnwritableDestinationDoesNotCrash()
    {
        var blockedDirectory = Path.Combine(TestDirectory, "BlockedLogDirectory");
        File.WriteAllText(blockedDirectory, "This is a file.");
        EditorLog.Initialize(blockedDirectory);
        EditorLog.Error("Disk logging unavailable.", new IOException("Write failed."));
        Assert(File.ReadAllText(blockedDirectory) == "This is a file.", "Failed logging must not damage existing files.");
        EditorLog.Initialize(Path.Combine(TestDirectory, "Logs"));
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

}
