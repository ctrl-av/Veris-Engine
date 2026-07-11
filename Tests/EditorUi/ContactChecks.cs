using System.Text.Json.Nodes;
using VerisEditor.Workspace;

internal static class ContactChecks
{
    public static void Run(WorkspaceView view, EditorWorkspace workspace)
    {
        var body = JsonNode.Parse(workspace.PhysicsJson)!;
        body["velocity"] = new JsonArray(0, 0);
        workspace.NewScene(() => UnsavedChoice.Discard);
        workspace.CreateEntity("Solid contact fixture");
        body["type"] = 0;
        workspace.PhysicsJson = body.ToJsonString(); workspace.ApplyPhysics();
        workspace.Inspector.PositionX = "50"; workspace.Inspector.PositionY = "50"; workspace.ApplyTransform();
        workspace.CreateEntity("Dynamic contact fixture");
        body["type"] = 2;
        workspace.PhysicsJson = body.ToJsonString(); workspace.ApplyPhysics();
        workspace.Inspector.PositionX = "78"; workspace.Inspector.PositionY = "50"; workspace.ApplyTransform();
        workspace.StepPhysics();
        Program.Assert(workspace.ContactDetails.Contains("begin") && workspace.ContactDetails.Contains("normal"), "Editor simulation must publish actual collision diagnostics.");
        var revision = workspace.History.Revision;
        workspace.PhysicsDebug = true;
        ((EngineViewport)view.FindName("SceneViewport")).RenderFrame();
        Program.Assert(workspace.History.Revision == revision, "Physics overlays must not dirty the scene.");
        Program.Arrange(view, 1280, 720);
        Program.SavePreview(view, "editor-physics.png");
    }
}
