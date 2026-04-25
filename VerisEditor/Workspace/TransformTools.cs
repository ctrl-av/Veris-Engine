using System.Globalization;
using VerisEditor.Interop;

namespace VerisEditor.Workspace
{
    public enum TransformMode { Move, Rotate, Scale }

    public sealed class TransformTools(NativeEngine engine, EntityInspector inspector) : ViewModelBase
    {
        private ulong _entity;
        private EntityTransform _initial;
        private EntityPoint _start;
        private TransformMode _mode;
        public static IReadOnlyList<TransformMode> Modes { get; } = Enum.GetValues<TransformMode>();
        public TransformMode Mode
        {
            get => _mode;
            set { if (_mode == value) return; Cancel(); _mode = value; OnPropertyChanged(nameof(Mode)); }
        }
        public bool SnapEnabled { get; set; }
        public bool TemporarySnap { get; set; }
        public string MoveStep { get; set; } = "16";
        public string AngleStep { get; set; } = "15";
        public string ScaleStep { get; set; } = "0.1";
        public bool IsDragging => _entity != 0;

        public void Begin(ulong entity, int pixelX, int pixelY)
        {
            Cancel();
            if (entity == 0) return;
            var initial = engine.GetTransform(entity);
            var start = engine.ParentCoordinates(entity, pixelX, pixelY);
            var distance = MathF.Sqrt(MathF.Pow(start.X - initial.PositionX, 2) + MathF.Pow(start.Y - initial.PositionY, 2));
            if (Mode != TransformMode.Move && distance < 0.001f) throw new ArgumentException("Start the drag on the rotation ring or scale handle, away from the pivot.");
            _initial = initial;
            _start = start;
            _entity = entity;
        }

        public void Drag(int pixelX, int pixelY)
        {
            if (!IsDragging) return;
            var pointer = engine.ParentCoordinates(_entity, pixelX, pixelY);
            var transform = Mode switch
            {
                TransformMode.Move => Move(pointer),
                TransformMode.Rotate => Rotate(pointer),
                TransformMode.Scale => Scale(pointer),
                _ => throw new ArgumentOutOfRangeException(nameof(Mode))
            };
            engine.SetTransform(_entity, transform);
            inspector.Load(_entity);
        }

        public void Complete()
        {
            _entity = 0;
            TemporarySnap = false;
        }

        public void Cancel()
        {
            TemporarySnap = false;
            if (!IsDragging) return;
            var entity = _entity;
            _entity = 0;
            engine.SetTransform(entity, _initial);
            inspector.Load(entity);
        }

        private EntityTransform Move(EntityPoint pointer)
        {
            var x = Snap(_initial.PositionX + pointer.X - _start.X, MoveStep);
            var y = Snap(_initial.PositionY + pointer.Y - _start.Y, MoveStep);
            return new EntityTransform(x, y, _initial.ScaleX, _initial.ScaleY, _initial.Rotation);
        }

        private EntityTransform Rotate(EntityPoint pointer)
        {
            var startAngle = MathF.Atan2(_start.Y - _initial.PositionY, _start.X - _initial.PositionX);
            var angle = MathF.Atan2(pointer.Y - _initial.PositionY, pointer.X - _initial.PositionX);
            var delta = MathF.IEEERemainder(angle - startAngle, MathF.Tau) * 180 / MathF.PI;
            return new EntityTransform(_initial.PositionX, _initial.PositionY, _initial.ScaleX, _initial.ScaleY, Snap(_initial.Rotation + delta, AngleStep));
        }

        private EntityTransform Scale(EntityPoint pointer)
        {
            var startDistance = MathF.Sqrt(MathF.Pow(_start.X - _initial.PositionX, 2) + MathF.Pow(_start.Y - _initial.PositionY, 2));
            var distance = MathF.Sqrt(MathF.Pow(pointer.X - _initial.PositionX, 2) + MathF.Pow(pointer.Y - _initial.PositionY, 2));
            var factor = Math.Clamp(distance / startDistance, 0.001f, 1000);
            var x = Snap(_initial.ScaleX * factor, ScaleStep);
            var y = Snap(_initial.ScaleY * factor, ScaleStep);
            return new EntityTransform(_initial.PositionX, _initial.PositionY, x, y, _initial.Rotation);
        }

        private float Snap(float number, string stepText)
        {
            if (!SnapEnabled && !TemporarySnap) return number;
            if (!float.TryParse(stepText, NumberStyles.Float, CultureInfo.InvariantCulture, out var step) || !float.IsFinite(step) || step <= 0)
                throw new ArgumentException("The active snap increment must be a finite positive number.");
            return MathF.Round(number / step, MidpointRounding.AwayFromZero) * step;
        }
    }
}
