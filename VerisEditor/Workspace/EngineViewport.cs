using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using VerisEditor.Interop;

namespace VerisEditor.Workspace
{
    public sealed class EngineViewport : UserControl
    {
        private readonly Image _image = new() { Stretch = Stretch.Fill };
        private readonly DrawingGroup _overlay = new();
        private static readonly Pen SelectionPen = new(Brushes.Gold, 1.5);
        private static readonly Pen EntityPen = new(new SolidColorBrush(Color.FromRgb(105, 125, 145)), 1);
        private WriteableBitmap? _bitmap;
        private byte[] _pixels = Array.Empty<byte>();
        private bool _rendering;
        private TimeSpan _lastFrame;
        public BitmapSource? Frame => _bitmap;

        public EngineViewport()
        {
            var layers = new Grid();
            layers.Children.Add(_image);
            layers.Children.Add(new Image { Source = new DrawingImage(_overlay), Stretch = Stretch.Fill, IsHitTestVisible = false });
            Content = layers;
            Background = new SolidColorBrush(Color.FromRgb(24, 26, 32));
            Focusable = true;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            MouseLeftButtonDown += OnPointerDown;
        }

        public void RenderFrame()
        {
            if (DataContext is not EditorWorkspace workspace || workspace.IsDisposed || ActualWidth < 1 || ActualHeight < 1) return;
            var dpi = VisualTreeHelper.GetDpi(this);
            var width = (int)Math.Clamp(Math.Ceiling(ActualWidth * dpi.DpiScaleX), 1, 4096);
            var height = (int)Math.Clamp(Math.Ceiling(ActualHeight * dpi.DpiScaleY), 1, 4096);
            if (_bitmap == null || _bitmap.PixelWidth != width || _bitmap.PixelHeight != height)
            {
                _bitmap = new WriteableBitmap(width, height, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Bgra32, null);
                _pixels = new byte[checked(width * height * 4)];
                _image.Source = _bitmap;
            }
            workspace.DrawViewport((uint)width, (uint)height, _pixels);
            for (var pixel = 0; pixel < _pixels.Length; pixel += 4)
                (_pixels[pixel], _pixels[pixel + 2]) = (_pixels[pixel + 2], _pixels[pixel]);
            _bitmap.WritePixels(new Int32Rect(0, 0, width, height), _pixels, width * 4, 0);
            DrawEntityOutlines(workspace);
        }

        public void SelectAt(Point position)
        {
            if (_bitmap == null || DataContext is not EditorWorkspace workspace || workspace.IsDisposed) return;
            workspace.SelectAt((int)(position.X * _bitmap.PixelWidth / ActualWidth), (int)(position.Y * _bitmap.PixelHeight / ActualHeight));
            RenderFrame();
        }

        private void OnPointerDown(object sender, MouseButtonEventArgs e)
        {
            Focus();
            SelectAt(e.GetPosition(this));
            e.Handled = true;
        }

        private void DrawEntityOutlines(EditorWorkspace workspace)
        {
            _overlay.ClipGeometry = new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight));
            using var drawing = _overlay.Open();
            drawing.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));
            foreach (var entity in workspace.Entities)
            {
                var corners = workspace.EntityCorners(entity.Id);
                var outline = new StreamGeometry();
                using (var geometry = outline.Open())
                {
                    geometry.BeginFigure(ToControlPoint(corners[0]), false, true);
                    for (var index = 1; index < corners.Length; index++) geometry.LineTo(ToControlPoint(corners[index]), true, false);
                }
                drawing.DrawGeometry(null, entity.Id == workspace.SelectedEntityId ? SelectionPen : EntityPen, outline);
            }
        }

        private Point ToControlPoint(EntityPoint pixel) => new(pixel.X * ActualWidth / _bitmap!.PixelWidth, pixel.Y * ActualHeight / _bitmap.PixelHeight);

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_rendering) return;
            _rendering = true;
            CompositionTarget.Rendering += OnRendering;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _rendering = false;
            CompositionTarget.Rendering -= OnRendering;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            if (!IsVisible || e is not RenderingEventArgs frame || frame.RenderingTime - _lastFrame < TimeSpan.FromMilliseconds(33)) return;
            _lastFrame = frame.RenderingTime;
            try { RenderFrame(); }
            catch (Exception exception) when (exception is NativeEngineException or ObjectDisposedException)
            {
                OnUnloaded(this, new RoutedEventArgs());
                if (DataContext is EditorWorkspace workspace) workspace.ReportViewportError(exception);
            }
        }
    }
}
