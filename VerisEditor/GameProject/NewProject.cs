using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VerisEditor.GameProject
{
    class NewProject : ViewModelBase
    {
        private readonly string _templatePath = System.IO.Path.Combine(AppContext.BaseDirectory, "ProjectTemplates");
        public IReadOnlyList<string> TemplateFiles { get; private set; } = Array.Empty<string>();
        public string ErrorMessage { get; private set; } = string.Empty;
        public IReadOnlyList<ProjectTemplate> Templates { get; private set; } = Array.Empty<ProjectTemplate>();
        private string _name = "NewProject";
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
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
                    OnPropertyChanged(nameof(Path));
                }
            }
        }

        public NewProject()
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
            } catch(Exception ex)
            {
                EditorLog.Error("Cannot discover project templates.", ex);
                ErrorMessage = $"Cannot load project templates: {ex.Message}";
            }
            
        }
    }

    
}
