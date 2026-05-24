using System.IO;

namespace VerisEditor.Workspace
{
    public sealed partial class EditorWorkspace
    {
        public void ImportAsset(string source) => Run(() =>
        {
            var relative = Path.GetRelativePath(ContentRoot, Path.Combine(CurrentDirectory, Path.GetFileName(source)));
            Catalog.Import(source, relative);
            _engine.LoadAssetCatalog(Catalog.ManifestPath);
            ReadContentDirectory(CurrentDirectory);
        });

        public void ReimportAsset(Guid id, string source) => Run(() =>
        {
            var asset = Catalog.Reimport(id, source);
            _engine.LoadAssetCatalog(Catalog.ManifestPath);
            _engine.ReloadTexture(asset.Path);
            if (HasSelection) Inspector.Load(SelectedEntityId);
        });
    }
}
