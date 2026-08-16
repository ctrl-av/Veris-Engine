using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VerisEditor.GameProject;
using VerisEditor.Workspace;

internal static class WorkspaceChecks
{
    public static void Run()
    {
        var parent = Path.Combine(AppContext.BaseDirectory, $"UiWorkspace-{Guid.NewGuid():N}");
        try
        {
            var template = ProjectTemplate.Load(Path.Combine(AppContext.BaseDirectory, "ProjectTemplates", "EmptyProject", "template.xml"));
            var projectFile = ProjectFiles.Create("Workspace Sample", parent, template);
            var project = ProjectDocument.Parse(File.ReadAllText(projectFile));
            using var workspace = new EditorWorkspace(project, projectFile);
            Directory.CreateDirectory(Path.Combine(workspace.ContentRoot, "Sprites"));
            File.WriteAllText(Path.Combine(workspace.ContentRoot, "notes.txt"), "Workspace fixture");
            var view = new WorkspaceView(workspace);
            VerifyPanels(view, workspace);
            VerifyContentNavigation(view, workspace);
            VerifyViewport(view);
            HistoryChecks.Run(workspace);
            HierarchyChecks.Run(view, workspace);
            SelectionChecks.Run(view, workspace);
            InspectorChecks.Run(view, workspace);
            TransformChecks.Run(view, workspace);
            SceneFileChecks.Run(workspace);
            UnsavedChecks.Run(workspace);
            AssetImportChecks.Run(view, workspace);
            AssetBrowserChecks.Run(view, workspace);
            AssetChangeChecks.Run(workspace);
            AnimationChecks.Run(view, workspace);
            AudioChecks.Run(workspace);
            PhysicsChecks.Run(workspace);
            ContactChecks.Run(view, workspace);
            PlayChecks.Run(workspace);
            StartupChecks.Run(workspace);
            PrefabChecks.Run(view, workspace);
            Program.Arrange(view, 1280, 720);
            Program.SavePreview(view, "editor-workspace.png");
            workspace.Dispose();
            ((EngineViewport)view.FindName("SceneViewport")).RenderFrame();
            try { workspace.Refresh(); throw new InvalidOperationException("Disposed workspace must reject native operations."); }
            catch (ObjectDisposedException) { }
        }
        finally { if (Directory.Exists(parent)) Directory.Delete(parent, true); }
    }

    private static void VerifyPanels(WorkspaceView view, EditorWorkspace workspace)
    {
        ((Button)view.FindName("RefreshWorkspaceButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Program.Arrange(view, 1280, 720);
        foreach (var name in new[] { "HierarchyPanel", "ScenePanel", "InspectorPanel", "ContentPanel", "OutputPanel" })
        {
            var panel = (FrameworkElement)view.FindName(name);
            Program.Assert(panel.ActualWidth > 100 && panel.ActualHeight > 100, "Workspace panels must receive usable layout space.");
        }
        Program.Assert(((TreeView)view.FindName("SceneEntities")).Items.Count == 0, "A new project's hierarchy must reflect its empty native scene.");
        Program.Assert(((TextBox)view.FindName("OutputLog")).Text.Contains("Workspace opened:"), "Output must show actual editor log entries.");
        Program.Assert(((ListBox)view.FindName("ContentFiles")).Items.Count == 2, "Refresh must discover newly created content.");
        Program.Assert(workspace.SceneSummary.StartsWith("0 entities"), "Scene summary must reflect the native scene.");
    }

    private static void VerifyContentNavigation(WorkspaceView view, EditorWorkspace workspace)
    {
        var files = (ListBox)view.FindName("ContentFiles");
        files.SelectedIndex = 0;
        Program.Arrange(view, 1280, 720);
        Program.Assert(((TextBlock)view.FindName("ContentSelectionDetails")).Text.EndsWith("Sprites"), "Selected content must populate inspector details.");
        files.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Control.MouseDoubleClickEvent });
        Program.Assert(workspace.CurrentDirectory.EndsWith("Sprites") && files.Items.Count == 0, "Double-click must navigate into the selected directory.");
        ((Button)view.FindName("ContentUpButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        workspace.NavigateUp();
        Program.Assert(workspace.CurrentDirectory == workspace.ContentRoot, "Up must stop at the content root.");
        workspace.NavigateTo(Path.GetDirectoryName(workspace.ContentRoot)!);
        Program.Assert(workspace.CurrentDirectory == workspace.ContentRoot && workspace.Status.Contains("inside"), "Rejected navigation must preserve the previous directory.");
        File.WriteAllText(Path.Combine(workspace.ContentRoot, "new-file.txt"), "Refresh fixture");
        ((Button)view.FindName("RefreshWorkspaceButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Program.Assert(files.Items.Count == 3, "Refresh must update existing content bindings.");
    }

    private static void VerifyViewport(WorkspaceView view)
    {
        var viewport = (EngineViewport)view.FindName("SceneViewport");
        viewport.RenderFrame();
        var frame = viewport.Frame ?? throw new InvalidOperationException("Viewport must present a native frame.");
        var pixel = new byte[4];
        frame.CopyPixels(new Int32Rect(0, 0, 1, 1), pixel, 4, 0);
        Program.Assert(pixel[0] == 32 && pixel[1] == 26 && pixel[2] == 24 && pixel[3] == 255,
            "Viewport must convert native RGBA pixels to WPF BGRA without swapping channels incorrectly.");
        Program.Arrange(view, 1400, 800);
        viewport.RenderFrame();
        Program.Assert(viewport.Frame!.PixelWidth > frame.PixelWidth && viewport.Frame.PixelHeight > frame.PixelHeight,
            "Panel resize must update the native target and the WPF frame dimensions.");
    }
}
