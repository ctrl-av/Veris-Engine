using VerisEditor.Workspace;

internal static class HistoryChecks
{
    public static void Run(EditorWorkspace workspace)
    {
        workspace.CreateEntity("History parent");
        var parent = workspace.SelectedEntityId;
        workspace.CreateEntity("History child", parent);
        var child = workspace.SelectedEntityId;
        workspace.SelectEntity(parent);
        workspace.DeleteSelected();
        workspace.Undo();
        Program.Assert(workspace.SelectedEntityId == parent && workspace.RootEntities.Single().Children.Single().Id == child,
            "Undo deletion must restore subtree IDs, relationships, and selection.");
        workspace.Redo();
        Program.Assert(workspace.Entities.Count == 0, "Redo must repeat deletion.");
        workspace.Undo();
        var count = workspace.History.UndoCount;
        workspace.RenameSelected("History parent");
        workspace.ReparentSelected(child);
        Program.Assert(workspace.History.UndoCount == count, "No-op and failed edits must not enter history.");
        workspace.BeginTransformDrag(0, 0);
        workspace.DragTransform(10, 10);
        workspace.DragTransform(20, 20);
        workspace.CompleteTransformDrag();
        Program.Assert(workspace.History.UndoCount == count + 1, "An entire pointer drag must produce one history entry.");
        workspace.Undo();
        Program.Assert(workspace.Inspector.PositionX == "0", "Undo drag must restore its initial transform.");
        workspace.Redo();
        Program.Assert(workspace.Inspector.PositionX == "20", "Redo drag must restore its final transform.");
        workspace.Undo();
        workspace.CreateEntity("Branched");
        Program.Assert(!workspace.History.CanRedo && workspace.SelectedEntityId > child, "New branches must discard redo and never reuse entity IDs.");
        workspace.DeleteSelected();
        workspace.SelectEntity(parent);
        workspace.DeleteSelected();
        workspace.History.Clear();
    }
}
