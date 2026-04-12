using System.Windows;
using System.Windows.Controls;
using VerisEditor.Workspace;

internal static class SelectionChecks
{
    public static void Run(WorkspaceView view, EditorWorkspace workspace)
    {
        workspace.CreateEntity("First");
        var first = workspace.SelectedEntityId;
        workspace.CreateEntity("Second");
        var second = workspace.SelectedEntityId;
        var viewport = (EngineViewport)view.FindName("SceneViewport");
        viewport.RenderFrame();
        viewport.SelectAt(new Point(2, 2));
        Program.Arrange(view, 1280, 720);
        var tree = (TreeView)view.FindName("SceneEntities");
        Program.Assert(workspace.SelectedEntityId == second && ((WorkspaceEntity)tree.SelectedItem).Id == second,
            "Viewport picking must synchronize hierarchy selection by stable entity ID.");
        var firstNode = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromItem(workspace.RootEntities.Single(entity => entity.Id == first));
        firstNode.IsSelected = true;
        Program.Assert(workspace.SelectedEntityId == first, "Hierarchy selection must update the viewport's shared selection.");
        workspace.Refresh();
        Program.Assert(workspace.SelectedEntityId == first, "Refresh must preserve selection across rebuilt tree nodes.");
        viewport.SelectAt(new Point(100, 100));
        Program.Assert(workspace.SelectedEntityId == 0, "Clicking empty viewport space must clear shared selection.");
        workspace.SelectEntity(first);
        workspace.DeleteSelected();
        Program.Assert(workspace.SelectedEntityId == 0, "Deleting a selected entity must clear shared selection.");
        workspace.SelectEntity(second);
        workspace.DeleteSelected();
    }
}
