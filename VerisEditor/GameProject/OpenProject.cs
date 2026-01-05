using System;
using System.IO;

namespace VerisEditor.GameProject
{
    public class OpenProject : ViewModelBase
    {
        private string _projectFile = string.Empty;
        public string ProjectFile
        {
            get => _projectFile;
            set
            {
                _projectFile = value;
                OpenedProject = null;
                ErrorMessage = string.Empty;
                OnPropertyChanged(nameof(ProjectFile));
            }
        }

        private ProjectDocument? _openedProject;
        public ProjectDocument? OpenedProject
        {
            get => _openedProject;
            private set { _openedProject = value; OnPropertyChanged(nameof(OpenedProject)); }
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            internal set { _errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); }
        }

        public bool Open()
        {
            OpenedProject = null;
            ErrorMessage = string.Empty;
            try
            {
                ProjectFile = Path.GetFullPath(ProjectFile);
                OpenedProject = ProjectFiles.Open(ProjectFile);
                EditorLog.Info($"Opened project: {ProjectFile}");
                return true;
            }
            catch (Exception exception)
            {
                EditorLog.Error("Cannot open project.", exception);
                ErrorMessage = $"Cannot open project: {exception.Message}";
                return false;
            }
        }
    }
}
