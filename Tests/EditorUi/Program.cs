using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VerisEditor;
using VerisEditor.GameProject;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        try
        {
            EditorLog.Initialize(Path.Combine(AppContext.BaseDirectory, "Logs"));
            var view = new NewProjectView();
            VerifyBrowserSelection(view);
            VerifyLocationValidation(view);
            SavePreview(view);
            VerifyProjectCreation(view);
            Console.WriteLine("PASS: Template browser selection, metadata, and preview bindings.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void VerifyBrowserSelection(NewProjectView view)
    {
        var templates = (ListBox)view.FindName("TemplatesList");
        var title = (TextBlock)view.FindName("TemplateName");
        Arrange(view);
        Assert(templates.Items.Count == 2 && title.Text == "Empty Project", "Browser must initially select Empty Project.");
        templates.SelectedIndex = 1;
        Arrange(view);
        Assert(title.Text == "Top-Down Project", "Selection must update template metadata.");
        var preview = (Image)view.FindName("TemplatePreview");
        Assert(preview.Source is BitmapSource bitmap && bitmap.PixelWidth > 0, "Selection must load a usable preview image.");
    }

    private static void Arrange(NewProjectView view)
    {
        view.Measure(new Size(800, 450));
        view.Arrange(new Rect(0, 0, 800, 450));
        view.UpdateLayout();
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    }

    private static void VerifyLocationValidation(NewProjectView view)
    {
        var name = (TextBox)view.FindName("ProjectNameInput");
        var parent = (TextBox)view.FindName("ProjectParentInput");
        var checkLocation = (Button)view.FindName("CheckLocationButton");
        var status = (TextBlock)view.FindName("ValidationStatus");
        parent.Text = AppContext.BaseDirectory;
        name.Text = "Bad?Name";
        checkLocation.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Arrange(view);
        Assert(status.Text.Contains("Invalid project-relative path"), "Validation errors must appear in the browser.");
        name.Text = "UiValidationGame";
        Arrange(view);
        Assert(status.Text == "", "Editing inputs must clear stale validation feedback.");
        checkLocation.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Arrange(view);
        Assert(status.Text.StartsWith("Location available:"), "A writable location must produce success feedback.");
        Assert(!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "UiValidationGame")), "Checking a location must not create a project.");
    }

    private static void SavePreview(NewProjectView view)
    {
        var preview = new RenderTargetBitmap(800, 450, 96, 96, PixelFormats.Pbgra32);
        preview.Render(view);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(preview));
        using var screenshot = File.Create(Path.Combine(AppContext.BaseDirectory, "template-browser.png"));
        encoder.Save(screenshot);
    }

    private static void VerifyProjectCreation(NewProjectView view)
    {
        var parent = Path.Combine(AppContext.BaseDirectory, $"UiCreation-{Guid.NewGuid():N}");
        try
        {
            ((TextBox)view.FindName("ProjectParentInput")).Text = parent;
            ((TextBox)view.FindName("ProjectNameInput")).Text = "CreatedFromBrowser";
            ((Button)view.FindName("CreateProjectButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var projectFile = Path.Combine(parent, "CreatedFromBrowser", "project.veris");
            Assert(File.Exists(projectFile), "Create must generate a project from the current browser inputs.");
            Assert(ProjectDocument.Parse(File.ReadAllText(projectFile)).Name == "CreatedFromBrowser", "Create must persist the entered name.");
        }
        finally
        {
            if (Directory.Exists(parent)) Directory.Delete(parent, true);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
