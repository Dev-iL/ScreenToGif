using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Styling;
using ScreenToGif.Linux.Services;
using System.Diagnostics;

namespace ScreenToGif.Linux;

public partial class OptionsWindow : Window
{
    private readonly LinuxAutostartService _autostart = new();
    private readonly LinuxApplicationSettings _settings = LinuxSettings.Current.Copy();
    private bool _ready;
    private bool _saving;
    private bool _allowClose;

    public OptionsWindow()
    {
        InitializeComponent();
        LoadSettings();
        LoadStorageStatus();
        Closing += WindowClosing;
        _ready = true;
        UpdateDependencies();
    }

    private void LoadSettings()
    {
        StartManuallyRadio.IsChecked = !_autostart.IsEnabled();
        StartAutomaticallyRadio.IsChecked = !StartManuallyRadio.IsChecked;
        SingleInstanceRadio.IsChecked = _settings.SingleInstance;
        MultipleInstancesRadio.IsChecked = !_settings.SingleInstance;
        StartMinimizedCheckBox.IsChecked = _settings.StartMinimized;
        StartupWindowComboBox.SelectedIndex = (int)_settings.StartupWindow;
        ThemeComboBox.SelectedIndex = (int)_settings.Theme;
        ShowTrayIconCheckBox.IsChecked = _settings.ShowNotificationIcon;
        KeepOpenCheckBox.IsChecked = _settings.KeepOpen;
        LeftActionComboBox.SelectedIndex = (int)_settings.LeftClickAction;
        LeftWindowComboBox.SelectedIndex = (int)_settings.LeftClickWindow;
        DoubleLeftActionComboBox.SelectedIndex = (int)_settings.DoubleLeftClickAction;
        DoubleLeftWindowComboBox.SelectedIndex = (int)_settings.DoubleLeftClickWindow;
        MiddleActionComboBox.SelectedIndex = (int)_settings.MiddleClickAction;
        MiddleWindowComboBox.SelectedIndex = (int)_settings.MiddleClickWindow;
        NotifyBeforeClosingCheckBox.IsChecked = _settings.NotifyBeforeClosing;
        DisableHardwareAccelerationCheckBox.IsChecked = _settings.DisableHardwareAcceleration;
        AskBeforeDeleteFramesCheckBox.IsChecked = _settings.AskBeforeDeleteFrames;
        AskBeforeDiscardProjectCheckBox.IsChecked = _settings.AskBeforeDiscardProject;
        AskBeforeCloseEditorCheckBox.IsChecked = _settings.AskBeforeCloseEditor;
        DropFramesSettingCheckBox.IsChecked = _settings.DropFramesWhenBehind;
        LimitUndoCheckBox.IsChecked = _settings.UndoLimit > 0;
        UndoLimitUpDown.Value = Math.Clamp(_settings.UndoLimit, 1, 100);
        DeleteCacheOnCloseCheckBox.IsChecked = _settings.DeleteCacheOnClose;
        RemoveOldProjectsCheckBox.IsChecked = _settings.RemoveOldProjects;
        RetentionDaysUpDown.Value = Math.Clamp(_settings.ProjectRetentionDays, 1, 30);
        FfmpegPathTextBox.Text = _settings.FfmpegPath;
    }

