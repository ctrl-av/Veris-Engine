using VerisEditor.Build;

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: ExportProject <project.veris> <new-output-directory> <Release-runtime-directory>");
    return 1;
}
try { Console.WriteLine(ProjectExporter.Export(args[0], args[1], args[2])); return 0; }
catch (Exception exception) { Console.Error.WriteLine(exception.Message); return 1; }
