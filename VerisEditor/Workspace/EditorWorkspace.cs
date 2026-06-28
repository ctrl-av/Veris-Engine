using System.Collections.ObjectModel;
using System.IO;
using VerisEditor.GameProject;
using VerisEditor.Interop;
using VerisEditor.Assets;

namespace VerisEditor.Workspace
{
    public sealed partial class EditorWorkspace : ViewModelBase, IDisposable
    {
        private readonly NativeEngine _engine;
        private string _currentDirectory;
        private string _status = "";
        private string _output = "";
        private bool _refreshingHierarchy;
        private WorkspaceEntity? _selectedEntity;

        public ProjectDocument Project { get; }
        public string ProjectFile { get; }
        public string ContentRoot { get; }
        public ObservableCollection<FileSystemInfo> ContentEntries { get; } = new();
        public ObservableCollection<WorkspaceEntity> Entities { get; } = new();
        public ObservableCollection<WorkspaceEntity> RootEntities { get; } = new();
        public ObservableCollection<WorkspaceEntity> ParentCandidates { get; } = new();
        public WorkspaceEntity? SelectedEntity => _selectedEntity;
        public ulong SelectedEntityId => _selectedEntity?.Id ?? 0;
        public bool HasSelection => _selectedEntity != null;
        public EntityInspector Inspector { get; }
        public TransformTools Tools { get; }
        public SceneHistory History { get; }
        public AssetCatalog Catalog { get; }
        public string EntityName { get; set; } = "Entity";
        public ulong SelectedParentId { get; set; }
        public bool IsDisposed { get; private set; }
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
            Catalog = new AssetCatalog(ContentRoot, Path.GetDirectoryName(ProjectFile)!, NativeEngine.ValidateTextureFile);
            _engine = new NativeEngine((uint)project.WindowWidth, (uint)project.WindowHeight);
            Inspector = new EntityInspector(_engine);
            History = new SceneHistory(_engine, () => SelectedEntityId, RestoreSelection);
            History.Changed += OnHistoryChanged;
            Tools = new TransformTools(_engine, Inspector, History);
            try
            {
                _engine.SetContentRoot(ContentRoot);
                _engine.LoadAssetCatalog(Catalog.ManifestPath);
                RefreshAssetList();
                RefreshScene();
                EditorLog.Info($"Workspace opened: {ProjectFile}");
                RefreshOutput();
                Status = historyWarning.Length == 0 ? "Project opened" : historyWarning;
                WatchAssets();
            }
            catch { _engine.Dispose(); throw; }
        }

        public void NavigateTo(string directory) => Run(() => ReadContentDirectory(directory));

        public void NavigateUp()
        {
            if (!CurrentDirectory.Equals(ContentRoot, StringComparison.OrdinalIgnoreCase))
                NavigateTo(Directory.GetParent(CurrentDirectory)!.FullName);
        }

        public void Refresh() => Run(() => { ReadContentDirectory(CurrentDirectory); RefreshScene(); RefreshOutput(); RefreshAssetList(); });
        public void CreateEntity(string name, ulong parent = 0) => Edit("Create entity", () =>
        {
            var entity = _engine.CreateEntity(name);
            try { _engine.SetParent(entity, parent); }
            catch { _engine.DestroyEntity(entity); throw; }
            RefreshScene();
            SelectEntity(entity);
        });

        public void RenameSelected(string name) => Edit("Rename entity", () => { _engine.RenameEntity(SelectedEntityId, name); RefreshScene(); });
        public void ReparentSelected(ulong parent) => Edit("Reparent entity", () => { _engine.SetParent(SelectedEntityId, parent); RefreshScene(); });
        public void DeleteSelected() => Edit("Delete subtree", () => { _engine.DestroyEntity(SelectedEntityId); RefreshScene(); });
        public void DuplicateSelected() => Edit("Duplicate subtree", () =>
        {
            var duplicate = _engine.DuplicateEntity(SelectedEntityId);
            RefreshScene();
            SelectEntity(duplicate);
        });

