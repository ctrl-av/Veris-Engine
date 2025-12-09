using VerisEditor;
using VerisEditor.GameProject;

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
            ProjectFormat_RoundTripsIdentityAndSettings();
            ProjectFormat_RejectsInvalidDocuments();
            Templates_LoadSupportedDescriptors();
            Templates_RejectInvalidDescriptorsAndKeepValidOnes();
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

    private static void ProjectFormat_RoundTripsIdentityAndSettings()
    {
        var project = CreateProjectDocument();
        var reopenedProject = ProjectDocument.Parse(project.Serialize());
        Assert(reopenedProject.ProjectId == project.ProjectId && reopenedProject.Name == project.Name,
            "Project identity must survive serialization.");
        Assert(reopenedProject.ContentDirectory == "Content" && reopenedProject.GameCodeDirectory == "GameCode" &&
            reopenedProject.StartupScene == "Content/Scenes/Main.scene" && reopenedProject.WindowWidth == 1280,
            "Project paths and settings must survive serialization.");
    }

    private static void ProjectFormat_RejectsInvalidDocuments()
    {
        var json = CreateProjectDocument().Serialize();
        Reject<InvalidDataException>(() => ProjectDocument.Parse(json.Replace("\"formatVersion\": 1", "\"formatVersion\": 2")));
        Reject<InvalidDataException>(() => ProjectDocument.Parse(json.Replace("\"Content\"", "\"../Outside\"")));
        Reject<InvalidDataException>(() => ProjectDocument.Parse(json.Replace("\"windowWidth\": 1280", "\"windowWidth\": 0")));
        Reject<InvalidDataException>(() => ProjectDocument.Parse("{}"));
        Reject<InvalidDataException>(() => ProjectDocument.Parse("null"));
        Reject<InvalidDataException>(() => ProjectDocument.Parse("invalid json"));
    }

    private static ProjectDocument CreateProjectDocument() => new()
    {
        FormatVersion = 1,
        ProjectId = Guid.NewGuid(),
        Name = "SampleGame",
        TemplateId = "empty",
        ContentDirectory = "Content",
        GameCodeDirectory = "GameCode",
        StartupScene = "Content/Scenes/Main.scene"
    };

    private static void Templates_LoadSupportedDescriptors()
    {
        var descriptorPaths = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "ProjectTemplates"), "template.xml", SearchOption.AllDirectories);
        var templates = ProjectTemplate.LoadAvailable(descriptorPaths, (_, exception) => throw exception);
        Assert(templates.Count == 2, "Exactly Empty and Top-Down descriptors must be available.");
        Assert(templates.Any(template => template.TemplateId == "empty") && templates.Any(template => template.TemplateId == "top-down"),
            "Supported template identities must be loaded.");
        Assert(templates.All(template => template.Folders.Contains("Content") && File.Exists(template.IconPath)),
            "Loaded templates must provide folders and existing assets.");
    }

    private static void Templates_RejectInvalidDescriptorsAndKeepValidOnes()
    {
        var validDescriptor = Path.Combine(AppContext.BaseDirectory, "ProjectTemplates", "EmptyProject", "template.xml");
        var invalidDescriptor = Path.Combine(TestDirectory, "template.xml");
        var xml = File.ReadAllText(validDescriptor);
        File.WriteAllText(invalidDescriptor, xml.Replace("<Id>empty</Id>", "<Id>../invalid</Id>"));
        Reject<InvalidDataException>(() => ProjectTemplate.Load(invalidDescriptor));
        var failures = new List<Exception>();
        var templates = ProjectTemplate.LoadAvailable(new[] { invalidDescriptor, validDescriptor, validDescriptor }, (_, exception) => failures.Add(exception));
        Assert(templates.Count == 1 && failures.Count == 2, "Invalid and duplicate templates must be skipped without dropping valid templates.");
        File.WriteAllText(invalidDescriptor, xml.Replace("<Icon>Icon.png.png</Icon>", "<Icon>../outside.png</Icon>"));
        Reject<InvalidDataException>(() => ProjectTemplate.Load(invalidDescriptor));
        File.WriteAllText(invalidDescriptor, "<!DOCTYPE Template [<!ENTITY external SYSTEM 'file:///outside'>]><Template Version=\"1\">&external;</Template>");
        Reject<System.Xml.XmlException>(() => ProjectTemplate.Load(invalidDescriptor));
    }

    private static void Reject<TException>(Action operation) where TException : Exception
    {
        try { operation(); }
        catch (TException) { return; }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }
}
