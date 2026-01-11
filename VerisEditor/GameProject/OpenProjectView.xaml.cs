using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace VerisEditor.GameProject
{
    public partial class OpenProjectView : UserControl
    {
        public OpenProjectView()
        {
            InitializeComponent();
        }

        private void OnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var openProject = (OpenProject)DataContext;
            try
            {
                var fileDialog = new OpenFileDialog { Title = "Open a Veris project", Filter = "Veris projects (*.veris)|*.veris", CheckFileExists = true };
                if (fileDialog.ShowDialog(Window.GetWindow(this)) == true) openProject.ProjectFile = fileDialog.FileName;
            }
            catch (Exception exception)
            {
                EditorLog.Error("Cannot browse project files.", exception);
                openProject.ErrorMessage = $"Cannot browse projects: {exception.Message}";
            }
        }

        private void OnOpen_Click(object sender, RoutedEventArgs e)
        {
            var openProject = (OpenProject)DataContext;
            if (openProject.Open() && Window.GetWindow(this) is ProjectBrowserDialog browser)
                browser.SelectProject(openProject.ProjectFile, openProject.OpenedProject!);
        }

        private void OnRemoveRecent_Click(object sender, RoutedEventArgs e)
        {
            ((OpenProject)DataContext).RemoveSelectedRecentProject();
        }
    }
}
