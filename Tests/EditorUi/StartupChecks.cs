using System.IO;
using VerisEditor.GameProject;
using VerisEditor.Workspace;

internal static class StartupChecks
{
    public static void Run(EditorWorkspace workspace)
    {
        Program.Assert(workspace.SaveScene("startup.verisscene"), "A scripted starting scene must save.");
        workspace.StartupSceneInput = "startup.verisscene"; workspace.SaveStartingScene();
        Program.Assert(ProjectDocument.Parse(File.ReadAllText(workspace.ProjectFile)).StartupScene == "startup.verisscene", "Starting-scene configuration must persist in the .veris project.");
        workspace.StartupSceneInput = "missing.verisscene"; workspace.SaveStartingScene();
        Program.Assert(workspace.Project.StartupScene == "startup.verisscene", "Invalid starting scenes must preserve the prior project configuration.");
        workspace.NewScene(() => UnsavedChoice.Discard);
        workspace.CreateEntity("Editing scene");
        var id = workspace.SelectedEntityId;
        workspace.PlayStartingScene(); workspace.Pause(); workspace.StepPlay();
        Program.Assert(workspace.RuntimeDiagnostics.Contains("tick"), "Play project must load the configured scene rather than the currently edited scene.");
        workspace.StopPlay();
        Program.Assert(workspace.SelectedEntityId == id && workspace.Entities.Single().Name == "Editing scene", "Stopping a project run must restore the separate editing scene.");
        workspace.StartupSceneInput = ""; workspace.SaveStartingScene();
        Program.Assert(workspace.Project.StartupScene == null, "Starting-scene configuration must support explicit clearing.");
    }
}
