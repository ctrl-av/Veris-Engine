using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using VerisEditor.Assets;

namespace VerisEditor.Workspace
{
    public sealed partial class EditorWorkspace
    {
        private AssetRecord? _selectedAsset;
        private string _assetSearch = "";
        public ObservableCollection<AssetRecord> ImportedAssets { get; } = new();
        public string AssetSearch { get => _assetSearch; set { _assetSearch = value; RefreshAssetList(); OnPropertyChanged(nameof(AssetSearch)); } }
        public string FolderName { get; set; } = "Textures";
        public string AssetDestination { get; set; } = "";
        public BitmapSource? AssetPreview { get; private set; }
        public string AssetDetails { get; private set; } = "Select an imported texture";
        public bool HasSelectedAsset => _selectedAsset != null;
        public AssetRecord? SelectedAsset
        {
            get => _selectedAsset;
            set
            {
                _selectedAsset = value;
                AssetDestination = value?.Path ?? "";
                LoadAssetPreview();
                OnPropertyChanged(nameof(SelectedAsset));
                OnPropertyChanged(nameof(HasSelectedAsset));
                OnPropertyChanged(nameof(AssetDestination));
            }
        }

        public void ImportAsset(string source) => Run(() =>
        {
            var relative = Path.GetRelativePath(ContentRoot, Path.Combine(CurrentDirectory, Path.GetFileName(source)));
            Catalog.Import(source, relative);
            _engine.LoadAssetCatalog(Catalog.ManifestPath);
            ReadContentDirectory(CurrentDirectory);
            RefreshAssetList();
        });

        public void ReimportAsset(Guid id, string source) => Run(() =>
        {
            var asset = Catalog.Reimport(id, source);
            _engine.LoadAssetCatalog(Catalog.ManifestPath);
            ReloadAsset(asset);
            if (HasSelection) Inspector.Load(SelectedEntityId);
            RefreshAssetList();
        });

        public void CreateContentFolder() => Run(() =>
        {
            if (Path.GetFileName(FolderName) != FolderName) throw new InvalidDataException("Enter a single folder name.");
            Catalog.CreateFolder(Path.GetRelativePath(ContentRoot, Path.Combine(CurrentDirectory, FolderName)));
            ReadContentDirectory(CurrentDirectory);
        });

        public void RegisterContent(string relativePath) => Run(() =>
        {
            var asset = Catalog.RegisterExisting(relativePath);
            _engine.LoadAssetCatalog(Catalog.ManifestPath);
            RefreshAssetList();
            SelectedAsset = asset;
        });

        public void MoveSelectedAsset() => Run(() =>
        {
            if (SelectedAsset == null) return;
            if (_engine.AssetUsage(SelectedAsset.Id).Legacy != 0) throw new IOException("Reassign path-based scene or undo references before moving this asset.");
            var asset = Catalog.Move(SelectedAsset.Id, AssetDestination);
            _engine.LoadAssetCatalog(Catalog.ManifestPath);
            ReadContentDirectory(CurrentDirectory);
            RefreshAssetList();
            SelectedAsset = asset;
            if (HasSelection) Inspector.Load(SelectedEntityId);
        });

        public void UseSelectedAsset() => Edit("Assign texture asset", () =>
        {
            if (SelectedAsset == null || !HasSelection) return;
            if (SelectedAsset.Kind == "audio")
            {
                _engine.SetAudio(SelectedEntityId, System.Text.Json.JsonSerializer.Serialize(new { clip = SelectedAsset.Path, assetId = SelectedAsset.Id, volume = 100, loop = false, autoPlay = false }));
                AudioJson = _engine.Audio(SelectedEntityId);
                OnPropertyChanged(nameof(AudioJson));
                return;
            }
            var sprite = _engine.GetSprite(SelectedEntityId) ?? new Interop.SpriteProperties { Tint = uint.MaxValue, Visible = 1 };
            _engine.SetSprite(SelectedEntityId, SelectedAsset.Path, sprite);
            Inspector.Load(SelectedEntityId);
        });

        public void DeleteSelectedAsset(Func<bool> confirm) => Run(() =>
        {
            if (SelectedAsset == null) return;
            var usage = _engine.AssetUsage(SelectedAsset.Id);
            if (usage.Stable != 0 || usage.Legacy != 0 || Catalog.HasSavedReference(SelectedAsset)) throw new IOException("This asset is referenced by an active scene, undo history, or a saved scene.");
            if (!confirm()) return;
            Catalog.Delete(SelectedAsset.Id);
            _engine.LoadAssetCatalog(Catalog.ManifestPath);
            SelectedAsset = null;
            ReadContentDirectory(CurrentDirectory);
            RefreshAssetList();
        });

        private void RefreshAssetList()
        {
            var selected = SelectedAsset?.Id;
            ImportedAssets.Clear();
            foreach (var asset in Catalog.Assets)
                if (asset.Path.Contains(AssetSearch, StringComparison.OrdinalIgnoreCase) || asset.Id.ToString("D").Contains(AssetSearch, StringComparison.OrdinalIgnoreCase)) ImportedAssets.Add(asset);
            SelectedAsset = null;
            foreach (var asset in ImportedAssets) if (asset.Id == selected) { SelectedAsset = asset; break; }
        }

        private void LoadAssetPreview()
        {
            AssetPreview = null;
            AssetDetails = SelectedAsset == null ? "Select an imported texture" : $"{SelectedAsset.Path}\n{SelectedAsset.Id:D}\nSHA-256: {SelectedAsset.Sha256}";
            if (SelectedAsset?.Kind == "texture")
            {
                try
                {
                    using var stream = File.OpenRead(ContentPaths.Resolve(ContentRoot, SelectedAsset.Path));
                    var preview = new BitmapImage();
                    preview.BeginInit(); preview.CacheOption = BitmapCacheOption.OnLoad; preview.DecodePixelWidth = 128; preview.StreamSource = stream; preview.EndInit(); preview.Freeze();
                    AssetPreview = preview;
                }
                catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or NotSupportedException)
                {
                    AssetDetails += "\nPreview unavailable: " + exception.Message;
                }
            }
            OnPropertyChanged(nameof(AssetPreview));
            OnPropertyChanged(nameof(AssetDetails));
        }
    }
}
