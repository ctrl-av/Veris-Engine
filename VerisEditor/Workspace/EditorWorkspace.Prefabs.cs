using System.Globalization;
using System.IO;

namespace VerisEditor.Workspace
{
    public sealed partial class EditorWorkspace
    {
        public string PrefabPathInput { get; set; } = "entity.verisprefab";
        public string PrefabSource { get; private set; } = "";
        public string PrefabX { get; set; } = "0";
        public string PrefabY { get; set; } = "0";

        public void SavePrefab() => Run(() =>
        {
            RequireEditing(); Tools.Cancel();
            if (!Path.GetExtension(PrefabPathInput).Equals(".verisprefab", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Choose a .verisprefab content path.");
            AtomicFile.WriteText(ContentPaths.Resolve(ContentRoot, PrefabPathInput), _engine.SerializePrefab(SelectedEntityId));
            ReadContentDirectory(CurrentDirectory);
        });

        public void InstantiatePrefab() => Edit("Instantiate prefab", () =>
        {
            if (!float.TryParse(PrefabX, NumberStyles.Float, CultureInfo.InvariantCulture, out var x) || !float.IsFinite(x) ||
                !float.TryParse(PrefabY, NumberStyles.Float, CultureInfo.InvariantCulture, out var y) || !float.IsFinite(y))
                throw new ArgumentException("Prefab placement requires finite X and Y coordinates.");
            var root = _engine.InstantiatePrefab(PrefabPathInput, x, y);
            RefreshScene(); SelectEntity(root);
        });

        public void ReapplyPrefab() => Edit("Reapply prefab source", () =>
        {
            _engine.ReapplyPrefab(SelectedEntityId, PrefabPathInput);
            RefreshScene();
        });

        public void UnpackPrefab() => Edit("Unpack prefab", () =>
        {
            _engine.UnpackPrefab(SelectedEntityId);
            RefreshScene();
        });
    }
}
