using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VerisEditor.Assets
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed record AssetRecord(Guid Id, string Path, string Kind, string Sha256);

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    internal sealed class AssetManifest
    {
        public required int FormatVersion { get; init; }
        public required List<AssetRecord> Assets { get; init; }
    }

    public sealed partial class AssetCatalog
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
        private readonly string _contentRoot;
        private readonly string _projectDirectory;
        private readonly Action<string> _validateTexture;
        private List<AssetRecord> _assets;
        public string ManifestPath { get; }
        public IReadOnlyList<AssetRecord> Assets => _assets;

        public AssetCatalog(string contentRoot, string projectDirectory, Action<string> validateTexture)
        {
            _contentRoot = contentRoot;
            _projectDirectory = projectDirectory;
            ManifestPath = ContentPaths.Resolve(projectDirectory, ".Veris/assets.json");
            _validateTexture = validateTexture;
            _assets = ReadManifest();
        }

        public AssetRecord Import(string source, string destination)
        {
            var path = ContentPaths.Resolve(_contentRoot, destination);
            foreach (var asset in _assets)
                if (asset.Path.Equals(destination.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase)) throw new IOException("This destination is already registered; use Reimport to replace its contents.");
            if (File.Exists(path) && !Path.GetFullPath(source).Equals(path, StringComparison.OrdinalIgnoreCase)) throw new IOException("Import destination already exists.");
            if (Path.GetFullPath(source).Equals(path, StringComparison.OrdinalIgnoreCase)) return RegisterExisting(destination);
            return Publish(new AssetRecord(Guid.NewGuid(), destination.Replace('\\', '/'), "texture", ""), source);
        }

        public AssetRecord RegisterExisting(string relativePath)
        {
            var path = ContentPaths.Resolve(_contentRoot, relativePath);
            foreach (var asset in _assets) if (asset.Path.Equals(relativePath.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase)) return asset;
            ValidateTexture(path);
            var record = new AssetRecord(Guid.NewGuid(), relativePath.Replace('\\', '/'), "texture", Hash(path));
            var replacement = new List<AssetRecord>(_assets) { record };
            Save(replacement);
            return record;
        }

        public AssetRecord Reimport(Guid id, string source) => Publish(Get(id), source);

        public AssetRecord Get(Guid id)
        {
            foreach (var asset in _assets) if (asset.Id == id) return asset;
            throw new InvalidDataException("Asset identifier is not registered in this project.");
        }

        private AssetRecord Publish(AssetRecord asset, string source)
        {
            var destination = ContentPaths.Resolve(_contentRoot, asset.Path);
            var staged = Path.Combine(Path.GetDirectoryName(destination)!, $".veris-import-{Guid.NewGuid():N}.tmp");
            var backup = staged + ".backup";
            var existing = _assets.Exists(registered => registered.Id == asset.Id);
            try
            {
                if (new FileInfo(source).Length > 256 * 1024 * 1024) throw new InvalidDataException("Texture imports cannot exceed 256 MiB.");
                File.Copy(source, staged);
                _validateTexture(staged);
                var replacement = asset with { Sha256 = Hash(staged) };
                var records = new List<AssetRecord>(_assets);
                records.RemoveAll(registered => registered.Id == asset.Id);
                records.Add(replacement);
                if (existing && File.Exists(destination)) File.Copy(destination, backup);
                File.Move(staged, destination, existing);
                try { Save(records); }
                catch { if (File.Exists(backup)) File.Move(backup, destination, true); else File.Delete(destination); throw; }
                return replacement;
            }
            finally { if (File.Exists(staged)) File.Delete(staged); if (File.Exists(backup)) File.Delete(backup); }
        }

        private List<AssetRecord> ReadManifest()
        {
            if (!File.Exists(ManifestPath)) return new List<AssetRecord>();
            if (new FileInfo(ManifestPath).Length > 16 * 1024 * 1024) throw new InvalidDataException("Asset manifest cannot exceed 16 MiB.");
            try
            {
                var manifest = JsonSerializer.Deserialize<AssetManifest>(File.ReadAllText(ManifestPath), JsonOptions) ?? throw new InvalidDataException("Asset manifest cannot be null.");
                if (manifest.FormatVersion != 1 || manifest.Assets == null) throw new InvalidDataException("Unsupported asset manifest format.");
                ValidateRecords(manifest.Assets);
                return manifest.Assets;
            }
            catch (JsonException exception) { throw new InvalidDataException("Asset manifest is not valid version 1 JSON.", exception); }
        }

        private void ValidateRecords(List<AssetRecord> assets)
        {
            var identities = new HashSet<Guid>();
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var asset in assets)
            {
                if (asset == null || asset.Id == Guid.Empty || !identities.Add(asset.Id) || string.IsNullOrWhiteSpace(asset.Path) || !paths.Add(asset.Path.Replace('\\', '/')))
                    throw new InvalidDataException("Asset identities and paths must be nonempty and unique.");
                ContentPaths.Resolve(_contentRoot, asset.Path);
                if (asset.Kind != "texture" || asset.Sha256 == null || asset.Sha256.Length != 64 || !asset.Sha256.All(Uri.IsHexDigit))
                    throw new InvalidDataException("Asset entries require a supported kind and SHA-256 digest.");
            }
        }

        private void Save(List<AssetRecord> records)
        {
            ValidateRecords(records);
            ContentPaths.Resolve(_projectDirectory, ".Veris/assets.json");
            Directory.CreateDirectory(Path.GetDirectoryName(ManifestPath)!);
            AtomicFile.WriteText(ManifestPath, JsonSerializer.Serialize(new AssetManifest { FormatVersion = 1, Assets = records }, JsonOptions));
            _assets = records;
        }

        private void ValidateTexture(string path)
        {
            if (new FileInfo(path).Length > 256 * 1024 * 1024) throw new InvalidDataException("Texture imports cannot exceed 256 MiB.");
            _validateTexture(path);
        }

        private static string Hash(string path)
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        }
    }
}
