using System.IO;

namespace VerisEditor.Assets
{
    public sealed partial class AssetCatalog
    {
        public IReadOnlyDictionary<Guid, string> Reconcile(Action<AssetRecord> reload)
        {
            var failures = new Dictionary<Guid, string>();
            var records = new List<AssetRecord>(_assets);
            for (var index = 0; index < records.Count; ++index)
            {
                var asset = records[index];
                try
                {
                    var path = ContentPaths.Resolve(_contentRoot, asset.Path);
                    if (!File.Exists(path)) throw new IOException("Missing file; restore it or reimport this asset using its existing GUID.");
                    var digest = Hash(path);
                    if (digest == asset.Sha256) continue;
                    ValidateTexture(path);
                    reload(asset);
                    records[index] = asset with { Sha256 = digest };
                }
                catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or Interop.NativeEngineException)
                {
                    failures.Add(asset.Id, $"{asset.Path}: {exception.Message}");
                }
            }
            if (!records.SequenceEqual(_assets)) Save(records);
            return failures;
        }
    }
}
