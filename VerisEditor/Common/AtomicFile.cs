using System.IO;
using System.Text;

namespace VerisEditor
{
    public static class AtomicFile
    {
        public static void WriteText(string path, string text)
        {
            var temporary = Path.Combine(Path.GetDirectoryName(path)!, $".veris-save-{Guid.NewGuid():N}.tmp");
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    var bytes = new UTF8Encoding(false, true).GetBytes(text);
                    stream.Write(bytes);
                    stream.Flush(true);
                }
                File.Move(temporary, path, true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
