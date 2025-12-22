using System;
using System.IO;

namespace VerisEditor.GameProject
{
    internal static class ProjectCreationValidation
    {
        public static string Validate(string projectName, string parentDirectory, ProjectTemplate? template)
        {
            if (template == null) throw new InvalidDataException("Select a project template.");
            ProjectPaths.ValidateRelative(projectName);
            if (projectName.Contains('/') || projectName.Contains('\\') || projectName != projectName.Trim())
                throw new InvalidDataException("Use a single project folder name without surrounding spaces.");
            if (string.IsNullOrWhiteSpace(parentDirectory) || !Path.IsPathFullyQualified(parentDirectory))
                throw new InvalidDataException("Choose an absolute parent folder path.");
            var fullParentDirectory = Path.GetFullPath(parentDirectory);
            var relativeParentDirectory = fullParentDirectory[Path.GetPathRoot(fullParentDirectory)!.Length..];
            if (relativeParentDirectory.Length > 0) ProjectPaths.ValidateRelative(relativeParentDirectory.TrimEnd('\\', '/'));
            var projectDirectory = Path.Combine(fullParentDirectory, projectName);
            RejectExistingDestination(projectDirectory);
            CheckWriteAccess(FindExistingAncestor(fullParentDirectory));
            return projectDirectory;
        }

        private static void RejectExistingDestination(string projectDirectory)
        {
            try { File.GetAttributes(projectDirectory); }
            catch (Exception exception) when (exception is DirectoryNotFoundException or FileNotFoundException)
            {
                return;
            }
            throw new InvalidDataException("A file or directory already exists at the project location.");
        }

        private static string FindExistingAncestor(string directory)
        {
            while (true)
            {
                try
                {
                    if ((File.GetAttributes(directory) & FileAttributes.Directory) == 0)
                        throw new InvalidDataException("The parent path contains a file instead of a directory.");
                    return directory;
                }
                catch (Exception exception) when (exception is DirectoryNotFoundException or FileNotFoundException)
                {
                    directory = Path.GetDirectoryName(directory) ?? throw new DirectoryNotFoundException("The parent drive or share is unavailable.");
                }
            }
        }

        private static void CheckWriteAccess(string existingDirectory)
        {
            var probePath = Path.Combine(existingDirectory, $".veris-write-check-{Guid.NewGuid():N}");
            using var probe = new FileStream(probePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
            probe.WriteByte(0);
            probe.Flush();
        }
    }
}
