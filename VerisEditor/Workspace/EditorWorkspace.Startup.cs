using System.IO;
using System.Text.Json.Nodes;
using VerisEditor.GameProject;

namespace VerisEditor.Workspace
{
    public sealed partial class EditorWorkspace
    {
        public string StartupSceneInput { get; set; } = "";

        public void SaveStartingScene() => Run(() =>
        {
            RequireEditing();
            var relative = string.IsNullOrWhiteSpace(StartupSceneInput) ? null : StartupSceneInput;
            if (relative != null) { ScenePath(relative); _engine.ValidateSceneFile(relative); }
            var document = JsonNode.Parse(File.ReadAllText(ProjectFile))!;
            document["startupScene"] = relative;
            var project = ProjectDocument.Parse(document.ToJsonString());
            AtomicFile.WriteText(ProjectFile, project.Serialize());
            Project = project;
            OnPropertyChanged(nameof(Project));
        });

        public void PlayStartingScene() => Run(() =>
        {
            RequireEditing();
            if (Project.StartupScene == null) throw new ArgumentException("Configure and save a starting scene first.");
            Tools.Cancel();
            CreatePlayEngine();
            try { _playEngine!.LoadSceneFile(Project.StartupScene); BeginPlayback(); }
            catch { StopPlay(); throw; }
        });
    }
}
