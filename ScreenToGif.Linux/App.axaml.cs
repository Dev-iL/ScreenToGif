using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Styling;
using ScreenToGif.Linux.Controls;
using ScreenToGif.Linux.Services;

namespace ScreenToGif.Linux;

public partial class App : Application
{
    private TrayIcon? _trayIcon;
    private TrayIcons? _trayIcons;
    private bool _exiting;

    internal static App? CurrentApp => Current as App;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        ApplyTheme(LinuxSettings.Current.Theme);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.Exit += (_, _) =>
            {
                if (LinuxSettings.Current.DeleteCacheOnClose)
                    ProjectArchive.ScavengeStaleWorkspaces(retentionDays: 0);
            };
            var startInEditor = Program.StartInEditor || LinuxSettings.Current.StartupWindow == LinuxStartupWindow.Editor;
            Window mainWindow = Program.StartInOptions
                ? new OptionsWindow()
                : startInEditor ? CreateEditorWindow() : new StartupWindow();
            mainWindow.Closed += (_, _) => HandleWindowClosed();
            desktop.MainWindow = mainWindow;
            RefreshTrayIcon();

            if (LinuxSettings.Current.StartMinimized && LinuxSettings.Current.ShowNotificationIcon)
                mainWindow.WindowState = WindowState.Minimized;
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

    internal static void ApplyTheme(LinuxAppTheme theme)
    {
        if (Current is null)
            return;

        Current.RequestedThemeVariant = theme switch
        {
            LinuxAppTheme.Dark => ThemeVariant.Dark,
            LinuxAppTheme.FollowSystem => ThemeVariant.Default,
            _ => ThemeVariant.Light
        };
    }

    /// <summary>
    /// Shows or hides the notification icon to match the settings. The icon is created once and
    /// only hidden afterwards: disposing Avalonia's D-Bus tray icon cancels a watch it never
    /// observes, and the unhandled cancellation ends the process, taking every open window and any
    /// recording in progress with it.
    /// </summary>
    internal void RefreshTrayIcon()
    {
        var show = LinuxSettings.Current.ShowNotificationIcon;
        if (_trayIcon is not null)
        {
            _trayIcon.IsVisible = show;
            return;
        }

        if (!show)
            return;

        var menu = new NativeMenu();
        menu.Add(CreateMenuItem("Startup window", (_, _) => OpenWindow(LinuxTrayWindow.Startup)));
        menu.Add(CreateMenuItem("Editor", (_, _) => OpenWindow(LinuxTrayWindow.Editor)));
        menu.Add(CreateMenuItem("Options", (_, _) => _ = ShowOptions()));
        menu.Add(new NativeMenuItemSeparator());
        menu.Add(CreateMenuItem("Exit", (_, _) => ExitApplication()));

        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://ScreenToGif.Linux/Resources/Logo.ico"))),
            ToolTipText = "ScreenToGif",
            IsVisible = true,
            Menu = menu
        };
        _trayIcon.Clicked += (_, _) => RunTrayAction(LinuxSettings.Current.LeftClickAction, LinuxSettings.Current.LeftClickWindow);
        _trayIcons = [_trayIcon];
        TrayIcon.SetIcons(this, _trayIcons);
    }

    /// <summary>
    /// Opens Options, on <paramref name="section"/>. The returned task completes when a dialog the
    /// caller owns closes, so a caller whose own fields mirror those settings can follow the edit;
    /// it completes at once when Options is shown without an owner or was already open.
    /// </summary>
    internal Task ShowOptions(Window? owner = null, OptionsSection section = OptionsSection.Application)
    {
        var existing = Desktop?.Windows.OfType<OptionsWindow>().FirstOrDefault();
        if (existing is not null)
        {
            existing.ShowSection(section);
            Restore(existing);
            return Task.CompletedTask;
        }

        var options = new OptionsWindow(section);
        if (owner is not null)
            return options.ShowDialog(owner);

        options.Show();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Makes a window the application's main window, for a window that opened from a capture shell
    /// rather than from Startup.
    /// </summary>
    internal void AdoptEditor(Window editor)
    {
        if (Desktop is not null)
            Desktop.MainWindow = editor;
    }

    internal void HandleWindowClosed()
    {
        if (_exiting || Desktop is null)
            return;

        var hasOpenWindows = Desktop.Windows.Any(window => window.IsVisible);
        if (!hasOpenWindows && !(LinuxSettings.Current.ShowNotificationIcon && LinuxSettings.Current.KeepOpen))
            Desktop.Shutdown();
    }

    private IClassicDesktopStyleApplicationLifetime? Desktop => ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

    private static NativeMenuItem CreateMenuItem(string header, EventHandler handler)
    {
        var item = new NativeMenuItem(header);
        item.Click += handler;
        return item;
    }

    private void RunTrayAction(LinuxTrayAction action, LinuxTrayWindow fallbackWindow)
    {
        var windows = Desktop?.Windows.Where(window => window is not OptionsWindow).ToArray() ?? [];
        switch (action)
        {
            case LinuxTrayAction.Nothing:
                return;
            case LinuxTrayAction.OpenWindow:
                OpenWindow(fallbackWindow);
                return;
            case LinuxTrayAction.ToggleWindows:
                if (windows.Length == 0)
                {
                    OpenWindow(fallbackWindow == LinuxTrayWindow.None ? LinuxTrayWindow.Startup : fallbackWindow);
                    return;
                }

                var minimize = windows.Any(window => window.WindowState != WindowState.Minimized && window.IsVisible);
                foreach (var window in windows)
                    window.WindowState = minimize ? WindowState.Minimized : WindowState.Normal;
                if (!minimize)
                    windows[0].Activate();
                return;
            case LinuxTrayAction.MinimizeWindows:
                foreach (var window in windows)
                    window.WindowState = WindowState.Minimized;
                return;
            case LinuxTrayAction.RestoreWindows:
                if (windows.Length == 0)
                    OpenWindow(fallbackWindow == LinuxTrayWindow.None ? LinuxTrayWindow.Startup : fallbackWindow);
                else
                    foreach (var window in windows)
                        Restore(window);
                return;
        }
    }

    private void OpenWindow(LinuxTrayWindow target)
    {
        if (Desktop is null)
            return;

        var wantsEditor = target == LinuxTrayWindow.Editor;
        Window? existing = wantsEditor
            ? Desktop.Windows.OfType<MainWindow>().FirstOrDefault()
            : Desktop.Windows.OfType<StartupWindow>().FirstOrDefault();
        if (existing is not null)
        {
            Restore(existing);
            return;
        }

        Window window = wantsEditor ? CreateEditorWindow() : (Window)new StartupWindow();
        window.Closed += (_, _) => HandleWindowClosed();
        Desktop.MainWindow = window;
        window.Show();
    }

    private void ExitApplication()
    {
        _exiting = true;
        _trayIcon?.Dispose();
        Desktop?.Shutdown();
    }

    private static void Restore(Window window)
    {
        window.Show();
        window.WindowState = WindowState.Normal;
        window.Activate();
    }
}
