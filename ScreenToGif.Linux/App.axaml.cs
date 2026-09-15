using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ScreenToGif.Linux.Controls;

namespace ScreenToGif.Linux;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Window mainWindow = Program.StartInEditor ? CreateEditorWindow() : new StartupWindow();
            if (Program.StartInEditor)
                mainWindow.Closed += (_, _) => desktop.Shutdown();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    internal static Window CreateEditorWindow()
    {
        try
        {
            return new MainWindow();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new StartupFailureWindow();
        }
    }
}
