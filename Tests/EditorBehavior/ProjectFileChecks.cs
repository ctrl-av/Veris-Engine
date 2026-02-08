using VerisEditor.GameProject;

internal static partial class Program
{
    private static void CreateProject_WritesProjectAndTemplateFolders()
    {
        var template = new NewProject().SelectedTemplate!;
        var projectFile = ProjectFiles.Create("CreatedGame", Path.Combine(TestDirectory, "Creation"), template);
        var project = ProjectDocument.Parse(File.ReadAllText(projectFile));
        Assert(project.Name == "CreatedGame" && project.TemplateId == template.TemplateId && project.StartupScene == null,
            "Creation must generate a valid new project from its selected template.");
        foreach (var folder in template.Folders)
            Assert(Directory.Exists(Path.Combine(Path.GetDirectoryName(projectFile)!, folder)), "Creation must generate template folders.");
        using var inputActions = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(Path.GetDirectoryName(projectFile)!, ".Veris", "input-actions.json")));
        Assert(inputActions.RootElement.GetProperty("formatVersion").GetInt32() == 1 &&
            inputActions.RootElement.GetProperty("actions").GetProperty("Quit")[0].GetString() == "Key:Escape", "Creation must write usable default action bindings.");
        Assert(!Directory.GetDirectories(Path.GetDirectoryName(Path.GetDirectoryName(projectFile))!, ".veris-create-*").Any(),
            "Successful creation must not leave staging directories.");
    }

    private static void CreateProject_RejectsExistingDestination()
    {
        var parent = Path.Combine(TestDirectory, "CreationConflicts");
        var template = new NewProject().SelectedTemplate!;
        var projectFile = ProjectFiles.Create("ExistingGame", parent, template);
        var originalJson = File.ReadAllText(projectFile);
        Reject<InvalidDataException>(() => ProjectFiles.Create("ExistingGame", parent, template));
        Assert(File.ReadAllText(projectFile) == originalJson, "Creation must not overwrite an existing project.");
    }

    private static void CreateProject_RemovesFailedStagingDirectory()
    {
        var parent = Path.Combine(TestDirectory, "FailedCreation");
        var template = new ProjectTemplate
        {
            TemplateId = "broken", ProjectType = "Broken", Description = "Invalid folder test", ProjectFile = "project.veris",
            IconPath = "", ScreenshotPath = "", Folders = new[] { "Content", "../Outside" }
        };
        Reject<InvalidDataException>(() => ProjectFiles.Create("FailedGame", parent, template));
        Assert(!Directory.Exists(Path.Combine(parent, "FailedGame")) && !Directory.GetDirectories(parent, ".veris-create-*").Any(),
            "Failed creation must remove its staging directory without publishing a partial project.");
        Assert(!Directory.Exists(Path.Combine(parent, "Outside")), "Template folders must not escape the staging directory.");
    }

    private static void OpenProject_ReloadsCreatedAndMovedProjects()
    {
        var projectFile = ProjectFiles.Create("OpenGame", Path.Combine(TestDirectory, "Opening"), new NewProject().SelectedTemplate!);
        var project = ProjectFiles.Open(projectFile);
        var relocatedDirectory = Path.Combine(TestDirectory, "RelocatedGame");
        Directory.Move(Path.GetDirectoryName(projectFile)!, relocatedDirectory);
        var relocatedProject = ProjectFiles.Open(Path.Combine(relocatedDirectory, "project.veris"));
        Assert(project.ProjectId == relocatedProject.ProjectId && relocatedProject.Name == "OpenGame",
            "Opening after moving a project must preserve its identity and relative directories.");
    }

    private static void OpenProject_RejectsMalformedOrIncompleteProjects()
    {
        var projectFile = ProjectFiles.Create("IncompleteGame", TestDirectory, new NewProject().SelectedTemplate!);
        Directory.Delete(Path.Combine(Path.GetDirectoryName(projectFile)!, "Content"));
        Reject<FileNotFoundException>(() => ProjectFiles.Open(projectFile));
        File.WriteAllText(projectFile, "invalid json");
        Reject<InvalidDataException>(() => ProjectFiles.Open(projectFile));
        Reject<InvalidDataException>(() => ProjectFiles.Open(Path.ChangeExtension(projectFile, ".txt")));
    }

    private static void OpenProject_FailureClearsLoadedState()
    {
        var projectFile = ProjectFiles.Create("StateGame", TestDirectory, new NewProject().SelectedTemplate!);
        var openProject = new OpenProject { ProjectFile = projectFile };
        Assert(openProject.Open() && openProject.OpenedProject != null, "A valid project must load into the browser.");
        File.WriteAllText(projectFile, "{}");
        Assert(!openProject.Open() && openProject.OpenedProject == null && openProject.ErrorMessage.Length > 0,
            "An opening failure must clear stale loaded state and display an error.");
    }
}
