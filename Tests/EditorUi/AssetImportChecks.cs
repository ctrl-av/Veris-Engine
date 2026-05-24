using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using VerisEditor.Assets;
using VerisEditor.Interop;
using VerisEditor.Workspace;

internal static class AssetImportChecks
{
    public static void Run(WorkspaceView view, EditorWorkspace workspace)
    {
        var projectDirectory = Path.GetDirectoryName(workspace.ProjectFile)!;
        var source = Path.Combine(projectDirectory, "imported.png");
        ImageFixtures.SaveSolidPng(source, Colors.Red);
        workspace.ImportAsset(source);
        var asset = workspace.Catalog.Assets.Single();
        Program.Assert(asset.Id != Guid.Empty && asset.Sha256.Length == 64 && File.Exists(Path.Combine(workspace.ContentRoot, asset.Path)), "Import must copy image bytes and persist a stable identity and content hash.");
        var reopened = new AssetCatalog(workspace.ContentRoot, projectDirectory, NativeEngine.ValidateTextureFile);
        Program.Assert(reopened.Assets.Single().Id == asset.Id, "Asset identities must survive reopening the catalog.");
        workspace.CreateEntity("Imported sprite");
        workspace.Inspector.ScaleX = "16";
        workspace.Inspector.ScaleY = "16";
        workspace.ApplyTransform();
        workspace.Inspector.TexturePath = asset.Path;
        workspace.ApplySprite();
        Program.Assert(workspace.SaveScene("imported.verisscene"), "Imported assets must be usable in saved scenes.");
        using var scene = JsonDocument.Parse(File.ReadAllText(workspace.SceneFile!));
        Program.Assert(scene.RootElement.GetProperty("entities")[0].GetProperty("sprite").GetProperty("assetId").GetGuid() == asset.Id, "Scene sprites must store the imported GUID.");
        ImageFixtures.SaveSolidPng(source, Colors.Blue);
        workspace.ReimportAsset(asset.Id, source);
        Program.Assert(workspace.Catalog.Assets.Single().Id == asset.Id && workspace.Catalog.Assets.Single().Sha256 != asset.Sha256, "Reimport must preserve identity and update the content hash.");
        var viewport = (EngineViewport)view.FindName("SceneViewport");
        viewport.RenderFrame();
        var pixel = new byte[4];
        viewport.Frame!.CopyPixels(new Int32Rect(2, 2, 1, 1), pixel, 4, 0);
        Program.Assert(pixel[0] == 255 && pixel[2] == 0, "Reimport must update live native texture references.");
        var invalid = Path.Combine(projectDirectory, "invalid.png");
        File.WriteAllText(invalid, "not an image");
        workspace.ImportAsset(invalid);
        Program.Assert(workspace.Catalog.Assets.Count == 1 && !File.Exists(Path.Combine(workspace.ContentRoot, "invalid.png")), "Failed image decoding must leave files and registry unchanged.");
        workspace.ImportAsset(source);
        Program.Assert(workspace.Catalog.Assets.Count == 1, "Import collisions must not silently overwrite or duplicate an asset.");
    }
}
