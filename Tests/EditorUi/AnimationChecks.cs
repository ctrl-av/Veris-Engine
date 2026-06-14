using System.Windows;
using System.Windows.Controls;
using VerisEditor.Workspace;

internal static class AnimationChecks
{
    public static void Run(WorkspaceView view, EditorWorkspace workspace)
    {
        var before = workspace.History.UndoCount;
        workspace.AnimationJson = "{\"frames\":[{\"rectangle\":[0,0,1,1],\"seconds\":0.1},{\"rectangle\":[1,0,1,1],\"seconds\":0.2}],\"loop\":true,\"playing\":true}";
        ((Button)view.FindName("ApplyAnimationButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Program.Assert(workspace.History.UndoCount == before + 1, "Animation edits must participate in undo history.");
        workspace.PreviewAnimation();
        workspace.Undo();
        Program.Assert(workspace.AnimationJson == "null", "Undo must remove the newly added animation.");
        workspace.Redo();
        Program.Assert(workspace.AnimationJson.Contains("frames"), "Redo must restore animation frames.");
        Program.Assert(workspace.SaveScene("animation.verisscene"), "Animated scenes must save.");
        Program.Assert(workspace.OpenScene("animation.verisscene", () => UnsavedChoice.Discard), "Animated scenes must reopen.");
        workspace.SelectEntity(workspace.Entities.Last().Id);
        Program.Assert(workspace.AnimationJson.Contains("frames"), "Scene loading must preserve animation configuration.");
    }
}
