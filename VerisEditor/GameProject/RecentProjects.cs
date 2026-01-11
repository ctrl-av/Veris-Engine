using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VerisEditor.GameProject
{
    public sealed record RecentProject
    {
        public required Guid ProjectId { get; init; }
        public required string ProjectFile { get; init; }
        public required string Name { get; init; }
        public required DateTimeOffset LastOpenedUtc { get; init; }
        [JsonIgnore]
        public bool IsAvailable => File.Exists(ProjectFile);
    }

    public sealed class RecentProjects
    {
        private static readonly object _writeLock = new();
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
        private readonly string _filePath;

        public RecentProjects(string? filePath = null)
        {
            _filePath = Path.GetFullPath(filePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Veris", "recent-projects.json"));
        }

        public IReadOnlyList<RecentProject> Load()
        {
            if (!File.Exists(_filePath)) return Array.Empty<RecentProject>();
            var entries = JsonSerializer.Deserialize<List<RecentProject>>(File.ReadAllText(_filePath), JsonOptions)
                ?? throw new InvalidDataException("Recent project history cannot be null.");
            foreach (var entry in entries)
            {
                if (entry == null || entry.ProjectId == Guid.Empty || string.IsNullOrWhiteSpace(entry.Name) ||
                    string.IsNullOrWhiteSpace(entry.ProjectFile) || !Path.IsPathFullyQualified(entry.ProjectFile) ||
                    !Path.GetExtension(entry.ProjectFile).Equals(".veris", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Recent project history contains an invalid entry.");
            }
            return entries.AsReadOnly();
        }

        public void Record(string projectFile, ProjectDocument project)
        {
            lock (_writeLock)
            {
                var fullProjectFile = Path.GetFullPath(projectFile);
                var entries = new List<RecentProject>(Load());
                entries.RemoveAll(entry => entry.ProjectId == project.ProjectId || entry.ProjectFile.Equals(fullProjectFile, StringComparison.OrdinalIgnoreCase));
                entries.Insert(0, new RecentProject { ProjectId = project.ProjectId, ProjectFile = fullProjectFile, Name = project.Name, LastOpenedUtc = DateTimeOffset.UtcNow });
                if (entries.Count > 20) entries.RemoveRange(20, entries.Count - 20);
                Save(entries);
            }
        }

        public void Remove(string projectFile)
        {
            lock (_writeLock)
            {
                var entries = new List<RecentProject>(Load());
                entries.RemoveAll(entry => entry.ProjectFile.Equals(projectFile, StringComparison.OrdinalIgnoreCase));
                Save(entries);
            }
        }

        private void Save(List<RecentProject> entries)
        {
            var historyDirectory = Path.GetDirectoryName(_filePath)!;
            Directory.CreateDirectory(historyDirectory);
            var stagingFile = Path.Combine(historyDirectory, $".veris-history-{Guid.NewGuid():N}.tmp");
            try
            {
                File.WriteAllText(stagingFile, JsonSerializer.Serialize(entries, JsonOptions));
                File.Move(stagingFile, _filePath, true);
            }
            finally
            {
                File.Delete(stagingFile);
            }
        }
    }
}
