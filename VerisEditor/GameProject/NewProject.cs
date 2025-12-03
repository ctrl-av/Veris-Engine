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
    public class ProjectTemplate
    {
        public string ProjectType { get; set; }
        public string ProjectFile { get; set; }
        public List<string> Folders { get; set; }

    }
    class NewProject : ViewModelBase
    {
        private readonly string _templatePath = System.IO.Path.Combine(AppContext.BaseDirectory, "ProjectTemplates");
        public IReadOnlyList<string> TemplateFiles { get; private set; } = Array.Empty<string>();
        public string ErrorMessage { get; private set; } = string.Empty;
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
            } catch(Exception ex)
            {
                EditorLog.Error("Cannot discover project templates.", ex);
                ErrorMessage = $"Cannot load project templates: {ex.Message}";
            }
            
        }
    }

    
}
