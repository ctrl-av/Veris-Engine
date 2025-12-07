using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VerisEditor.GameProject
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed class ProjectDocument
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        public required int FormatVersion { get; init; }
        public required Guid ProjectId { get; init; }
        public required string Name { get; init; }
        public required string TemplateId { get; init; }
        public required string ContentDirectory { get; init; }
        public required string GameCodeDirectory { get; init; }
        public string? StartupScene { get; init; }
        public int WindowWidth { get; init; } = 1280;
        public int WindowHeight { get; init; } = 720;

        public static ProjectDocument Parse(string projectJson)
        {
            try
            {
                var project = JsonSerializer.Deserialize<ProjectDocument>(projectJson, JsonOptions)
                    ?? throw new InvalidDataException("Project document cannot be null.");
                project.Validate();
                return project;
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException("The .veris file is not a valid version 1 project document.", exception);
            }
        }

        public string Serialize()
        {
            Validate();
            return JsonSerializer.Serialize(this, JsonOptions);
        }

        private void Validate()
        {
            if (FormatVersion != 1) throw new InvalidDataException($"Unsupported project format version: {FormatVersion}.");
            if (ProjectId == Guid.Empty) throw new InvalidDataException("Project identity must be a nonempty GUID.");
            if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(TemplateId))
                throw new InvalidDataException("Project name and template identity are required.");
            ProjectPaths.ValidateRelative(ContentDirectory);
            ProjectPaths.ValidateRelative(GameCodeDirectory);
            if (ContentDirectory.Equals(GameCodeDirectory, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Content and game code directories must be distinct.");
            if (StartupScene != null) ProjectPaths.ValidateRelative(StartupScene);
            if (WindowWidth < 1 || WindowHeight < 1)
                throw new InvalidDataException("Window dimensions must be positive.");
        }
    }

    internal static class ProjectPaths
    {
        public static void ValidateRelative(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
                throw new InvalidDataException("Project paths must be nonempty and relative.");
            foreach (var segment in relativePath.Replace('\\', '/').Split('/'))
            {
                if (segment is "" or "." or ".." || segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                    segment.EndsWith(' ') || segment.EndsWith('.'))
                    throw new InvalidDataException($"Invalid project-relative path: {relativePath}.");
            }
        }

        public static string Resolve(string rootDirectory, string relativePath)
        {
            ValidateRelative(relativePath);
            return Path.GetFullPath(Path.Combine(rootDirectory, relativePath));
        }
    }
}
