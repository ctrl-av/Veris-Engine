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
}
