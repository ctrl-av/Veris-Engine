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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace VerisEditor.GameProject
{
    /// <summary>
    /// Interaction logic for NewProject.xaml
    /// </summary>
    public partial class NewProjectView : UserControl
    {
        public NewProjectView()
        {
            InitializeComponent();

        }

        private void OnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var newProject = (NewProject)DataContext;
            try
            {
                var folderDialog = new OpenFolderDialog { Title = "Choose a parent folder for the project" };
                if (folderDialog.ShowDialog(Window.GetWindow(this)) == true) newProject.Path = folderDialog.FolderName;
            }
            catch (Exception exception)
            {
                EditorLog.Error("Cannot browse project folders.", exception);
                newProject.ErrorMessage = $"Cannot browse folders: {exception.Message}";
            }
        }

        private void OnTemplateImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            EditorLog.Error("Cannot display a template image.", e.ErrorException);
            if (DataContext is NewProject newProject) newProject.ErrorMessage = "A template image could not be displayed. Check the editor log for details.";
        }
    }
}
