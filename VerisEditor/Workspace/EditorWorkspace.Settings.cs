using System.Globalization;
using System.IO;
using System.Text.Json.Nodes;
using VerisEditor.GameProject;
using VerisEditor.Interop;

namespace VerisEditor.Workspace
{
    public sealed partial class EditorWorkspace
    {
        public static string[] WindowModes { get; } = { "windowed", "fullscreen" };
        public string SettingName { get; set; } = "";
        public string SettingWidth { get; set; } = "1280";
        public string SettingHeight { get; set; } = "720";
        public string SettingWindowMode { get; set; } = "windowed";
        public bool SettingResizable { get; set; } = true;
        public bool SettingVsync { get; set; } = true;
        public string SettingGravityX { get; set; } = "0";
        public string SettingGravityY { get; set; } = "9.81";
        public string SettingMasterVolume { get; set; } = "100";
        public string InputBindingsJson { get; set; } = "";

        public void SaveProjectSettings() => Run(() =>
        {
            RequireEditing();
            var document = JsonNode.Parse(File.ReadAllText(ProjectFile))!;
            document["name"] = SettingName;
            document["windowWidth"] = int.Parse(SettingWidth, CultureInfo.InvariantCulture);
            document["windowHeight"] = int.Parse(SettingHeight, CultureInfo.InvariantCulture);
            document["windowMode"] = SettingWindowMode;
            document["windowResizable"] = SettingResizable;
            document["verticalSync"] = SettingVsync;
            document["gravityX"] = float.Parse(SettingGravityX, CultureInfo.InvariantCulture);
            document["gravityY"] = float.Parse(SettingGravityY, CultureInfo.InvariantCulture);
            document["masterVolume"] = float.Parse(SettingMasterVolume, CultureInfo.InvariantCulture);
            var project = ProjectDocument.Parse(document.ToJsonString());
            AtomicFile.WriteText(ProjectFile, project.Serialize());
            Project = project;
            _engine.ConfigureSimulation(project.GravityX, project.GravityY, project.MasterVolume);
            OnPropertyChanged(nameof(Project)); OnPropertyChanged(nameof(WindowSummary));
        });

        public void SaveInputBindings() => Run(() =>
        {
            RequireEditing(); NativeEngine.ValidateInputJson(InputBindingsJson);
            var root = Path.GetDirectoryName(ProjectFile)!;
            AtomicFile.WriteText(ContentPaths.Resolve(root, ".Veris/input-actions.json"), InputBindingsJson);
            _engine.ConfigureInput(root);
        });

        private void InitializeProjectSettings()
        {
            SettingName = Project.Name;
            SettingWidth = Project.WindowWidth.ToString(CultureInfo.InvariantCulture);
            SettingHeight = Project.WindowHeight.ToString(CultureInfo.InvariantCulture);
            SettingWindowMode = Project.WindowMode; SettingResizable = Project.WindowResizable; SettingVsync = Project.VerticalSync;
            SettingGravityX = Project.GravityX.ToString(CultureInfo.InvariantCulture);
            SettingGravityY = Project.GravityY.ToString(CultureInfo.InvariantCulture);
            SettingMasterVolume = Project.MasterVolume.ToString(CultureInfo.InvariantCulture);
            var inputPath = ContentPaths.Resolve(Path.GetDirectoryName(ProjectFile)!, ".Veris/input-actions.json");
            InputBindingsJson = File.Exists(inputPath) ? File.ReadAllText(inputPath) : "{\"formatVersion\":1,\"actions\":{\"Quit\":[\"Key:Escape\"]}}";
            _engine.ConfigureSimulation(Project.GravityX, Project.GravityY, Project.MasterVolume);
        }
    }
}
