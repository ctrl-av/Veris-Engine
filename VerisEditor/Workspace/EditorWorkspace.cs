using System.Collections.ObjectModel;
using System.IO;
using VerisEditor.GameProject;
using VerisEditor.Interop;

namespace VerisEditor.Workspace
{
    public sealed class EditorWorkspace : ViewModelBase, IDisposable
    {
        private readonly NativeEngine _engine;
        private string _currentDirectory;
        private string _status = "";
        private string _output = "";

        public ProjectDocument Project { get; }
        public string ProjectFile { get; }
        public string ContentRoot { get; }
        public ObservableCollection<FileSystemInfo> ContentEntries { get; } = new();
        public ObservableCollection<WorkspaceEntity> Entities { get; } = new();
        public string CurrentDirectory => _currentDirectory;
        public string SceneSummary => $"{Entities.Count} entities in the active scene";
        public string WindowSummary => $"{Project.WindowWidth} × {Project.WindowHeight} · {Project.WindowMode}";
        public string Output { get => _output; private set { _output = value; OnPropertyChanged(nameof(Output)); } }
        public string Status { get => _status; private set { _status = value; OnPropertyChanged(nameof(Status)); } }

        public EditorWorkspace(ProjectDocument project, string projectFile, string historyWarning = "")
        {
            Project = project;
            ProjectFile = Path.GetFullPath(projectFile);
            ContentRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(ProjectFile)!, project.ContentDirectory));
            _currentDirectory = ContentRoot;
            ReadContentDirectory(ContentRoot);
            _engine = new NativeEngine((uint)project.WindowWidth, (uint)project.WindowHeight);
            try
            {
                RefreshScene();
                EditorLog.Info($"Workspace opened: {ProjectFile}");
                RefreshOutput();
                Status = historyWarning.Length == 0 ? "Project opened" : historyWarning;
            }
            catch { _engine.Dispose(); throw; }
        }

        public void NavigateTo(string directory) => Run(() => ReadContentDirectory(directory));

        public void NavigateUp()
        {
            if (!CurrentDirectory.Equals(ContentRoot, StringComparison.OrdinalIgnoreCase))
                NavigateTo(Directory.GetParent(CurrentDirectory)!.FullName);
        }

        public void Refresh() => Run(() => { ReadContentDirectory(CurrentDirectory); RefreshScene(); RefreshOutput(); });
        public void Dispose() => _engine.Dispose();

        private void Run(Action operation)
        {
            try { operation(); Status = "Workspace refreshed"; }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NativeEngineException or ArgumentException)
            {
                Status = exception.Message;
                EditorLog.Error("Cannot refresh editor workspace.", exception);
            }
        }

        private void ReadContentDirectory(string directory)
        {
            var fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
            if (!fullPath.Equals(ContentRoot, StringComparison.OrdinalIgnoreCase) &&
                !fullPath.StartsWith(ContentRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Content browsing must stay inside the project's content directory.");
            for (var ancestor = new DirectoryInfo(fullPath); ancestor != null; ancestor = ancestor.Parent)
            {
                if ((ancestor.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Content browsing does not follow linked directories.");
                if (ancestor.FullName.Equals(ContentRoot, StringComparison.OrdinalIgnoreCase)) break;
            }
            var entries = new DirectoryInfo(fullPath).GetFileSystemInfos();
            Array.Sort(entries, CompareContentEntries);
            ContentEntries.Clear();
            foreach (var entry in entries) ContentEntries.Add(entry);
            _currentDirectory = fullPath;
            OnPropertyChanged(nameof(CurrentDirectory));
        }

        private static int CompareContentEntries(FileSystemInfo left, FileSystemInfo right)
        {
            if (left is DirectoryInfo && right is not DirectoryInfo) return -1;
            if (left is not DirectoryInfo && right is DirectoryInfo) return 1;
            return StringComparer.OrdinalIgnoreCase.Compare(left.Name, right.Name);
        }

        private void RefreshScene()
        {
            var entities = new List<WorkspaceEntity>();
            foreach (var entity in _engine.EntityIds()) entities.Add(new WorkspaceEntity(entity, _engine.EntityName(entity)));
            Entities.Clear();
            foreach (var entity in entities) Entities.Add(entity);
            OnPropertyChanged(nameof(SceneSummary));
        }

        private void RefreshOutput()
        {
            if (EditorLog.FilePath == null || !File.Exists(EditorLog.FilePath)) { Output = "No log entries in this session."; return; }
            var lines = new Queue<string>();
            foreach (var line in File.ReadLines(EditorLog.FilePath))
            {
                lines.Enqueue(line);
                if (lines.Count > 200) lines.Dequeue();
            }
            Output = string.Join(Environment.NewLine, lines);
        }
    }

    public sealed record WorkspaceEntity(ulong Id, string Name);
}
