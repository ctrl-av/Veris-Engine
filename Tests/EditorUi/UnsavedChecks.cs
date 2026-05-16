using System.IO;
using VerisEditor.Workspace;

internal static class UnsavedChecks
{
    public static void Run(EditorWorkspace workspace)
    {
        Program.Assert(!workspace.IsDirty, "Successful save/open must establish a clean revision.");
        var original = workspace.SelectedEntity!.Name;
        workspace.RenameSelected("Unsaved rename");
        Program.Assert(workspace.IsDirty && workspace.SceneTitle.EndsWith("*"), "Scene edits must mark the title dirty.");
        workspace.Undo();
        Program.Assert(!workspace.IsDirty, "Undoing to the saved revision must become clean.");
        workspace.Redo();
        Program.Assert(workspace.IsDirty, "Redoing away from the save point must become dirty.");
        Program.Assert(!workspace.CanLeaveScene(() => UnsavedChoice.Cancel) && workspace.SelectedEntity!.Name == "Unsaved rename", "Cancel must preserve the active scene.");
        Program.Assert(workspace.CanLeaveScene(() => UnsavedChoice.Discard) && workspace.IsDirty, "Discard permission must not destroy the current scene before replacement succeeds.");
        var savedFile = workspace.SceneFile!;
        Directory.CreateDirectory(Path.Combine(workspace.ContentRoot, "blocked.verisscene"));
        Program.Assert(!workspace.SaveScene("blocked.verisscene") && workspace.IsDirty && workspace.SceneFile == savedFile, "Failed saves must preserve dirty state and file identity.");
        Program.Assert(!workspace.NewScene(() => UnsavedChoice.Cancel), "Cancel must block new scene creation.");
        Program.Assert(workspace.CanLeaveScene(() => UnsavedChoice.Save) && !workspace.IsDirty, "Save choice must write the scene and become clean.");
        workspace.RenameSelected(original);
        Program.Assert(!workspace.OpenScene("roundtrip.verisscene", () => UnsavedChoice.Cancel), "Scene opening must honor Cancel.");
        Program.Assert(workspace.NewScene(() => UnsavedChoice.Discard) && workspace.Entities.Count == 0 && !workspace.IsDirty,
            "Discard followed by New must create an empty clean scene.");
        var prompted = false;
        Program.Assert(workspace.CanLeaveScene(() => { prompted = true; return UnsavedChoice.Cancel; }) && !prompted, "Clean scenes must not prompt.");
    }
}
