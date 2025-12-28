using System;
using System.IO;

namespace VerisEditor.GameProject
{
    public static class ProjectFiles
    {
        public static string Create(string projectName, string parentDirectory, ProjectTemplate template)
        {
            var projectDirectory = ProjectCreationValidation.Validate(projectName, parentDirectory, template);
            var parent = Path.GetDirectoryName(projectDirectory)!;
            Directory.CreateDirectory(parent);
            var stagingDirectory = Path.Combine(parent, $".veris-create-{Guid.NewGuid():N}");
            Directory.CreateDirectory(stagingDirectory);
            try
            {
                WriteProject(stagingDirectory, projectName, template);
                Directory.Move(stagingDirectory, projectDirectory);
                return Path.Combine(projectDirectory, template.ProjectFile);
            }
            finally
            {
                if (Directory.Exists(stagingDirectory)) Directory.Delete(stagingDirectory, true);
            }
        }

        private static void WriteProject(string stagingDirectory, string projectName, ProjectTemplate template)
        {
            foreach (var folder in template.Folders)
            {
                ProjectPaths.ValidateRelative(folder);
                Directory.CreateDirectory(Path.Combine(stagingDirectory, folder));
            }
            ProjectPaths.ValidateRelative(template.ProjectFile);
            var project = new ProjectDocument
            {
                FormatVersion = 1, ProjectId = Guid.NewGuid(), Name = projectName,
                TemplateId = template.TemplateId, ContentDirectory = "Content", GameCodeDirectory = "GameCode"
            };
            File.WriteAllText(Path.Combine(stagingDirectory, template.ProjectFile), project.Serialize());
        }
    }
}
