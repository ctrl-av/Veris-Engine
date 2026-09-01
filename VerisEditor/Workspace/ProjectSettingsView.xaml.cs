using System.Windows;
using System.Windows.Controls;

namespace VerisEditor.Workspace
{
    public partial class ProjectSettingsView : UserControl
    {
        public ProjectSettingsView(EditorWorkspace workspace) { InitializeComponent(); DataContext = workspace; }
        private void SaveSettings(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).SaveProjectSettings();
        private void SaveInput(object sender, RoutedEventArgs e) => ((EditorWorkspace)DataContext).SaveInputBindings();
    }
}
