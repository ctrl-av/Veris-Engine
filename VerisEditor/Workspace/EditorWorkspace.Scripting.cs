namespace VerisEditor.Workspace
{
    public sealed partial class EditorWorkspace
    {
        public string ScriptPath { get; set; } = "player.lua";
        public void ApplyScript() => Edit("Assign gameplay script", () => _engine.SetScript(SelectedEntityId,
            System.Text.Json.JsonSerializer.Serialize(new { path = ScriptPath, enabled = true })));
        public void RemoveScript() => Edit("Remove gameplay script", () => _engine.SetScript(SelectedEntityId, "null"));
    }
}
