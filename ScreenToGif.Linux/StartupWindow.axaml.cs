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
        _captureShellCoordinator.Open(CaptureShellKind.Recorder);

    private void OpenWebcamClick(object? sender, RoutedEventArgs e) =>
        _captureShellCoordinator.Open(CaptureShellKind.Webcam);

    private void OpenBoardClick(object? sender, RoutedEventArgs e) =>
        _captureShellCoordinator.Open(CaptureShellKind.Board);

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

    private void OpenOptionsClick(object? sender, RoutedEventArgs e) => App.CurrentApp?.ShowOptions(this);

    public ICaptureShellWindow CreateShell(CaptureShellKind kind) => kind switch
    {
        CaptureShellKind.Recorder => new RecorderWindow(),
        CaptureShellKind.Webcam => new WebcamWindow(),
        CaptureShellKind.Board => new BoardWindow(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    public void HideStartup() => Hide();

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
