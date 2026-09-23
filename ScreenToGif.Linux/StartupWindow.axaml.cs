using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using ScreenToGif.Linux.Services;

namespace ScreenToGif.Linux;

public partial class StartupWindow : Window, ICaptureShellHost
{
    private readonly CaptureShellCoordinator _captureShellCoordinator;
    private bool _openingEditor;
    private bool _isClosed;

    public StartupWindow()
    {
        InitializeComponent();
        _captureShellCoordinator = new CaptureShellCoordinator(this);
        Closed += StartupClosed;
    }

    private void OpenRecorderClick(object? sender, RoutedEventArgs e) =>
        OpenShell(CaptureShellKind.Recorder, "Recorder");

    private void OpenWebcamClick(object? sender, RoutedEventArgs e) =>
        OpenShell(CaptureShellKind.Webcam, "Webcam");

    private void OpenBoardClick(object? sender, RoutedEventArgs e) =>
        OpenShell(CaptureShellKind.Board, "Board");

    /// <summary>
    /// Opens a capture shell, saying so when it cannot open. Without this the coordinator's
    /// failure reaches the dispatcher and the user sees a button that does nothing.
    /// </summary>
    private async void OpenShell(CaptureShellKind kind, string name)
    {
        try
        {
            _captureShellCoordinator.Open(kind);
        }
        catch (Exception exception)
        {
            await new Controls.ConfirmDialog(
                $"The {name} could not open",
                exception.Message,
                "Close").ShowForAsync(this);
        }
    }

    private void OpenEditorClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        _openingEditor = true;
        var editor = App.CreateEditorWindow();
        editor.Closed += (_, _) => App.CurrentApp?.HandleWindowClosed();
        desktop.MainWindow = editor;
        editor.Show();
        Close();
    }

    private void OpenOptionsClick(object? sender, RoutedEventArgs e) => _ = App.CurrentApp?.ShowOptions(this);

    public ICaptureShellWindow CreateShell(CaptureShellKind kind) => kind switch
    {
        CaptureShellKind.Recorder => new RecorderWindow(),
        CaptureShellKind.Webcam => new WebcamWindow(),
        CaptureShellKind.Board => new BoardWindow(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    public void HideStartup() => Hide();

    public void CloseStartup()
    {
        if (_isClosed)
            return;

        // The editor is the application's window now, so Startup going away must not end the run.
        _openingEditor = true;
        Close();
    }

    public void RestoreStartup()
    {
        if (_isClosed)
            return;

        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void StartupClosed(object? sender, EventArgs e)
    {
        _isClosed = true;
        if (!_openingEditor)
            App.CurrentApp?.HandleWindowClosed();
    }
}
