using System;
using System.Windows;

namespace P2PLauncher.Standalone
{
    /// <summary>
    /// Provides a concrete WPF entrypoint for the standalone publish.
    /// This constructs the main P2PLauncher App window via the factory and runs it.
    /// </summary>
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            var application = new Application();
            Window mainWindow = global::P2PLauncher.LauncherEntry.CreateMainWindow();
            application.Run(mainWindow);
        }
    }
}
