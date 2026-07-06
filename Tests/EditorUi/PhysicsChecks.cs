using VerisEditor.Workspace;

internal static class PhysicsChecks
{
    public static void Run(EditorWorkspace workspace)
    {
        workspace.CreateEntity("Physics fixture");
        var id = workspace.SelectedEntityId;
        workspace.PhysicsJson = "{\"type\":2,\"shape\":0,\"halfSize\":[16,16],\"radius\":16,\"offset\":[0,0],\"velocity\":[60,0],\"angularVelocity\":0,\"gravityScale\":0,\"density\":1,\"friction\":0.3,\"restitution\":0,\"fixedRotation\":true,\"bullet\":false}";
        workspace.ApplyPhysics();
        workspace.StepPhysics();
        Program.Assert(float.Parse(workspace.Inspector.PositionX, System.Globalization.CultureInfo.InvariantCulture) > 0.9f, "Editor simulation must advance actual Box2D bodies.");
        workspace.Undo();
        Program.Assert(workspace.Inspector.PositionX == "0", "Undo must restore the position before a manual physics step.");
        workspace.Undo();
        Program.Assert(workspace.PhysicsJson == "null", "Undo must remove a newly added physics component.");
        workspace.Redo();
        Program.Assert(workspace.SaveScene("physics.verisscene") && workspace.OpenScene("physics.verisscene", () => UnsavedChoice.Discard), "Physics scenes must save and reopen.");
        workspace.SelectEntity(id);
        Program.Assert(workspace.PhysicsJson.Contains("halfSize"), "Physics geometry must survive editor scene loading.");
    }
}
