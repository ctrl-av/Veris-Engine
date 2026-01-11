using System;
using System.IO;
using System.Collections.Generic;

namespace VerisEditor.GameProject
{
    public class OpenProject : ViewModelBase
    {
        private readonly RecentProjects _recentProjects;
        public IReadOnlyList<RecentProject> RecentProjectEntries { get; private set; } = Array.Empty<RecentProject>();
        private RecentProject? _selectedRecentProject;
        public RecentProject? SelectedRecentProject
        {
            get => _selectedRecentProject;
            set
            {
                _selectedRecentProject = value;
                if (value != null) ProjectFile = value.ProjectFile;
                OnPropertyChanged(nameof(SelectedRecentProject));
            }
        }

        public OpenProject() : this(new RecentProjects()) { }

        public OpenProject(RecentProjects recentProjects)
        {
            _recentProjects = recentProjects;
            ReloadRecentProjects();
        }

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

        public void RemoveSelectedRecentProject()
        {
            if (SelectedRecentProject == null) return;
            try
            {
                _recentProjects.Remove(SelectedRecentProject.ProjectFile);
                SelectedRecentProject = null;
                ProjectFile = string.Empty;
                ReloadRecentProjects();
            }
            catch (Exception exception)
            {
                EditorLog.Error("Cannot remove recent project.", exception);
                ErrorMessage = $"Cannot update recent projects: {exception.Message}";
            }
        }

        private void ReloadRecentProjects()
        {
            try
            {
                RecentProjectEntries = _recentProjects.Load();
                OnPropertyChanged(nameof(RecentProjectEntries));
            }
            catch (Exception exception)
            {
                EditorLog.Error("Cannot load recent projects.", exception);
                ErrorMessage = $"Cannot load recent projects: {exception.Message}. You can still browse to a project.";
            }
        }
    }
}
