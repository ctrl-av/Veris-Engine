using System.IO;
using System.Windows.Media;
using VerisEditor.Workspace;

internal static class AssetChangeChecks
{
    public static void Run(EditorWorkspace workspace)
    {
        var asset = workspace.Catalog.Assets.First();
        var path = Path.Combine(workspace.ContentRoot, asset.Path);
        var saved = File.ReadAllBytes(path);
        var revision = workspace.History.Revision;
        File.Delete(path);
        workspace.ReconcileAssets();
        Program.Assert(workspace.AssetDiagnostics.Contains("Missing file"), "Missing assets must retain their GUID and report recovery instructions.");
        File.WriteAllText(path, "broken image");
        workspace.ReconcileAssets();
        Program.Assert(workspace.AssetDiagnostics.Contains(asset.Path), "Corrupt external edits must identify the affected asset.");
        File.WriteAllBytes(path, saved);
        ImageFixtures.SaveSolidPng(path, Colors.Green);
        workspace.ReconcileAssets();
        Program.Assert(workspace.AssetDiagnostics.Length == 0 && workspace.Catalog.Get(asset.Id).Sha256 != asset.Sha256,
            "A repaired asset must preserve identity, update its hash, and clear diagnostics.");
        Program.Assert(workspace.History.Revision == revision, "Asset refresh must not create scene edits or alter undo history.");
    }
}
