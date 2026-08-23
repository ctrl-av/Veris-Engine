using System;
using System.IO;

namespace VerisEditor.GameProject
{
    public static class ProjectFiles
    {
        private const string DefaultInputActions = """
            {
              "formatVersion": 1,
              "actions": {
                "Quit": ["Key:Escape"],
                "MoveUp": ["Key:W", "Key:Up"],
                "MoveDown": ["Key:S", "Key:Down"],
                "MoveLeft": ["Key:A", "Key:Left"],
                "MoveRight": ["Key:D", "Key:Right"],
                "Fire": ["Mouse:Left"]
              }
            }
            """;
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

        private static void CopyPayload(string source, string destination)
        {
            var pending = new System.Collections.Generic.Stack<string>();
            pending.Push(Path.GetFullPath(source));
            long bytes = 0; var files = 0;
            while (pending.Count != 0)
            {
                var directory = pending.Pop();
                if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Template payload cannot contain links.");
                foreach (var entry in new DirectoryInfo(directory).GetFileSystemInfos())
                {
                    if ((entry.Attributes & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Template payload cannot contain links.");
                    var relative = Path.GetRelativePath(source, entry.FullName);
                    ProjectPaths.ValidateRelative(relative);
                    if (relative.Split(Path.DirectorySeparatorChar)[0] is not ("Content" or "GameCode" or ".Veris")) throw new InvalidDataException("Payload files must stay in Content, GameCode or .Veris.");
                    var target = Path.Combine(destination, relative);
                    if (entry is DirectoryInfo) { Directory.CreateDirectory(target); pending.Push(entry.FullName); continue; }
                    bytes += ((FileInfo)entry).Length;
                    if (++files > 10000 || bytes > 256 * 1024 * 1024) throw new InvalidDataException("Template payload exceeds 10000 files or 256 MiB.");
                    File.Copy(entry.FullName, target, true);
                }
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
                TemplateId = template.TemplateId, ContentDirectory = "Content", GameCodeDirectory = "GameCode", StartupScene = template.StartupScene
            };
            File.WriteAllText(Path.Combine(stagingDirectory, template.ProjectFile), project.Serialize());
            var metadataDirectory = Path.Combine(stagingDirectory, ".Veris");
            Directory.CreateDirectory(metadataDirectory);
            File.WriteAllText(Path.Combine(metadataDirectory, "input-actions.json"), DefaultInputActions);
            if (template.PayloadDirectory != null) CopyPayload(template.PayloadDirectory, stagingDirectory);
            if (template.StartupScene != null && !File.Exists(Path.Combine(stagingDirectory, "Content", template.StartupScene))) throw new InvalidDataException("Template starting scene is missing from its payload.");
        }
    }
}
