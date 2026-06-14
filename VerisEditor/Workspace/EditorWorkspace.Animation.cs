namespace VerisEditor.Workspace
{
    public sealed partial class EditorWorkspace
    {
        public string AnimationJson { get; set; } = "null";
        public void ApplyAnimation() => Edit("Edit sprite animation", () => _engine.SetAnimation(SelectedEntityId, AnimationJson));
        public void PreviewAnimation() => Run(() => _engine.AdvanceAnimation(SelectedEntityId, 0.1));
    }
}
