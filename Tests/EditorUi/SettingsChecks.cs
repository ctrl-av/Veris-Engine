using System.IO;
using VerisEditor.GameProject;
using VerisEditor.Workspace;

internal static class SettingsChecks
{
    public static void Run(EditorWorkspace workspace)
    {
        var before = File.ReadAllText(workspace.ProjectFile);
        workspace.SettingWidth = "invalid"; workspace.SaveProjectSettings();
        Program.Assert(File.ReadAllText(workspace.ProjectFile) == before, "Invalid numeric settings must preserve the project file.");
        workspace.SettingWidth = "800"; workspace.SettingHeight = "600"; workspace.SettingMasterVolume = "101"; workspace.SaveProjectSettings();
        Program.Assert(File.ReadAllText(workspace.ProjectFile) == before, "Out-of-range audio settings must not publish partial window changes.");
        var revision = workspace.History.Revision;
        workspace.SettingMasterVolume = "25"; workspace.SettingGravityY = "0"; workspace.SettingVsync = false; workspace.SaveProjectSettings();
        var reopened = ProjectDocument.Parse(File.ReadAllText(workspace.ProjectFile));
        Program.Assert(reopened.WindowWidth == 800 && reopened.WindowHeight == 600 && reopened.MasterVolume == 25 && reopened.GravityY == 0 && !reopened.VerticalSync && workspace.History.Revision == revision,
            "Settings must persist independently from scene history.");
        var inputPath = Path.Combine(Path.GetDirectoryName(workspace.ProjectFile)!, ".Veris", "input-actions.json");
        var inputBefore = File.ReadAllText(inputPath);
        workspace.InputBindingsJson = "{\"formatVersion\":1,\"actions\":{\"MoveRight\":[\"Key:Invalid\"]}}"; workspace.SaveInputBindings();
        Program.Assert(File.ReadAllText(inputPath) == inputBefore, "Unknown input bindings must be rejected before writing.");
        workspace.InputBindingsJson = inputBefore; workspace.SaveInputBindings();
        var view = new ProjectSettingsView(workspace); Program.Arrange(view, 440, 720); Program.SavePreview(view, "project-settings.png");
    }
}
