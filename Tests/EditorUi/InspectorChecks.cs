using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VerisEditor.Workspace;

internal static class InspectorChecks
{
    public static void Run(WorkspaceView view, EditorWorkspace workspace)
    {
        workspace.CreateEntity("Inspector sprite");
        var entity = workspace.SelectedEntityId;
        Program.Arrange(view, 1280, 720);
        ((TextBox)view.FindName("PositionXInput")).Text = "32";
        ((TextBox)view.FindName("PositionYInput")).Text = "48";
        ((TextBox)view.FindName("ScaleXInput")).Text = "16";
        ((TextBox)view.FindName("ScaleYInput")).Text = "16";
        ((Button)view.FindName("ApplyTransformButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Program.Assert(workspace.Inspector.PositionX == "32" && workspace.Inspector.ScaleX == "16", "Transform edits must reload native component values.");
        ((TextBox)view.FindName("PositionXInput")).Text = "NaN";
        workspace.ApplyTransform();
        Program.Assert(workspace.Status.Contains("finite"), "Invalid transform input must produce actionable feedback.");
        workspace.Inspector.Load(entity);
        Program.Assert(workspace.Inspector.PositionX == "32", "Rejected transform drafts must preserve native state.");
        VerifySprite(view, workspace, entity);
        workspace.DeleteSelected();
    }

    private static void VerifySprite(WorkspaceView view, EditorWorkspace workspace, ulong entity)
    {
        SaveTexture(Path.Combine(workspace.ContentRoot, "inspector.png"));
        ((TextBox)view.FindName("TexturePathInput")).Text = "inspector.png";
        ((TextBox)view.FindName("TintInput")).Text = "E84050FF";
        ((Button)view.FindName("ApplySpriteButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Program.Assert(workspace.Inspector.HasSprite, "Add sprite must create a native component.");
        var viewport = (EngineViewport)view.FindName("SceneViewport");
        viewport.RenderFrame();
        var pixel = new byte[4];
        viewport.Frame!.CopyPixels(new Int32Rect(40, 50, 1, 1), pixel, 4, 0);
        Program.Assert(pixel[0] == 80 && pixel[1] == 64 && pixel[2] == 232, "Inspector sprite changes must affect native framebuffer pixels.");
        workspace.Inspector.TexturePath = "missing.png";
        workspace.ApplySprite();
        workspace.Inspector.Load(entity);
        Program.Assert(workspace.Inspector.TexturePath == "inspector.png", "Failed texture loads must preserve the previous sprite.");
        workspace.Inspector.UseRectangle = true;
        workspace.Inspector.RectangleWidth = "3";
        workspace.Inspector.RectangleHeight = "1";
        workspace.ApplySprite();
        workspace.Inspector.Load(entity);
        Program.Assert(!workspace.Inspector.UseRectangle, "Rejected atlas rectangles must preserve the previous component.");
        ((Button)view.FindName("RemoveSpriteButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Program.Assert(!workspace.Inspector.HasSprite, "Remove sprite must release the native component.");
    }

    private static void SaveTexture(string path)
    {
        var pixels = Enumerable.Repeat((byte)255, 16).ToArray();
        var texture = BitmapSource.Create(2, 2, 96, 96, PixelFormats.Bgra32, null, pixels, 8);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(texture));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}
