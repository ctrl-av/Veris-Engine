using System.Globalization;
using System.Windows.Controls;
using VerisEditor.Workspace;

internal static class TransformChecks
{
    public static void Run(WorkspaceView view, EditorWorkspace workspace)
    {
        workspace.CreateEntity("Manipulated");
        var entity = workspace.SelectedEntityId;
        SetTransform(workspace, 64, 64, 1, 1, 0);
        ((EngineViewport)view.FindName("SceneViewport")).RenderFrame();
        VerifyMoveAndCancel(workspace);
        VerifyRotationAndScale(workspace);
        VerifyParentCoordinates(workspace, entity);
        workspace.CreateEntity("Viewport sample");
        SetTransform(workspace, 120, 120, 48, 48, 15);
        workspace.Inspector.TexturePath = "inspector.png";
        workspace.Inspector.Tint = "66B5E8FF";
        workspace.ApplySprite();
        ((ComboBox)view.FindName("TransformToolInput")).SelectedItem = TransformMode.Move;
        ((EngineViewport)view.FindName("SceneViewport")).RenderFrame();
    }

    private static void VerifyMoveAndCancel(EditorWorkspace workspace)
    {
        workspace.BeginTransformDrag(64, 64);
        workspace.DragTransform(80, 90);
        Program.Assert(workspace.Inspector.PositionX == "80" && workspace.Inspector.PositionY == "90", "Move must apply pointer deltas to native transforms.");
        workspace.CancelTransformDrag();
        Program.Assert(workspace.Inspector.PositionX == "64" && workspace.Inspector.PositionY == "64", "Cancel must restore the complete starting transform.");
        workspace.Tools.SnapEnabled = true;
        workspace.BeginTransformDrag(64, 64);
        workspace.DragTransform(85, 87);
        workspace.Tools.Complete();
        Program.Assert(workspace.Inspector.PositionX == "80" && workspace.Inspector.PositionY == "80", "Move snapping must quantize parent-local coordinates.");
    }

    private static void VerifyRotationAndScale(EditorWorkspace workspace)
    {
        workspace.Tools.Mode = TransformMode.Rotate;
        workspace.BeginTransformDrag(112, 80);
        workspace.DragTransform(80, 112);
        workspace.Tools.Complete();
        Program.Assert(Math.Abs(float.Parse(workspace.Inspector.Rotation, CultureInfo.InvariantCulture) - 90) < 0.01f, "Rotation drags must apply snapped angular deltas around the entity pivot.");
        workspace.Tools.Mode = TransformMode.Scale;
        workspace.BeginTransformDrag(112, 80);
        workspace.DragTransform(144, 80);
        workspace.Tools.Complete();
        Program.Assert(workspace.Inspector.ScaleX == "2" && workspace.Inspector.ScaleY == "2", "Scale drags must apply uniform ratios to native scale.");
        workspace.Tools.ScaleStep = "0";
        workspace.BeginTransformDrag(112, 80);
        workspace.DragTransform(144, 80);
        Program.Assert(workspace.Status.Contains("positive") && workspace.Inspector.ScaleX == "2", "Invalid snap settings must preserve the last good transform.");
        workspace.CancelTransformDrag();
        workspace.Tools.ScaleStep = "0.1";
    }

    private static void VerifyParentCoordinates(EditorWorkspace workspace, ulong child)
    {
        workspace.CreateEntity("Parent transform");
        var parent = workspace.SelectedEntityId;
        SetTransform(workspace, 100, 100, 2, 2, 90);
        workspace.SelectEntity(child);
        SetTransform(workspace, 10, 0, 1, 1, 0);
        workspace.ReparentSelected(parent);
        workspace.Tools.Mode = TransformMode.Move;
        workspace.Tools.SnapEnabled = false;
        workspace.BeginTransformDrag(100, 120);
        workspace.DragTransform(100, 140);
        workspace.Tools.Complete();
        Program.Assert(Math.Abs(float.Parse(workspace.Inspector.PositionX, CultureInfo.InvariantCulture) - 20) < 0.01f,
            "Movement must invert rotated and scaled parent coordinates.");
        workspace.BeginTransformDrag(100, 140);
        workspace.DragTransform(100, 160);
        workspace.SelectEntity(parent);
        workspace.SelectEntity(child);
        Program.Assert(Math.Abs(float.Parse(workspace.Inspector.PositionX, CultureInfo.InvariantCulture) - 20) < 0.01f, "Selection changes must cancel the active drag.");
        workspace.SelectEntity(parent);
        SetTransform(workspace, 100, 100, 0, 2, 90);
        workspace.SelectEntity(child);
        workspace.BeginTransformDrag(100, 140);
        Program.Assert(!workspace.Tools.IsDragging && workspace.Status.Contains("collapsed"), "Collapsed parent transforms must reject manipulation safely.");
        workspace.SelectEntity(parent);
        workspace.DeleteSelected();
    }

    private static void SetTransform(EditorWorkspace workspace, float x, float y, float scaleX, float scaleY, float rotation)
    {
        workspace.Inspector.PositionX = x.ToString(CultureInfo.InvariantCulture);
        workspace.Inspector.PositionY = y.ToString(CultureInfo.InvariantCulture);
        workspace.Inspector.ScaleX = scaleX.ToString(CultureInfo.InvariantCulture);
        workspace.Inspector.ScaleY = scaleY.ToString(CultureInfo.InvariantCulture);
        workspace.Inspector.Rotation = rotation.ToString(CultureInfo.InvariantCulture);
        workspace.ApplyTransform();
    }
}
