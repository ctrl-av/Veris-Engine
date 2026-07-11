namespace VerisEditor.Workspace
{
    public sealed partial class EditorWorkspace
    {
        public string PhysicsJson { get; set; } = "null";
        private bool _physicsDebug;
        public bool PhysicsDebug
        {
            get => _physicsDebug;
            set { _engine.SetPhysicsDebug(value); _physicsDebug = value; OnPropertyChanged(nameof(PhysicsDebug)); }
        }
        public string ContactDetails { get; private set; } = "No contacts in the last fixed frame";
        public void ApplyPhysics() => Edit("Edit physics body", () => _engine.SetPhysics(SelectedEntityId, PhysicsJson));
        public void StepPhysics() => Edit("Simulate one fixed frame", () =>
        {
            _engine.FixedUpdate(1.0 / 60);
            if (HasSelection) Inspector.Load(SelectedEntityId);
            PhysicsJson = HasSelection ? _engine.Physics(SelectedEntityId) : "null";
            OnPropertyChanged(nameof(PhysicsJson));
            ContactDetails = _engine.Contacts();
            OnPropertyChanged(nameof(ContactDetails));
        });
    }
}
