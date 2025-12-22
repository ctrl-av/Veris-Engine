using System.Windows;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace VerisEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            EditorLog.Initialize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Veris", "Logs"));
            DispatcherUnhandledException += OnDispatcherException;
            AppDomain.CurrentDomain.UnhandledException += (_, failure) =>
                EditorLog.Error("Fatal application error.", (Exception)failure.ExceptionObject);
            TaskScheduler.UnobservedTaskException += (_, failure) =>
            {
                EditorLog.Error("Background task failed.", failure.Exception);
                failure.SetObserved();
            };
            base.OnStartup(e);
        }

        private void OnDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            EditorLog.Error("Unhandled editor error. The editor will close.", e.Exception);
            MessageBox.Show($"The editor encountered an unexpected error and must close.\n{e.Exception.Message}\nLog: {EditorLog.FilePath}",
                "Veris Editor", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
            Shutdown(1);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            EditorLog.Info($"Editor session ended with exit code {e.ApplicationExitCode}.");
            base.OnExit(e);
        }
    }

}
