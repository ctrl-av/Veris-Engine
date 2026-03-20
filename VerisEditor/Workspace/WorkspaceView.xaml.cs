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

        private void OpenContentDirectory(object sender, MouseButtonEventArgs e)
        {
            if (ContentFiles.SelectedItem is DirectoryInfo directory)
                ((EditorWorkspace)DataContext).NavigateTo(directory.FullName);
        }
    }
}
