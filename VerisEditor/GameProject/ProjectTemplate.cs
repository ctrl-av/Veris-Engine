using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace VerisEditor.GameProject
{
    public sealed class ProjectTemplate
    {
        public required string TemplateId { get; init; }
        public required string ProjectType { get; init; }
        public required string Description { get; init; }
        public required string ProjectFile { get; init; }
        public required string IconPath { get; init; }
        public required string ScreenshotPath { get; init; }
        public required IReadOnlyList<string> Folders { get; init; }

        public static ProjectTemplate Load(string descriptorPath)
        {
            using var reader = XmlReader.Create(descriptorPath, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            var root = XDocument.Load(reader).Root;
            if (root?.Name != "Template" || (string?)root.Attribute("Version") != "1")
                throw new InvalidDataException("Template must have a Template root with Version=1.");
            var templateDirectory = Path.GetDirectoryName(Path.GetFullPath(descriptorPath))!;
            return new ProjectTemplate
            {
                TemplateId = ReadIdentity(root), ProjectType = ReadRequired(root, "Name"),
                Description = ReadRequired(root, "Description"), ProjectFile = ReadProjectFile(root),
                IconPath = ReadAsset(root, "Icon", templateDirectory),
                ScreenshotPath = ReadAsset(root, "Screenshot", templateDirectory), Folders = ReadFolders(root)
            };
        }

        public static IReadOnlyList<ProjectTemplate> LoadAvailable(IEnumerable<string> descriptorPaths, Action<string, Exception> reportFailure)
        {
            var templates = new List<ProjectTemplate>();
            var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var descriptorPath in descriptorPaths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    var template = Load(descriptorPath);
                    if (!identities.Add(template.TemplateId)) throw new InvalidDataException($"Duplicate template identity: {template.TemplateId}.");
                    templates.Add(template);
                }
                catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or XmlException or ArgumentException)
                {
                    reportFailure(descriptorPath, exception);
                }
            }
            return templates.AsReadOnly();
        }

        private static string ReadIdentity(XElement root)
        {
            var identity = ReadRequired(root, "Id");
            if (identity.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
                throw new InvalidDataException("Template identity must contain ASCII letters, digits, or hyphens.");
            return identity;
        }

        private static string ReadProjectFile(XElement root)
        {
            var projectFile = ReadRequired(root, "ProjectFile");
            ProjectPaths.ValidateRelative(projectFile);
            if (projectFile.Contains('/') || projectFile.Contains('\\') || !projectFile.EndsWith(".veris", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Template project file must be a .veris filename without directories.");
            return projectFile;
        }

        private static IReadOnlyList<string> ReadFolders(XElement root)
        {
            var folders = new List<string>();
            var folderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var folder in root.Element("Folders")?.Elements("Folder") ?? Enumerable.Empty<XElement>())
            {
                var folderPath = folder.Value.Trim();
                ProjectPaths.ValidateRelative(folderPath);
                if (!folderNames.Add(folderPath.Replace('\\', '/'))) throw new InvalidDataException($"Duplicate template folder: {folderPath}.");
                folders.Add(folderPath);
            }
            if (!folderNames.Contains("Content") || !folderNames.Contains("GameCode"))
                throw new InvalidDataException("Templates must define Content and GameCode folders.");
            return folders.AsReadOnly();
        }

        private static string ReadAsset(XElement root, string elementName, string templateDirectory)
        {
            var assetPath = ProjectPaths.Resolve(templateDirectory, ReadRequired(root, elementName));
            if (!File.Exists(assetPath)) throw new InvalidDataException($"Template {elementName} asset does not exist.");
            for (var directory = Path.GetDirectoryName(assetPath); directory != templateDirectory; directory = Path.GetDirectoryName(directory))
            {
                if ((File.GetAttributes(directory!) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Template assets cannot use linked directories.");
            }
            if ((File.GetAttributes(assetPath) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Template assets cannot use linked files.");
            return assetPath;
        }

        private static string ReadRequired(XElement root, string elementName)
        {
            var text = root.Element(elementName)?.Value.Trim();
            if (string.IsNullOrWhiteSpace(text)) throw new InvalidDataException($"Template requires {elementName}.");
            return text;
        }
    }
}
