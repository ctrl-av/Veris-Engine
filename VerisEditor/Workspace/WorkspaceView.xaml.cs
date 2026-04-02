using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VerisEditor.Workspace
{
    public partial class WorkspaceView : UserControl
    {
        public WorkspaceView(EditorWorkspace workspace)
        {
            InitializeComponent();
            DataContext = workspace;
        }

        private void RefreshWorkspace(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).Refresh();
        private void NavigateUp(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).NavigateUp();
        private void CreateEntity(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).CreateEntity(((EditorWorkspace)DataContext).EntityName);
        private void RenameEntity(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).RenameSelected(((EditorWorkspace)DataContext).EntityName);
        private void DuplicateEntity(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).DuplicateSelected();
        private void DeleteEntity(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).DeleteSelected();
        private void ReparentEntity(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).ReparentSelected(((EditorWorkspace)DataContext).SelectedParentId);

        private void HierarchySelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is WorkspaceEntity entity) ((EditorWorkspace)DataContext).SelectEntity(entity.Id);
        }

        private void OpenContentDirectory(object sender, MouseButtonEventArgs e)
        {
            if (ContentFiles.SelectedItem is DirectoryInfo directory)
                ((EditorWorkspace)DataContext).NavigateTo(directory.FullName);
        }
    }
}
