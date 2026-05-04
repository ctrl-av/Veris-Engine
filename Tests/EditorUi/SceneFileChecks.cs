using System.IO;
using VerisEditor.Workspace;

internal static class SceneFileChecks
{
    public static void Run(EditorWorkspace workspace)
    {
        var id = workspace.SelectedEntityId;
        var name = workspace.SelectedEntity!.Name;
        Program.Assert(workspace.SaveScene("roundtrip.verisscene"), "Scene save must write a valid scene file.");
        workspace.RenameSelected("Changed after save");
        Program.Assert(workspace.OpenScene("roundtrip.verisscene"), "Scene open must load saved entities and textures.");
        workspace.SelectEntity(id);
        Program.Assert(workspace.SelectedEntity!.Name == name && workspace.Inspector.HasSprite && !workspace.History.CanUndo,
            "Opening a scene must restore stable IDs and sprite components and clear old-scene history.");
        workspace.RenameSelected("Replaced save");
        Program.Assert(workspace.SaveScene("roundtrip.verisscene"), "Saving an existing scene must replace it atomically.");
        var activeFile = workspace.SceneFile;
        File.WriteAllText(Path.Combine(workspace.ContentRoot, "invalid.verisscene"), "{}");
        Program.Assert(!workspace.OpenScene("invalid.verisscene") && workspace.SceneFile == activeFile && workspace.SelectedEntityId == id,
            "Failed loads must preserve the active scene, file identity, and selection.");
        Program.Assert(!workspace.SaveScene("../outside.verisscene"), "Scene saves must stay inside the content root.");
        Program.Assert(!Directory.GetFiles(workspace.ContentRoot, ".veris-save-*.tmp").Any(), "Atomic saves must not leave temporary files.");
    }
}
