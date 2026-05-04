using System.IO;

namespace VerisEditor.Workspace
{
    public sealed partial class EditorWorkspace
    {
        private string? _sceneFile;
        public string? SceneFile => _sceneFile;
        public string ScenePathInput { get; set; } = "scene.verisscene";

        public bool SaveScene(string relativePath)
        {
            try
            {
                Tools.Cancel();
                var path = ScenePath(relativePath);
                AtomicFile.WriteText(path, _engine.SerializeScene());
                _sceneFile = path;
                ScenePathInput = relativePath;
                OnPropertyChanged(nameof(SceneFile));
                OnPropertyChanged(nameof(ScenePathInput));
                Status = "Scene saved";
                return true;
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or Interop.NativeEngineException)
            {
                Status = exception.Message;
                EditorLog.Error("Cannot save scene.", exception);
                return false;
            }
        }

        public bool OpenScene(string relativePath)
        {
            try
            {
                Tools.Cancel();
                var path = ScenePath(relativePath);
                if (new FileInfo(path).Length > 16 * 1024 * 1024) throw new InvalidDataException("Scene documents cannot exceed 16 MiB.");
                _engine.DeserializeScene(File.ReadAllText(path));
                History.Clear();
                RestoreSelection(0);
                _sceneFile = path;
                ScenePathInput = relativePath;
                OnPropertyChanged(nameof(SceneFile));
                OnPropertyChanged(nameof(ScenePathInput));
                Status = "Scene opened";
                return true;
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or Interop.NativeEngineException)
            {
                Status = exception.Message;
                EditorLog.Error("Cannot open scene.", exception);
                return false;
            }
        }

        private string ScenePath(string relativePath)
        {
            if (!Path.GetExtension(relativePath).Equals(".verisscene", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Choose a .verisscene file inside the content directory.");
            return ContentPaths.Resolve(ContentRoot, relativePath);
        }
    }
}
