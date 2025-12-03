using System;
using System.Diagnostics;
using System.IO;

namespace VerisEditor
{
    public static class EditorLog
    {
        private static readonly object _writeLock = new();
        public static string? FilePath { get; private set; }

        public static void Initialize(string logDirectory)
        {
            lock (_writeLock)
            {
                FilePath = Path.Combine(logDirectory, $"editor-{DateTime.UtcNow:yyyyMMdd}.log");
                Write("INFO", "Editor session started.");
            }
        }

        public static void Info(string message) => Write("INFO", message);
        public static void Error(string message, Exception exception) => Write("ERROR", $"{message}{Environment.NewLine}{exception}");

        private static void Write(string level, string message)
        {
            var entry = $"{DateTimeOffset.UtcNow:O} [{level}] {message}{Environment.NewLine}";
            Trace.Write(entry);
            lock (_writeLock)
            {
                if (FilePath == null) return;
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                    File.AppendAllText(FilePath, entry);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    Trace.WriteLine($"Cannot write editor log: {exception.Message}");
                }
            }
        }
    }
}
