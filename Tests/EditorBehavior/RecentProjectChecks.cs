using VerisEditor.GameProject;

internal static partial class Program
{
    private static void RecentProjects_PersistsOrderAndDeduplicatesMovedProjects()
    {
        var parent = Path.Combine(TestDirectory, "RecentOrdering");
        var template = new NewProject().SelectedTemplate!;
        var first = ProjectFiles.Create("FirstGame", parent, template);
        var second = ProjectFiles.Create("SecondGame", parent, template);
        var historyFile = Path.Combine(parent, "history.json");
        var history = new RecentProjects(historyFile);
        var firstProject = ProjectFiles.Open(first);
        history.Record(first, firstProject);
        history.Record(second, ProjectFiles.Open(second));
        history.Record(first, firstProject);
        Assert(new RecentProjects(historyFile).Load()[0].ProjectFile == first, "Reopening must move a project to the top without duplicates.");
        var movedDirectory = Path.Combine(parent, "MovedFirst");
        Directory.Move(Path.GetDirectoryName(first)!, movedDirectory);
        history.Record(Path.Combine(movedDirectory, "project.veris"), firstProject);
        Assert(history.Load().Count == 2 && history.Load()[0].ProjectFile.Contains("MovedFirst"), "Moved project identity must replace its old history path.");
    }

    private static void RecentProjects_HandlesMissingFilesAndRemoval()
    {
        var parent = Path.Combine(TestDirectory, "RecentMissing");
        var projectFile = ProjectFiles.Create("MissingGame", parent, new NewProject().SelectedTemplate!);
        var history = new RecentProjects(Path.Combine(parent, "history.json"));
        history.Record(projectFile, ProjectFiles.Open(projectFile));
        File.Delete(projectFile);
        Assert(!history.Load()[0].IsAvailable, "Deleted projects must remain identifiable as unavailable.");
        var browser = new OpenProject(history) { SelectedRecentProject = history.Load()[0] };
        Assert(!browser.Open() && browser.ErrorMessage.Length > 0, "Opening a missing recent project must report an error.");
        browser.RemoveSelectedRecentProject();
        Assert(history.Load().Count == 0 && browser.RecentProjectEntries.Count == 0, "Removing a recent project must persist and refresh the browser.");
    }

    private static void RecentProjects_LimitsHistoryAndPreservesCorruptHistory()
    {
        var historyFile = Path.Combine(TestDirectory, "RecentLimits", "history.json");
        var history = new RecentProjects(historyFile);
        for (var index = 0; index < 22; index++)
            history.Record(Path.Combine(TestDirectory, $"Recorded{index}.veris"), CreateProjectDocument());
        Assert(history.Load().Count == 20 && history.Load()[0].ProjectFile.EndsWith("Recorded21.veris"), "History must retain only the 20 most recent entries.");
        File.WriteAllText(historyFile, "corrupt history");
        Reject<System.Text.Json.JsonException>(() => history.Record(Path.Combine(TestDirectory, "New.veris"), CreateProjectDocument()));
        Assert(File.ReadAllText(historyFile) == "corrupt history", "Malformed history must not be silently overwritten.");
        var browser = new OpenProject(history);
        Assert(browser.ErrorMessage.Length > 0, "Corrupt history must produce a browser warning without blocking manual project opening.");
    }
}
