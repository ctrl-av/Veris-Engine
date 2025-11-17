using VerisEditor.GameProject;

var originalDirectory = Environment.CurrentDirectory;
var templateDirectory = Path.Combine(AppContext.BaseDirectory, "ProjectTemplates", "DiscoveryTest");
var unrelatedDirectory = Path.Combine(AppContext.BaseDirectory, "UnrelatedWorkingDirectory");
Directory.CreateDirectory(templateDirectory);
Directory.CreateDirectory(unrelatedDirectory);
var templateFile = Path.Combine(templateDirectory, "template.xml");
File.WriteAllText(templateFile, "<template />");

try
{
    Environment.CurrentDirectory = unrelatedDirectory;
    var newProject = new NewProject();
    if (!newProject.TemplateFiles.Contains(templateFile))
    {
        throw new InvalidOperationException("TemplateDiscovery_UsesApplicationDirectory failed.");
    }

    File.Delete(templateFile);
    if (new NewProject().TemplateFiles.Contains(templateFile))
    {
        throw new InvalidOperationException("TemplateDiscovery_RefreshesAvailableDescriptors failed.");
    }

    Console.WriteLine("PASS: Template discovery uses the application directory and refreshes descriptors.");
}
finally
{
    Environment.CurrentDirectory = originalDirectory;
    File.Delete(templateFile);
    Directory.Delete(templateDirectory);
    Directory.Delete(unrelatedDirectory);
}
