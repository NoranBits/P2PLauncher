using System.Windows;

namespace P2PLauncher;

public static class LauncherEntry
{
	/// <summary>
	/// Creates the application's main window. Kept public so external hosts can start the UI
	/// without exposing internal window types.
	/// </summary>
	public static Window CreateMainWindow()
	{
		return new MainWindow();
	}
}
