using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace VerisEditor.GameProject
{
    /// <summary>
    /// Interaction logic for ProjectBrowseDialog.xaml
    /// </summary>
    public partial class ProjectBrowserDialog : Window
    {
        private readonly RecentProjects _recentProjects = new();
        public string HistoryWarning { get; private set; } = string.Empty;
        public string? SelectedProjectFile { get; private set; }
        public ProjectDocument? SelectedProject { get; private set; }
        public ProjectBrowserDialog()
        {
            InitializeComponent();
        }
        public void SelectProject(string projectFile, ProjectDocument project)
        {
            SelectedProjectFile = projectFile;
            SelectedProject = project;
            try
            {
                _recentProjects.Record(projectFile, project);
            }
            catch (Exception exception)
            {
                EditorLog.Error("Project opened, but recent project history could not be saved.", exception);
                HistoryWarning = $"Project opened. Recent projects could not be saved: {exception.Message}";
            }
            DialogResult = true;
        }
        private void OnToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender == openProjectButton)
            {
                if(createProjectButton.IsChecked == true)
                {
                    createProjectButton.IsChecked = false;
                    browser.Margin = new Thickness(0);

                    
                }
                openProjectButton.IsChecked = true;
            }
            else
            {
                if (openProjectButton.IsChecked == true)
                {
                    openProjectButton.IsChecked = false;
                    browser.Margin = new Thickness(-800,0,0,0);
                }
                createProjectButton.IsChecked = true;
            }
        }
    }
}
