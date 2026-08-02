using System.IO;
using VerisEditor.Workspace;

internal static class PlayChecks
{
    public static void Run(EditorWorkspace workspace)
    {
        workspace.NewScene(() => UnsavedChoice.Discard);
        workspace.CreateEntity("Scripted player");
        var id = workspace.SelectedEntityId;
        var code = Path.Combine(Path.GetDirectoryName(workspace.ProjectFile)!, workspace.Project.GameCodeDirectory);
        File.WriteAllText(Path.Combine(code, "player.lua"), "return {update=function(dt) local x,y=engine.position(self); local down=engine.action('MoveRight'); if down then x=x+60*dt end; engine.set_position(self,x,y); print('tick',x) end}");
        workspace.ScriptPath = "player.lua"; workspace.ApplyScript();
        var revision = workspace.History.Revision;
        var dirty = workspace.IsDirty;
        workspace.Play(); workspace.Pause();
        Program.Assert(workspace.Playback == PlaybackState.Paused && !workspace.CanEditScene, "Play must create an isolated paused runtime scene.");
        workspace.SetPlayInput("Key:D", true); workspace.StepPlay();
        Program.Assert(workspace.RuntimeDiagnostics.Contains("tick\t1"), "Stepping must execute Lua with configured input in the runtime scene.");
        workspace.Inspector.PositionX = "99"; workspace.ApplyTransform();
        Program.Assert(!workspace.SaveScene("during-play.verisscene"), "Saving runtime state over the editing scene must be blocked.");
        workspace.Play(); workspace.StopPlay();
        Program.Assert(workspace.SelectedEntityId == id && workspace.Inspector.PositionX == "0" && workspace.History.Revision == revision && workspace.IsDirty == dirty,
            "Stop must preserve editing transforms, selection, dirty state and undo history.");
        workspace.Play(); workspace.Pause(); workspace.StepPlay(); workspace.StopPlay();
        Program.Assert(workspace.RuntimeDiagnostics.Contains("tick\t0"), "A new play session must start with fresh script state and released input.");
    }
}
