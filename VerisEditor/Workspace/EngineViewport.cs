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
            MouseMove += OnPointerMove;
            MouseLeftButtonUp += OnPointerUp;
            LostMouseCapture += OnCaptureLost;
            KeyDown += OnKeyDown;
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
            var pixel = ToPixel(position);
            workspace.SelectAt(pixel.X, pixel.Y);
            RenderFrame();
        }

        private void OnPointerDown(object sender, MouseButtonEventArgs e)
        {
            Focus();
            if (_bitmap == null || DataContext is not EditorWorkspace workspace) return;
            var position = e.GetPosition(this);
            var onHandle = workspace.HasSelection && HitTransformHandle(position, workspace);
            if (!onHandle) SelectAt(position);
            if (workspace.HasSelection && (onHandle || workspace.Tools.Mode == TransformMode.Move))
            {
                var pixel = ToPixel(position);
                workspace.BeginTransformDrag(pixel.X, pixel.Y);
                if (workspace.Tools.IsDragging) CaptureMouse();
            }
            e.Handled = true;
        }

        private void OnPointerMove(object sender, MouseEventArgs e)
        {
            if (!IsMouseCaptured || DataContext is not EditorWorkspace workspace || !workspace.Tools.IsDragging) return;
            var pixel = ToPixel(e.GetPosition(this));
            workspace.Tools.TemporarySnap = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
            workspace.DragTransform(pixel.X, pixel.Y);
            RenderFrame();
            e.Handled = true;
        }

        private void OnPointerUp(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is EditorWorkspace workspace) workspace.Tools.Complete();
            ReleaseMouseCapture();
            e.Handled = true;
        }

        private void OnCaptureLost(object sender, MouseEventArgs e)
        {
            if (DataContext is EditorWorkspace workspace && !workspace.IsDisposed) workspace.CancelTransformDrag();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is not EditorWorkspace workspace) return;
            if (e.Key == Key.Escape) { workspace.CancelTransformDrag(); ReleaseMouseCapture(); }
            else if (e.Key == Key.W) workspace.Tools.Mode = TransformMode.Move;
            else if (e.Key == Key.E) workspace.Tools.Mode = TransformMode.Rotate;
            else if (e.Key == Key.R) workspace.Tools.Mode = TransformMode.Scale;
            else return;
            RenderFrame();
            e.Handled = true;
        }

        private bool HitTransformHandle(Point pointer, EditorWorkspace workspace)
        {
            var pivot = ToControlPoint(workspace.EntityOrigin(workspace.SelectedEntityId));
            var offset = pointer - pivot;
            return workspace.Tools.Mode switch
            {
                TransformMode.Rotate => Math.Abs(offset.Length - 32) <= 8,
                TransformMode.Scale => (pointer - (pivot + new Vector(24, -24))).Length <= 10,
                _ => offset.Length <= 12
            };
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
            if (workspace.HasSelection) DrawTransformHandle(drawing, workspace);
        }

        private void DrawTransformHandle(DrawingContext drawing, EditorWorkspace workspace)
        {
            var pivot = ToControlPoint(workspace.EntityOrigin(workspace.SelectedEntityId));
            switch (workspace.Tools.Mode)
            {
                case TransformMode.Rotate:
                    drawing.DrawEllipse(null, SelectionPen, pivot, 32, 32);
                    break;
                case TransformMode.Scale:
                    var handle = pivot + new Vector(24, -24);
                    drawing.DrawLine(SelectionPen, pivot, handle);
                    drawing.DrawRectangle(Brushes.Gold, null, new Rect(handle - new Vector(4, 4), new Size(8, 8)));
                    break;
                default:
                    drawing.DrawLine(SelectionPen, pivot - new Vector(12, 0), pivot + new Vector(12, 0));
                    drawing.DrawLine(SelectionPen, pivot - new Vector(0, 12), pivot + new Vector(0, 12));
                    drawing.DrawRectangle(null, SelectionPen, new Rect(pivot - new Vector(5, 5), new Size(10, 10)));
                    break;
            }
        }

        private (int X, int Y) ToPixel(Point position) => ((int)(position.X * _bitmap!.PixelWidth / ActualWidth), (int)(position.Y * _bitmap.PixelHeight / ActualHeight));

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
            if (DataContext is EditorWorkspace workspace && !workspace.IsDisposed) workspace.CancelTransformDrag();
            ReleaseMouseCapture();
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
