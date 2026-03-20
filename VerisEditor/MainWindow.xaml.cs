using System.Windows;
using VerisEditor.GameProject;
using VerisEditor.Workspace;

namespace VerisEditor
{
    public partial class MainWindow : Window
    {
        private EditorWorkspace? _workspace;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnMainWindowLoaded;
        }

        protected override void OnClosed(EventArgs e)
        {
            _workspace?.Dispose();
            base.OnClosed(e);
        }

        private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnMainWindowLoaded;
            OpenProjectBrowserDialog();
        }

        private void OpenProject(object sender, RoutedEventArgs e) => OpenProjectBrowserDialog();
        private void ExitEditor(object sender, RoutedEventArgs e) => Close();

        private void OpenProjectBrowserDialog()
        {
            while (true)
            {
                var browser = new ProjectBrowserDialog { Owner = this };
                if (browser.ShowDialog() != true) { if (_workspace == null) Close(); return; }
                try
                {
                    LoadWorkspace(browser.SelectedProject!, browser.SelectedProjectFile!, browser.HistoryWarning);
                    return;
                }
                catch (Exception exception)
                {
                    EditorLog.Error("Cannot open editor workspace.", exception);
                    MessageBox.Show(this, exception.Message, "Cannot open workspace", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadWorkspace(ProjectDocument project, string projectFile, string historyWarning)
        {
            var workspace = new EditorWorkspace(project, projectFile, historyWarning);
            WorkspaceView view;
            try { view = new WorkspaceView(workspace); }
            catch { workspace.Dispose(); throw; }
            var previous = _workspace;
            WorkspaceContent.Content = view;
            _workspace = workspace;
            Title = $"Veris Editor - {project.Name}";
            previous?.Dispose();
        }
    }
}
