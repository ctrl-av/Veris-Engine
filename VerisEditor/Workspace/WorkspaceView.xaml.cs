using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Controls.Primitives;

namespace VerisEditor.Workspace
{
    public partial class WorkspaceView : UserControl
    {
        public WorkspaceView(EditorWorkspace workspace)
        {
            InitializeComponent();
            DataContext = workspace;
            PreviewKeyDown += OnWorkspaceKeyDown;
        }

        private void OnWorkspaceKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control) return;
            if (e.Key == Key.S) ((EditorWorkspace)DataContext).SaveScene(((EditorWorkspace)DataContext).ScenePathInput);
            else if (e.OriginalSource is TextBoxBase) return;
            else if (e.Key == Key.Z) ((EditorWorkspace)DataContext).Undo();
            else if (e.Key == Key.Y) ((EditorWorkspace)DataContext).Redo();
            else return;
            e.Handled = true;
        }

        private void RefreshWorkspace(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).Refresh();
        private void ApplyAnimation(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).ApplyAnimation();
        private void PreviewAnimation(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).PreviewAnimation();
        private void ApplyAudio(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).ApplyAudio();
        private void PlayAudio(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).PlayAudio();
        private void PauseAudio(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).PauseAudio();
        private void ResumeAudio(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).ResumeAudio();
        private void StopAudio(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).StopAudio();
        private void NavigateUp(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).NavigateUp();
        private void ImportAsset(object sender, RoutedEventArgs e)
        {
            var picker = new Microsoft.Win32.OpenFileDialog { Title = "Import asset", Filter = "Images and audio|*.png;*.jpg;*.jpeg;*.bmp;*.tga;*.wav;*.ogg;*.flac|All files|*.*" };
            if (picker.ShowDialog(Window.GetWindow(this)) == true) ((EditorWorkspace)DataContext).ImportAsset(picker.FileName);
        }
        private void CreateFolder(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).CreateContentFolder();
        private void MoveAsset(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).MoveSelectedAsset();
        private void UseAsset(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).UseSelectedAsset();

        private void RegisterTexture(object sender, RoutedEventArgs e)
        {
            if (ContentFiles.SelectedItem is FileInfo file)
                ((EditorWorkspace)DataContext).RegisterContent(Path.GetRelativePath(((EditorWorkspace)DataContext).ContentRoot, file.FullName));
        }

        private void ReimportAsset(object sender, RoutedEventArgs e)
        {
            var workspace = (EditorWorkspace)DataContext;
            if (workspace.SelectedAsset == null) return;
            var picker = new Microsoft.Win32.OpenFileDialog { Title = "Reimport asset", Filter = "Images and audio|*.png;*.jpg;*.jpeg;*.bmp;*.tga;*.wav;*.ogg;*.flac|All files|*.*" };
            if (picker.ShowDialog(Window.GetWindow(this)) == true) workspace.ReimportAsset(workspace.SelectedAsset.Id, picker.FileName);
        }

        private void DeleteAsset(object sender, RoutedEventArgs e)
        {
            var workspace = (EditorWorkspace)DataContext;
            workspace.DeleteSelectedAsset(() => MessageBox.Show(Window.GetWindow(this), $"Delete {workspace.SelectedAsset!.Path} from this project?", "Delete asset", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes);
        }

        private void ImportDroppedFiles(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            foreach (var file in (string[])e.Data.GetData(DataFormats.FileDrop)) ((EditorWorkspace)DataContext).ImportAsset(file);
            e.Handled = true;
        }
        private void Undo(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).Undo();
        private void Redo(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).Redo();
        private void SaveScene(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).SaveScene(((EditorWorkspace)DataContext).ScenePathInput);
        private void OpenScene(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).OpenScene(((EditorWorkspace)DataContext).ScenePathInput, ChooseUnsaved);
        private void NewScene(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).NewScene(ChooseUnsaved);
        private UnsavedChoice ChooseUnsaved() => ScenePrompts.Choose(Window.GetWindow(this), ((EditorWorkspace)DataContext).SceneTitle);
        private void CreateEntity(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).CreateEntity(((EditorWorkspace)DataContext).EntityName);
        private void RenameEntity(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).RenameSelected(((EditorWorkspace)DataContext).EntityName);
        private void DuplicateEntity(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).DuplicateSelected();
        private void DeleteEntity(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).DeleteSelected();
        private void ReparentEntity(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).ReparentSelected(((EditorWorkspace)DataContext).SelectedParentId);
        private void ApplyTransform(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).ApplyTransform();
        private void ApplySprite(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).ApplySprite();
        private void RemoveSprite(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).RemoveSprite();

        private void UseSelectedTexture(object sender, RoutedEventArgs e)
        {
            if (ContentFiles.SelectedItem is FileInfo texture)
                TexturePathInput.Text = Path.GetRelativePath(((EditorWorkspace)DataContext).ContentRoot, texture.FullName);
        }

        private void HierarchySelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is WorkspaceEntity entity) ((EditorWorkspace)DataContext).SelectEntity(entity.Id);
        }

        private void OpenContentDirectory(object sender, MouseButtonEventArgs e)
        {
            if (ContentFiles.SelectedItem is DirectoryInfo directory)
                ((EditorWorkspace)DataContext).NavigateTo(directory.FullName);
            else if (ContentFiles.SelectedItem is FileInfo file && file.Extension.Equals(".verisscene", StringComparison.OrdinalIgnoreCase))
                ((EditorWorkspace)DataContext).OpenScene(Path.GetRelativePath(((EditorWorkspace)DataContext).ContentRoot, file.FullName), ChooseUnsaved);
        }
    }
}
