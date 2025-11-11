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
        // get path from installation location
        private readonly string _templatePath = @"..\..\VerisEditor\ProjectTemplates\";
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
                var templatesFiles = Directory.GetFiles(_templatePath, "template.xml", SearchOption.AllDirectories);
                Debug.Assert(templatesFiles.Any());
                foreach (var file in templatesFiles)
                {
                    var template = new ProjectTemplate()
                    {
                        ProjectType = "Empty Project",
                        ProjectFile = "project.veris",
                        Folders = new List<string>() { ".Veris", "Content", "GameCode" }

                    };
                }
            } catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
                // log errors
            }
            
        }
    }

    
}
