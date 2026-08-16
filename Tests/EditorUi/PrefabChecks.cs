using System.IO;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows;
using VerisEditor.Workspace;

internal static class PrefabChecks
{
    public static void Run(WorkspaceView view, EditorWorkspace workspace)
    {
        var image = Path.Combine(Path.GetDirectoryName(workspace.ProjectFile)!, "prefab-only.png");
        ImageFixtures.SaveSolidPng(image, Colors.Orange); workspace.ImportAsset(image);
        var asset = workspace.Catalog.Assets.Single(asset => asset.Path == "prefab-only.png");
        workspace.CreateEntity("Prefab source"); var source = workspace.SelectedEntityId;
        workspace.SelectedAsset = asset; workspace.UseSelectedAsset();
        workspace.Inspector.ScaleX = "24"; workspace.Inspector.ScaleY = "24"; workspace.ApplyTransform();
        workspace.CreateEntity("Child", source); workspace.SelectEntity(source);
        workspace.PrefabPathInput = "actor.verisprefab";
        Program.Assert(!workspace.Catalog.HasSavedReference(asset), "Prefab fixture must start without saved references.");
        workspace.SavePrefab();
        Program.Assert(workspace.Catalog.HasSavedReference(asset), "Saved prefab assets must protect referenced textures from deletion.");
        workspace.PrefabX = "250"; workspace.PrefabY = "100"; workspace.InstantiatePrefab();
        var instance = workspace.SelectedEntityId;
        Program.Assert(workspace.PrefabSource == "actor.verisprefab" && workspace.Inspector.HasSprite, "Prefab instantiation must assign source metadata and copied components.");
        workspace.RenameSelected("Override"); workspace.CreateEntity("Added override", instance);
        workspace.SelectEntity(source); workspace.RenameSelected("Updated source"); workspace.SavePrefab();
        Program.Assert(workspace.Entities.Single(entity => entity.Id == instance).Name == "Override", "Saving a source must preserve existing instance overrides.");
        workspace.SelectEntity(instance); workspace.ReapplyPrefab();
        Program.Assert(workspace.SelectedEntity!.Name == "Updated source" && !workspace.Entities.Any(entity => entity.Name == "Added override") && workspace.Inspector.PositionX == "250",
            "Reapply must discard subtree overrides while retaining placement and root selection.");
        workspace.Undo();
        Program.Assert(workspace.SelectedEntity!.Name == "Override" && workspace.Entities.Any(entity => entity.Name == "Added override"), "Undo must restore prefab subtree overrides and selection.");
        workspace.Redo();
        Program.Assert(workspace.SaveScene("prefab-instance.verisscene"), "Linked prefab instances must save as baked scene content.");
        ((Expander)view.FindName("PrefabInspector")).IsExpanded = true;
        Program.Arrange(view, 1280, 720);
        ((Expander)view.FindName("PrefabInspector")).BringIntoView();
        var viewport = (EngineViewport)view.FindName("SceneViewport");
        viewport.RenderFrame();
        var pixel = new byte[4];
        viewport.Frame!.CopyPixels(new Int32Rect(255, 105, 1, 1), pixel, 4, 0);
        Program.Assert(pixel[0] == 0 && pixel[1] == 165 && pixel[2] == 255, "Instantiated prefab sprites must render their copied textures and transforms in the viewport.");
        Program.SavePreview(view, "editor-prefabs.png");
        File.Delete(Path.Combine(workspace.ContentRoot, "actor.verisprefab"));
        Program.Assert(workspace.OpenScene("prefab-instance.verisscene", () => UnsavedChoice.Discard), "Missing prefab sources must not prevent reopening baked instances.");
        workspace.SelectEntity(instance); workspace.UnpackPrefab();
        Program.Assert(workspace.PrefabSource == "" && workspace.Inspector.HasSprite, "Unpack must remove the link while preserving entity components.");
    }
}
