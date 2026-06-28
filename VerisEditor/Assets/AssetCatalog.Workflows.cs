using System.IO;
using System.Text.Json;

namespace VerisEditor.Assets
{
    public sealed partial class AssetCatalog
    {
        public AssetRecord Move(Guid id, string destination)
        {
            var asset = Get(id);
            var normalized = destination.Replace('\\', '/');
            if (asset.Path == normalized) return asset;
            var source = ContentPaths.Resolve(_contentRoot, asset.Path);
            var target = ContentPaths.Resolve(_contentRoot, normalized);
            if (HasSavedLegacyReference(asset)) throw new IOException("Reassign path-based scene references before moving this asset.");
            if (File.Exists(target) && !source.Equals(target, StringComparison.OrdinalIgnoreCase)) throw new IOException("Asset destination already exists.");
            foreach (var registered in _assets)
                if (registered.Id != id && registered.Path.Equals(normalized, StringComparison.OrdinalIgnoreCase)) throw new IOException("Asset destination is already registered.");
            var staged = Path.Combine(Path.GetDirectoryName(source)!, $".veris-move-{Guid.NewGuid():N}.tmp");
            var moved = asset with { Path = normalized };
            File.Move(source, staged);
            try
            {
                File.Move(staged, target);
                try { Save(ReplaceRecord(moved)); }
                catch { File.Move(target, staged); throw; }
            }
            catch { if (File.Exists(staged)) File.Move(staged, source); throw; }
            return moved;
        }

        public void Delete(Guid id)
        {
            var asset = Get(id);
            if (HasSavedReference(asset)) throw new IOException("A saved scene still references this asset.");
            var path = ContentPaths.Resolve(_contentRoot, asset.Path);
            var staged = Path.Combine(Path.GetDirectoryName(path)!, $".veris-delete-{Guid.NewGuid():N}.tmp");
            var replacement = new List<AssetRecord>(_assets);
            replacement.RemoveAll(registered => registered.Id == id);
            File.Move(path, staged);
            try { Save(replacement); }
            catch { File.Move(staged, path); throw; }
            File.Delete(staged);
        }

        public void CreateFolder(string relativePath)
        {
            var path = ContentPaths.Resolve(_contentRoot, relativePath);
            if (File.Exists(path) || Directory.Exists(path)) throw new IOException("Folder destination already exists.");
            Directory.CreateDirectory(path);
        }

        private List<AssetRecord> ReplaceRecord(AssetRecord replacement)
        {
            var records = new List<AssetRecord>(_assets);
            var index = records.FindIndex(asset => asset.Id == replacement.Id);
            records[index] = replacement;
            return records;
        }

        private IEnumerable<JsonDocument> SavedScenes()
        {
            var options = new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.ReparsePoint, IgnoreInaccessible = false };
            foreach (var path in Directory.EnumerateFiles(_contentRoot, "*.verisscene", options))
            {
                if (new FileInfo(path).Length > 16 * 1024 * 1024) throw new InvalidDataException("Cannot validate references in an oversized saved scene.");
                JsonDocument scene;
                try { scene = JsonDocument.Parse(File.ReadAllText(path)); }
                catch (JsonException exception) { throw new InvalidDataException("Cannot validate references in malformed scene: " + path, exception); }
                try { ValidateReferenceDocument(scene); }
                catch (Exception exception) when (exception is InvalidDataException or InvalidOperationException or KeyNotFoundException)
                {
                    scene.Dispose();
                    throw new InvalidDataException("Cannot validate references in malformed scene: " + path, exception);
                }
                yield return scene;
            }
        }

        private static void ValidateReferenceDocument(JsonDocument scene)
        {
            if (scene.RootElement.GetProperty("formatVersion").GetInt32() != 1) throw new InvalidDataException("Unsupported saved scene version.");
            foreach (var entity in scene.RootElement.GetProperty("entities").EnumerateArray())
            foreach (var sprite in ReferencedComponents(entity))
            {
                if (string.IsNullOrEmpty(sprite.GetProperty(sprite.TryGetProperty("clip", out _) ? "clip" : "texture").GetString())) throw new InvalidDataException("Saved assets require content paths.");
                if (sprite.TryGetProperty("assetId", out var id)) id.GetString();
            }
        }

        public bool HasSavedReference(AssetRecord asset)
        {
            foreach (var scene in SavedScenes())
            using (scene)
                foreach (var entity in scene.RootElement.GetProperty("entities").EnumerateArray())
                foreach (var sprite in ReferencedComponents(entity))
                {
                    if (sprite.TryGetProperty("assetId", out var id) && Guid.TryParse(id.GetString(), out var guid) && guid == asset.Id) return true;
                    if (IsLegacyReference(sprite, asset)) return true;
                }
            return false;
        }

        public bool HasSavedLegacyReference(AssetRecord asset)
        {
            foreach (var scene in SavedScenes())
            using (scene)
                foreach (var entity in scene.RootElement.GetProperty("entities").EnumerateArray())
                foreach (var sprite in ReferencedComponents(entity))
                {
                    if (sprite.ValueKind != JsonValueKind.Null && IsLegacyReference(sprite, asset)) return true;
                }
            return false;
        }

        private static bool IsLegacyReference(JsonElement sprite, AssetRecord asset)
        {
            return (!sprite.TryGetProperty("assetId", out var id) || string.IsNullOrEmpty(id.GetString())) &&
                sprite.GetProperty(sprite.TryGetProperty("clip", out _) ? "clip" : "texture").GetString()!.Replace('\\', '/').Equals(asset.Path, StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<JsonElement> ReferencedComponents(JsonElement entity)
        {
            var sprite = entity.GetProperty("sprite");
            if (sprite.ValueKind != JsonValueKind.Null) yield return sprite;
            if (entity.TryGetProperty("audio", out var audio) && audio.ValueKind != JsonValueKind.Null) yield return audio;
        }
    }
}
