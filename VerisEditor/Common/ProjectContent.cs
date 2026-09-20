using System.IO;
using System.Security.Cryptography;

namespace VerisEditor
{
    public static class ProjectContent
    {
        public static void PublishDirectory(string source, string destination)
        {
            for (var attempt = 0; ; attempt++)
            {
                try { Directory.Move(source, destination); return; }
                catch (IOException exception) when (attempt < 4 && (exception.HResult & 65535) is 5 or 32 or 33 &&
                    Directory.Exists(source) && !Directory.Exists(destination) && !File.Exists(destination))
                {
                    Thread.Sleep(50 * (attempt + 1));
                }
            }
        }

        public static void CopyFile(string source, string target)
        {
            File.Copy(source, target, true); File.SetAttributes(target, FileAttributes.Normal);
        }
        public static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        }

        public static void CopyTree(string source, string destination)
        {
            source = Path.TrimEndingDirectorySeparator(Path.GetFullPath(source));
            destination = Path.TrimEndingDirectorySeparator(Path.GetFullPath(destination));
            if (destination.Equals(source, StringComparison.OrdinalIgnoreCase) || destination.StartsWith(source + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Content cannot be copied into itself.");
            var pending = new Stack<string>(); pending.Push(source);
            long bytes = 0; var files = 0;
            while (pending.Count != 0)
            {
                var directory = pending.Pop();
                if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Game content cannot contain linked directories.");
                Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
                foreach (var entry in new DirectoryInfo(directory).GetFileSystemInfos())
                {
                    if ((entry.Attributes & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Game content cannot contain links.");
                    if (entry is DirectoryInfo) { pending.Push(entry.FullName); continue; }
                    bytes += ((FileInfo)entry).Length;
                    if (++files > 10000 || bytes > 256 * 1024 * 1024) throw new InvalidDataException("Game content exceeds 10000 files or 256 MiB.");
                    var target = Path.Combine(destination, Path.GetRelativePath(source, entry.FullName));
                    CopyFile(entry.FullName, target);
                }
            }
        }
    }
}
