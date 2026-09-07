using System.IO;
using VerisEditor.Build;

namespace VerisEditor.Workspace
{
    public sealed partial class EditorWorkspace
    {
        public void ExportGame(string destination) => Run(() =>
        {
            RequireEditing(); Tools.Cancel();
            if (IsDirty) throw new ArgumentException("Save the editing scene before exporting; export uses saved files.");
            var executable = ProjectExporter.Export(ProjectFile, destination, Path.Combine(AppContext.BaseDirectory, "Runtime"));
            EditorLog.Info("Exported standalone game: " + executable);
            RefreshOutput();
        });
    }
}
