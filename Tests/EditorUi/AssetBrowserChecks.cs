using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VerisEditor.Workspace;

internal static class AssetBrowserChecks
{
    public static void Run(WorkspaceView view, EditorWorkspace workspace)
    {
        ((TabControl)view.FindName("AssetTabs")).SelectedIndex = 1;
        Program.Arrange(view, 1280, 720);
        var asset = workspace.Catalog.Assets.Single();
        ((TextBox)view.FindName("AssetSearchInput")).Text = "no matching asset";
        Program.Assert(workspace.ImportedAssets.Count == 0, "Asset search must filter the registered list.");
        ((TextBox)view.FindName("AssetSearchInput")).Text = asset.Id.ToString("D");
        var list = (ListBox)view.FindName("ImportedAssetsList");
        list.SelectedIndex = 0;
        Program.Assert(workspace.SelectedAsset?.Id == asset.Id && workspace.AssetPreview != null, "Asset selection must show a decoded preview and metadata.");
        ((TextBox)view.FindName("AssetSearchInput")).Text = "";
        workspace.SelectedAsset = workspace.Catalog.Get(asset.Id);
        workspace.FolderName = "Textures";
        ((Button)view.FindName("CreateFolderButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        workspace.AssetDestination = "Textures/moved.png";
        workspace.MoveSelectedAsset();
        Program.Assert(workspace.Catalog.Get(asset.Id).Path == asset.Path, "Malformed saved scenes must block moves until references can be checked.");
        File.Delete(Path.Combine(workspace.ContentRoot, "invalid.verisscene"));
        workspace.MoveSelectedAsset();
        Program.Assert(workspace.Catalog.Get(asset.Id).Path == "Textures/moved.png" && workspace.Inspector.TexturePath == "Textures/moved.png",
            "Moving an asset must preserve its GUID and update active native resolution.");
        var historyCount = workspace.History.UndoCount;
        workspace.UseSelectedAsset();
        Program.Assert(workspace.History.UndoCount == historyCount, "Assigning the same GUID after a move must remain a no-op scene edit.");
        var entity = workspace.SelectedEntityId;
        Program.Assert(workspace.OpenScene("imported.verisscene", () => UnsavedChoice.Discard), "Previously saved GUID references must survive an asset move.");
        workspace.SelectEntity(entity);
        Program.Assert(workspace.Inspector.HasSprite && workspace.Inspector.TexturePath == "Textures/moved.png", "Moved GUID references must load the new catalog path.");
        var confirmed = false;
        workspace.DeleteSelectedAsset(() => { confirmed = true; return true; });
        Program.Assert(!confirmed && File.Exists(Path.Combine(workspace.ContentRoot, "Textures", "moved.png")), "Referenced assets must be rejected before deletion confirmation.");
        VerifyUnreferencedDeletion(workspace);
        workspace.SelectedAsset = workspace.Catalog.Get(asset.Id);
        workspace.CreateEntity("Browser-assigned sprite");
        ((Button)view.FindName("UseAssetButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Program.Assert(workspace.Inspector.HasSprite, "Use texture must assign the selected registered resource to the native entity.");
        workspace.Inspector.PositionX = "120"; workspace.Inspector.PositionY = "120";
        workspace.Inspector.ScaleX = "48"; workspace.Inspector.ScaleY = "48"; workspace.Inspector.Rotation = "15";
        workspace.ApplyTransform();
        ((EngineViewport)view.FindName("SceneViewport")).RenderFrame();
    }

    private static void VerifyUnreferencedDeletion(EditorWorkspace workspace)
    {
        var source = Path.Combine(Path.GetDirectoryName(workspace.ProjectFile)!, "unused.png");
        ImageFixtures.SaveSolidPng(source, Colors.Green);
        workspace.ImportAsset(source);
        var unused = workspace.Catalog.Assets.Single(asset => asset.Path == "unused.png");
        workspace.SelectedAsset = unused;
        workspace.DeleteSelectedAsset(() => false);
        Program.Assert(File.Exists(Path.Combine(workspace.ContentRoot, unused.Path)), "Cancelled deletion must preserve the file and catalog.");
        workspace.DeleteSelectedAsset(() => true);
        Program.Assert(!File.Exists(Path.Combine(workspace.ContentRoot, unused.Path)) && workspace.Catalog.Assets.Count == 1,
            "Confirmed unreferenced deletion must remove both the file and registry entry.");
    }
}
