using System;
using System.IO;

namespace VerisEditor.GameProject
{
    public static class ProjectFiles
    {
        public static ProjectDocument Open(string projectFile)
        {
            var fullProjectFile = Path.GetFullPath(projectFile);
            if (!Path.GetExtension(fullProjectFile).Equals(".veris", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Choose a .veris project file.");
            var project = ProjectDocument.Parse(File.ReadAllText(fullProjectFile));
            var projectDirectory = Path.GetDirectoryName(fullProjectFile)!;
            ValidateDirectory(projectDirectory, project.ContentDirectory);
            ValidateDirectory(projectDirectory, project.GameCodeDirectory);
            return project;
        }

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

        private static void ValidateDirectory(string projectDirectory, string relativeDirectory)
        {
            var currentDirectory = projectDirectory;
            foreach (var segment in relativeDirectory.Replace('\\', '/').Split('/'))
            {
                currentDirectory = Path.Combine(currentDirectory, segment);
                var attributes = File.GetAttributes(currentDirectory);
                if ((attributes & FileAttributes.Directory) == 0 || (attributes & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException($"Project directory must be a real directory inside the project: {relativeDirectory}.");
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
