using System;
using System.Windows;

namespace P2PLauncher.Standalone
{
    /// <summary>
    /// Provides a concrete WPF entrypoint for the standalone publish.
    /// This constructs the main P2PLauncher App from the referenced project and runs it.
    /// </summary>
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            // The App class in the main project is internal; create the Application and MainWindow manually.
            var application = new Application();
            var mainWindow = new global::P2PLauncher.MainWindow();
            application.Run(mainWindow);
        }
    }
}