    private void SectionSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_ready)
            return;

        if (SectionsList.SelectedItem is not ListBoxItem item || item.Tag is not string section)
            return;

        var pages = new Dictionary<string, Control>(StringComparer.Ordinal)
        {
            ["Application"] = ApplicationPage,
            ["Recorder"] = RecorderPage,
            ["Editor"] = EditorPage,
            ["Tasks"] = TasksPage,
            ["Shortcuts"] = ShortcutsPage,
            ["Language"] = LanguagePage,
            ["Storage"] = StoragePage,
            ["Cloud"] = CloudPage,
            ["Extras"] = ExtrasPage,
            ["Donate"] = DonatePage,
            ["About"] = AboutPage
        };
        foreach (var page in pages.Values)
            page.IsVisible = false;
        UnavailablePage.IsVisible = false;

        if (pages.TryGetValue(section, out var selectedPage))
            selectedPage.IsVisible = true;
        else
        {
            UnavailablePage.IsVisible = true;
            UnavailableTitle.Text = section;
            UnavailableMessage.Text = "This settings page is unavailable.";
        }
    }

    private void StartupModeChanged(object? sender, RoutedEventArgs e)
    {
        if (_ready)
            StatusText.Text = "Startup mode will take effect after you press Ok.";
    }

    private void StartupDependencyChanged(object? sender, RoutedEventArgs e)
    {
        if (StartMinimizedCheckBox.IsChecked == true)
            ShowTrayIconCheckBox.IsChecked = true;
        UpdateDependencies();
    }

    private void StartupWindowChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_ready || StartupWindowComboBox.SelectedIndex is 0 or 4)
            return;

        StartupWindowComboBox.SelectedIndex = 0;
    }

    private void TrayDependencyChanged(object? sender, RoutedEventArgs e)
    {
        if (ShowTrayIconCheckBox.IsChecked != true)
            StartMinimizedCheckBox.IsChecked = false;
        UpdateDependencies();
    }

    private void TrayActionChanged(object? sender, SelectionChangedEventArgs e) => UpdateDependencies();

    private void UpdateDependencies()
    {
        if (KeepOpenCheckBox is null || TrayInteractionsGrid is null)
            return;

        var trayEnabled = ShowTrayIconCheckBox.IsChecked == true;
        KeepOpenCheckBox.IsEnabled = trayEnabled;
        TrayInteractionsGrid.IsEnabled = trayEnabled;
        StartupWindowComboBox.IsEnabled = StartMinimizedCheckBox.IsChecked != true;

        var opensWindow = LeftActionComboBox.SelectedIndex == (int)LinuxTrayAction.OpenWindow;
        LeftWindowLabel.Text = opensWindow ? "Window:" : "Or else, opens:";
        LeftWindowComboBox.IsEnabled = trayEnabled;
    }

    private void ThemeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_ready || ThemeComboBox.SelectedIndex < 0)
            return;

        App.ApplyTheme((LinuxAppTheme)ThemeComboBox.SelectedIndex);
    }

    private void ReadSettings()
    {
        _settings.SingleInstance = SingleInstanceRadio.IsChecked == true;
        _settings.StartMinimized = StartMinimizedCheckBox.IsChecked == true;
        _settings.StartupWindow = (LinuxStartupWindow)Math.Max(0, StartupWindowComboBox.SelectedIndex);
        _settings.Theme = (LinuxAppTheme)Math.Max(0, ThemeComboBox.SelectedIndex);
        _settings.ShowNotificationIcon = ShowTrayIconCheckBox.IsChecked == true;
        _settings.KeepOpen = KeepOpenCheckBox.IsChecked == true;
        _settings.LeftClickAction = (LinuxTrayAction)Math.Max(0, LeftActionComboBox.SelectedIndex);
        _settings.LeftClickWindow = (LinuxTrayWindow)Math.Max(0, LeftWindowComboBox.SelectedIndex);
        _settings.DoubleLeftClickAction = (LinuxTrayAction)Math.Max(0, DoubleLeftActionComboBox.SelectedIndex);
        _settings.DoubleLeftClickWindow = (LinuxTrayWindow)Math.Max(0, DoubleLeftWindowComboBox.SelectedIndex);
        _settings.MiddleClickAction = (LinuxTrayAction)Math.Max(0, MiddleActionComboBox.SelectedIndex);
        _settings.MiddleClickWindow = (LinuxTrayWindow)Math.Max(0, MiddleWindowComboBox.SelectedIndex);
        _settings.NotifyBeforeClosing = NotifyBeforeClosingCheckBox.IsChecked == true;
        _settings.DisableHardwareAcceleration = DisableHardwareAccelerationCheckBox.IsChecked == true;
        _settings.AskBeforeDeleteFrames = AskBeforeDeleteFramesCheckBox.IsChecked == true;
        _settings.AskBeforeDiscardProject = AskBeforeDiscardProjectCheckBox.IsChecked == true;
        _settings.AskBeforeCloseEditor = AskBeforeCloseEditorCheckBox.IsChecked == true;
        _settings.DropFramesWhenBehind = DropFramesSettingCheckBox.IsChecked == true;
        _settings.UndoLimit = LimitUndoCheckBox.IsChecked == true ? (int)(UndoLimitUpDown.Value ?? 50) : 0;
        _settings.DeleteCacheOnClose = DeleteCacheOnCloseCheckBox.IsChecked == true;
        _settings.RemoveOldProjects = RemoveOldProjectsCheckBox.IsChecked == true;
        _settings.ProjectRetentionDays = (int)(RetentionDaysUpDown.Value ?? 5);
        _settings.FfmpegPath = string.IsNullOrWhiteSpace(FfmpegPathTextBox.Text) ? "ffmpeg" : FfmpegPathTextBox.Text.Trim();
    }

    private void LoadStorageStatus()
    {
        var cachePath = Path.Combine(Path.GetTempPath(), "screentogif-linux");
        CachePathTextBox.Text = cachePath;
        SettingsPathText.Text = new LinuxApplicationSettingsStore().Path;
        try
        {
            var root = Path.GetPathRoot(cachePath);
            if (string.IsNullOrWhiteSpace(root))
                return;
            var drive = new DriveInfo(root);
            var used = drive.TotalSize - drive.AvailableFreeSpace;
            StorageVolumeText.Text = $"{drive.Name}     {FormatBytes(drive.AvailableFreeSpace)} free of {FormatBytes(drive.TotalSize)}";
            StorageProgressBar.Value = drive.TotalSize == 0 ? 0 : used * 100d / drive.TotalSize;
            var workspaceCount = Directory.Exists(cachePath) ? Directory.EnumerateDirectories(cachePath).Count() : 0;
            StorageUsageText.Text = $"{workspaceCount} temporary project folder(s)";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            StorageVolumeText.Text = "Storage status unavailable";
            StorageUsageText.Text = ex.Message;
        }
    }

    private async void TestFfmpegClick(object? sender, RoutedEventArgs e)
    {
        var executable = string.IsNullOrWhiteSpace(FfmpegPathTextBox.Text) ? "ffmpeg" : FfmpegPathTextBox.Text.Trim();
        FfmpegStatusText.Text = "Checking...";
        try
        {
            var info = new ProcessStartInfo
            {
                FileName = executable,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            info.ArgumentList.Add("-version");
            using var process = Process.Start(info) ?? throw new InvalidOperationException("FFmpeg could not be started.");
            await process.WaitForExitAsync();
            FfmpegStatusText.Text = process.ExitCode == 0 ? "Available and ready" : $"Exited with code {process.ExitCode}";
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            FfmpegStatusText.Text = ex.Message;
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return $"{value:0.#} {units[unit]}";
    }

    private void OkClick(object? sender, RoutedEventArgs e) => Close();

    private async void WindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose)
            return;

        e.Cancel = true;
        if (_saving)
            return;

        _saving = true;
        StatusText.Text = "Saving settings...";
        try
        {
            ReadSettings();
            await _autostart.SetEnabledAsync(StartAutomaticallyRadio.IsChecked == true);
            await LinuxSettings.SaveAsync(_settings);
            App.ApplyTheme(_settings.Theme);
            App.CurrentApp?.RefreshTrayIcon();
            _allowClose = true;
            Close();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            StatusText.Text = $"Settings were not saved: {ex.Message}";
        }
        finally
        {
            _saving = false;
        }
    }
}
