namespace VerisEditor.Workspace
{
    public sealed partial class EditorWorkspace
    {
        public string AudioJson { get; set; } = "null";
        public void ApplyAudio() => Edit("Edit audio source", () => _engine.SetAudio(SelectedEntityId, AudioJson));
        public void PlayAudio() => Run(() => _engine.PlayAudio(SelectedEntityId));
        public void PauseAudio() => Run(() => _engine.PauseAudio(SelectedEntityId));
        public void ResumeAudio() => Run(() => _engine.ResumeAudio(SelectedEntityId));
        public void StopAudio() => Run(() => _engine.StopAudio(SelectedEntityId));
        private void ReloadAsset(Assets.AssetRecord asset)
        {
            if (asset.Kind == "texture") _engine.ReloadTexture(asset.Path);
            else _engine.ReloadAudio(asset.Path);
        }
    }
}
