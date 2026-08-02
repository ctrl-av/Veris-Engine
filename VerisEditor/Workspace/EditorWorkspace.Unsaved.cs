using System.IO;

namespace VerisEditor.Workspace
{
    public enum UnsavedChoice { Save, Discard, Cancel }

    public sealed partial class EditorWorkspace
    {
        private long _savedRevision;
        public bool IsDirty => History.Revision != _savedRevision;
        public string SceneTitle => (SceneFile == null ? "Untitled" : Path.GetFileName(SceneFile)) + (IsDirty ? " *" : "");

        public bool CanLeaveScene(Func<UnsavedChoice> choose)
        {
            if (IsPlayingSession) StopPlay();
            Tools.Cancel();
            if (!IsDirty) return true;
            return choose() switch
            {
                UnsavedChoice.Save => SaveScene(SceneFile == null ? ScenePathInput : Path.GetRelativePath(ContentRoot, SceneFile)),
                UnsavedChoice.Discard => true,
                _ => false
            };
        }

        public bool NewScene(Func<UnsavedChoice> choose)
        {
            if (!CanLeaveScene(choose)) return false;
            _engine.DeserializeScene("{\"formatVersion\":1,\"entities\":[]}");
            History.Clear();
            RestoreSelection(0);
            _sceneFile = null;
            ScenePathInput = "scene.verisscene";
            MarkSaved();
            OnPropertyChanged(nameof(SceneFile));
            OnPropertyChanged(nameof(ScenePathInput));
            return true;
        }

        private void MarkSaved()
        {
            _savedRevision = History.Revision;
            NotifySceneState();
        }

        private void OnHistoryChanged(object? sender, EventArgs e) => NotifySceneState();

        private void NotifySceneState()
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            OnPropertyChanged(nameof(IsDirty));
            OnPropertyChanged(nameof(SceneTitle));
        }
    }
}
