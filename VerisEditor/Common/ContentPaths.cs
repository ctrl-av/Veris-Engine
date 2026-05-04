using System.IO;
using VerisEditor.GameProject;

namespace VerisEditor
{
    public static class ContentPaths
    {
        public static string Resolve(string root, string relativePath)
        {
            ProjectPaths.ValidateRelative(relativePath);
            var fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
            var path = Path.GetFullPath(Path.Combine(fullRoot, relativePath));
            if (!path.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Content paths must stay inside the content root.");
            for (var ancestor = path; ancestor != null && !ancestor.Equals(fullRoot, StringComparison.OrdinalIgnoreCase); ancestor = Path.GetDirectoryName(ancestor))
                if ((File.Exists(ancestor) || Directory.Exists(ancestor)) && (File.GetAttributes(ancestor) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Content paths cannot follow linked files or directories.");
            return path;
        }
    }
}