        public void SelectEntity(ulong entityId)
        {
            if (_refreshingHierarchy) return;
            WorkspaceEntity? selected = null;
            foreach (var entity in Entities) if (entity.Id == entityId) selected = entity;
            if (entityId != 0 && selected == null) throw new ArgumentOutOfRangeException(nameof(entityId), "Entity is not in the active scene.");
            if (ReferenceEquals(_selectedEntity, selected)) return;
            Tools.Cancel();
            _selectedEntity = selected;
            Inspector.Load(entityId);
            AnimationJson = entityId == 0 ? "null" : _engine.Animation(entityId);
            OnPropertyChanged(nameof(AnimationJson));
            AudioJson = entityId == 0 ? "null" : _engine.Audio(entityId);
            OnPropertyChanged(nameof(AudioJson));
            foreach (var entity in Entities) entity.IsSelected = entity.Id == entityId;
            EntityName = selected?.Name ?? "Entity";
            SelectedParentId = selected?.ParentId ?? 0;
            OnPropertyChanged(nameof(SelectedEntity));
            OnPropertyChanged(nameof(SelectedEntityId));
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(EntityName));
            OnPropertyChanged(nameof(SelectedParentId));
        }
        public void DrawViewport(uint width, uint height, byte[] rgba)
        {
            _engine.Resize(width, height);
            _engine.Update();
            _engine.CopyFrame(rgba);
        }

        public EntityPoint[] EntityCorners(ulong entity) => _engine.EntityCorners(entity);
        public EntityPoint EntityOrigin(ulong entity) => _engine.EntityOrigin(entity);
        public void SelectAt(int pixelX, int pixelY) => Run(() => SelectEntity(_engine.PickEntity(pixelX, pixelY)));
        public void ApplyTransform() => Edit("Edit transform", Inspector.ApplyTransform);
        public void ApplySprite() => Edit("Edit sprite", Inspector.ApplySprite);
        public void RemoveSprite() => Edit("Remove sprite", Inspector.RemoveSprite);
        public void BeginTransformDrag(int x, int y) => Run(() => Tools.Begin(SelectedEntityId, x, y));
        public void DragTransform(int x, int y) => Run(() => Tools.Drag(x, y));
        public void CancelTransformDrag() => Run(Tools.Cancel);
        public void CompleteTransformDrag() => Run(Tools.Complete);
        public void Undo() => Run(() => { Tools.Cancel(); History.Undo(); });
        public void Redo() => Run(() => { Tools.Cancel(); History.Redo(); });

        public void ReportViewportError(Exception exception)
        {
            Status = exception.Message;
            EditorLog.Error("Cannot render editor viewport.", exception);
        }

        public void Dispose()
        {
            IsDisposed = true;
            _assetTimer?.Stop();
            _assetWatcher?.Dispose();
            History.Dispose();
            _engine.Dispose();
        }

        private void Edit(string name, Action operation) => Run(() => { Tools.Cancel(); History.Execute(name, operation); });

        private void RestoreSelection(ulong entity)
        {
            RefreshScene();
            foreach (var candidate in Entities) if (candidate.Id == entity) { SelectEntity(entity); return; }
            SelectEntity(0);
        }

        private void Run(Action operation)
        {
            try { operation(); Status = "Workspace refreshed"; }
            catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or NativeEngineException or ArgumentException)
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
            var selectedId = SelectedEntityId;
            var entities = new Dictionary<ulong, WorkspaceEntity>();
            foreach (var id in _engine.EntityIds()) entities.Add(id, new WorkspaceEntity(id, _engine.EntityName(id), _engine.EntityParent(id)));
            _refreshingHierarchy = true;
            try { PublishHierarchy(entities); }
            finally { _refreshingHierarchy = false; }
            SelectEntity(entities.ContainsKey(selectedId) ? selectedId : 0);
            OnPropertyChanged(nameof(SceneSummary));
        }

        private void PublishHierarchy(Dictionary<ulong, WorkspaceEntity> entities)
        {
            Entities.Clear();
            RootEntities.Clear();
            ParentCandidates.Clear();
            ParentCandidates.Add(new WorkspaceEntity(0, "Scene root", 0));
            foreach (var entity in entities.Values)
            {
                Entities.Add(entity);
                ParentCandidates.Add(entity);
                if (entity.ParentId == 0) RootEntities.Add(entity);
                else entities[entity.ParentId].Children.Add(entity);
            }
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

    public sealed class WorkspaceEntity(ulong id, string name, ulong parentId) : ViewModelBase
    {
        private bool _isSelected;
        public ulong Id { get; } = id;
        public string Name { get; } = name;
        public ulong ParentId { get; } = parentId;
        public ObservableCollection<WorkspaceEntity> Children { get; } = new();
        public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); } }
    }
}
