using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;

namespace VerisEditor.GameProject
{
    class NewProject : ViewModelBase
    {
        private static readonly string _templatePath = System.IO.Path.Combine(AppContext.BaseDirectory, "ProjectTemplates");
        public IReadOnlyList<string> TemplateFiles { get; private set; } = Array.Empty<string>();
        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            internal set
            {
                _errorMessage = value;
                OnPropertyChanged(nameof(ErrorMessage));
            }
        }
        public IReadOnlyList<ProjectTemplate> Templates { get; private set; } = Array.Empty<ProjectTemplate>();
        private string? _validatedProjectDirectory;
        public bool IsProjectLocationValid => _validatedProjectDirectory != null;
        private string _validationMessage = string.Empty;
        public string ValidationMessage
        {
            get => _validationMessage;
            private set
            {
                _validationMessage = value;
                OnPropertyChanged(nameof(ValidationMessage));
            }
        }
        private ProjectTemplate? _selectedTemplate;
        public ProjectTemplate? SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                if (_selectedTemplate == value) return;
                _selectedTemplate = value;
                InvalidateValidation();
                OnPropertyChanged(nameof(SelectedTemplate));
            }
        }
        private string _name = "NewProject";
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    InvalidateValidation();
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        private string _path = $@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\VerisProject\";
        public string Path
        {
            get => _path;
            set
            {
                if (_path != value)
                {
                    _path = value;
                    InvalidateValidation();
                    OnPropertyChanged(nameof(Path));
                }
            }
        }

        public NewProject()
        {
            LoadTemplates();
        }

        public bool ValidateCreation()
        {
            InvalidateValidation();
            try
            {
                _validatedProjectDirectory = ProjectCreationValidation.Validate(Name, Path, SelectedTemplate);
                ValidationMessage = $"Location available: {_validatedProjectDirectory}";
                OnPropertyChanged(nameof(IsProjectLocationValid));
                return true;
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or SecurityException)
            {
                EditorLog.Error("Project location validation failed.", exception);
                ValidationMessage = exception.Message;
                return false;
            }
        }

        private void InvalidateValidation()
        {
            _validatedProjectDirectory = null;
            ValidationMessage = string.Empty;
            OnPropertyChanged(nameof(IsProjectLocationValid));
        }

        private void LoadTemplates()
        {
            try
            {
                TemplateFiles = Directory.GetFiles(_templatePath, "template.xml", SearchOption.AllDirectories);
                Templates = ProjectTemplate.LoadAvailable(TemplateFiles, (descriptorPath, exception) =>
                {
                    EditorLog.Error($"Cannot load template {descriptorPath}.", exception);
                    ErrorMessage += $"Cannot load template {System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(descriptorPath))}: {exception.Message}\n";
                });
                if (TemplateFiles.Count == 0) ErrorMessage = "No project templates were found in the installation.";
                SelectedTemplate = Templates.FirstOrDefault();
            }
            catch (Exception exception)
            {
                EditorLog.Error("Cannot discover project templates.", exception);
                ErrorMessage = $"Cannot load project templates: {exception.Message}";
            }
        }
    }
}
