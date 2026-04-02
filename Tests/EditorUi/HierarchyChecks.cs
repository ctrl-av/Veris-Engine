using System.Windows;
using System.Windows.Controls;
using VerisEditor.Workspace;

internal static class HierarchyChecks
{
    public static void Run(WorkspaceView view, EditorWorkspace workspace)
    {
        var name = (TextBox)view.FindName("EntityNameInput");
        name.Text = "Parent";
        Click(view, "CreateEntityButton");
        var parent = workspace.SelectedEntityId;
        name.Text = "Child";
        Click(view, "CreateEntityButton");
        var child = workspace.SelectedEntityId;
        ((ComboBox)view.FindName("ParentEntityInput")).SelectedValue = parent;
        Click(view, "ReparentEntityButton");
        Program.Assert(workspace.RootEntities.Count == 1 && workspace.RootEntities[0].Children.Single().Id == child,
            "Set parent must update the native hierarchy and nested WPF nodes.");
        name.Text = "Renamed";
        Click(view, "RenameEntityButton");
        Program.Assert(workspace.SelectedEntity!.Name == "Renamed", "Rename must refresh the selected node from native state.");
        Click(view, "DuplicateEntityButton");
        Program.Assert(workspace.Entities.Count == 3 && workspace.SelectedEntityId != child, "Duplicate must create and select a distinct entity.");
        workspace.SelectEntity(parent);
        workspace.ReparentSelected(child);
        Program.Assert(workspace.Status.Contains("cycles") && workspace.RootEntities[0].Id == parent, "Invalid reparenting must show an error and preserve hierarchy.");
        Click(view, "DeleteEntityButton");
        Program.Assert(workspace.Entities.Count == 0 && workspace.SelectedEntityId == 0, "Deleting a subtree must clear descendant nodes and stale selection.");
    }

    private static void Click(WorkspaceView view, string name)
    {
        ((Button)view.FindName(name)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Program.Arrange(view, 1280, 720);
    }
}
