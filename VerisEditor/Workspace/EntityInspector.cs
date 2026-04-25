using System.Globalization;
using VerisEditor.Interop;

namespace VerisEditor.Workspace
{
    public sealed class EntityInspector(NativeEngine engine) : ViewModelBase
    {
        private ulong _entity;
        public bool HasEntity => _entity != 0;
        public bool HasSprite { get; private set; }
        public string PositionX { get; set; } = "0";
        public string PositionY { get; set; } = "0";
        public string ScaleX { get; set; } = "1";
        public string ScaleY { get; set; } = "1";
        public string Rotation { get; set; } = "0";
        public string TexturePath { get; set; } = "";
        public string Tint { get; set; } = "FFFFFFFF";
        public string OriginX { get; set; } = "0";
        public string OriginY { get; set; } = "0";
        public string Layer { get; set; } = "0";
        public bool Visible { get; set; } = true;
        public bool UseRectangle { get; set; }
        public string RectangleX { get; set; } = "0";
        public string RectangleY { get; set; } = "0";
        public string RectangleWidth { get; set; } = "1";
        public string RectangleHeight { get; set; } = "1";

        public void Load(ulong entity)
        {
            _entity = entity;
            var transform = entity == 0 ? new EntityTransform(0, 0, 1, 1, 0) : engine.GetTransform(entity);
            PositionX = transform.PositionX.ToString("R", CultureInfo.InvariantCulture);
            PositionY = transform.PositionY.ToString("R", CultureInfo.InvariantCulture);
            ScaleX = transform.ScaleX.ToString("R", CultureInfo.InvariantCulture);
            ScaleY = transform.ScaleY.ToString("R", CultureInfo.InvariantCulture);
            Rotation = transform.Rotation.ToString("R", CultureInfo.InvariantCulture);
            var sprite = entity == 0 ? null : engine.GetSprite(entity);
            LoadSprite(sprite);
            OnPropertyChanged(string.Empty);
        }

        public void ApplyTransform()
        {
            var transform = new EntityTransform(ParseNumber(PositionX, "Position X"), ParseNumber(PositionY, "Position Y"),
                ParseNumber(ScaleX, "Scale X"), ParseNumber(ScaleY, "Scale Y"), ParseNumber(Rotation, "Rotation"));
            engine.SetTransform(_entity, transform);
            Load(_entity);
        }

        public void ApplySprite()
        {
            if (Tint.Length != 8 || !uint.TryParse(Tint, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var tint))
                throw new ArgumentException("Tint must contain eight hexadecimal RGBA digits.");
            var sprite = new SpriteProperties
            {
                OriginX = ParseNumber(OriginX, "Origin X"), OriginY = ParseNumber(OriginY, "Origin Y"), Tint = tint,
                Layer = ParseInteger(Layer, "Layer"), Visible = Visible ? 1 : 0, UseRectangle = UseRectangle ? 1 : 0,
                RectangleX = ParseInteger(RectangleX, "Atlas X"), RectangleY = ParseInteger(RectangleY, "Atlas Y"),
                RectangleWidth = ParseInteger(RectangleWidth, "Atlas width"), RectangleHeight = ParseInteger(RectangleHeight, "Atlas height")
            };
            engine.SetSprite(_entity, TexturePath, sprite);
            Load(_entity);
        }

        public void RemoveSprite()
        {
            engine.RemoveSprite(_entity);
            Load(_entity);
        }

        private void LoadSprite(SpriteProperties? sprite)
        {
            HasSprite = sprite.HasValue;
            TexturePath = sprite.HasValue ? engine.SpritePath(_entity) : "";
            var properties = sprite ?? new SpriteProperties { Tint = uint.MaxValue, Visible = 1, RectangleWidth = 1, RectangleHeight = 1 };
            Tint = properties.Tint.ToString("X8", CultureInfo.InvariantCulture);
            OriginX = properties.OriginX.ToString("R", CultureInfo.InvariantCulture);
            OriginY = properties.OriginY.ToString("R", CultureInfo.InvariantCulture);
            Layer = properties.Layer.ToString(CultureInfo.InvariantCulture);
            Visible = properties.Visible != 0;
            UseRectangle = properties.UseRectangle != 0;
            RectangleX = properties.RectangleX.ToString(CultureInfo.InvariantCulture);
            RectangleY = properties.RectangleY.ToString(CultureInfo.InvariantCulture);
            RectangleWidth = properties.RectangleWidth.ToString(CultureInfo.InvariantCulture);
            RectangleHeight = properties.RectangleHeight.ToString(CultureInfo.InvariantCulture);
        }

        private static float ParseNumber(string text, string name)
        {
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || !float.IsFinite(number))
                throw new ArgumentException($"{name} must be a finite number using a decimal point.");
            return number;
        }

        private static int ParseInteger(string text, string name)
        {
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)) throw new ArgumentException($"{name} must be a whole number.");
            return number;
        }
    }
}
