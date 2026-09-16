using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;

namespace ScreenToGif.Linux;

public partial class StartupWindow : Window
{
    private bool _openingEditor;
    private bool _allowClose;
    private bool _closePromptOpen;

    public StartupWindow()
    {
        InitializeComponent();
        Closing += StartupClosing;
        Closed += StartupClosed;
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

    private void OpenOptionsClick(object? sender, RoutedEventArgs e) => App.CurrentApp?.ShowOptions(this);

    private async void StartupClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_openingEditor || _allowClose || !Services.LinuxSettings.Current.NotifyBeforeClosing)
            return;

        e.Cancel = true;
        if (_closePromptOpen)
            return;

        _closePromptOpen = true;
        try
        {
            var confirmed = await new Controls.ConfirmDialog(
                "Close ScreenToGif",
                "Close ScreenToGif?",
                "Close").ShowForAsync(this);
            if (!confirmed)
                return;
            _allowClose = true;
            Close();
        }
        finally
        {
            _closePromptOpen = false;
        }
    }

    private void StartupClosed(object? sender, EventArgs e)
    {
        if (!_openingEditor)
            App.CurrentApp?.HandleWindowClosed();
    }
}
