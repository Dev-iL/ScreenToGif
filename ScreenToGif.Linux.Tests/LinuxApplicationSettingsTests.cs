using ScreenToGif.Linux.Services;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class LinuxApplicationSettingsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"screentogif-settings-{Guid.NewGuid():N}");

    [Fact]
    public async Task SettingsRoundTripEverySupportedApplicationChoice()
    {
        var path = Path.Combine(_directory, "settings.json");
        var store = new LinuxApplicationSettingsStore(path);
        var expected = new LinuxApplicationSettings
        {
            SingleInstance = false,
            StartMinimized = true,
            StartupWindow = LinuxStartupWindow.Editor,
            Theme = LinuxAppTheme.FollowSystem,
            ShowNotificationIcon = true,
            KeepOpen = false,
            LeftClickAction = LinuxTrayAction.OpenWindow,
            LeftClickWindow = LinuxTrayWindow.Editor,
            DoubleLeftClickAction = LinuxTrayAction.Nothing,
            DoubleLeftClickWindow = LinuxTrayWindow.None,
            MiddleClickAction = LinuxTrayAction.RestoreWindows,
            MiddleClickWindow = LinuxTrayWindow.Startup,
            NotifyBeforeClosing = false,
            DisableHardwareAcceleration = true,
            AskBeforeDeleteFrames = false,
            AskBeforeDiscardProject = false,
            AskBeforeCloseEditor = false,
            DropFramesWhenBehind = true,
            UndoLimit = 12,
            DeleteCacheOnClose = true,
            RemoveOldProjects = false,
            ProjectRetentionDays = 14,
            FfmpegPath = "/opt/ffmpeg/bin/ffmpeg"
        };

        await store.SaveAsync(expected);
        var actual = store.Load();

        Assert.Equal(expected.SingleInstance, actual.SingleInstance);
        Assert.Equal(expected.StartMinimized, actual.StartMinimized);
        Assert.Equal(expected.StartupWindow, actual.StartupWindow);
        Assert.Equal(expected.Theme, actual.Theme);
        Assert.Equal(expected.ShowNotificationIcon, actual.ShowNotificationIcon);
        Assert.Equal(expected.KeepOpen, actual.KeepOpen);
        Assert.Equal(expected.LeftClickAction, actual.LeftClickAction);
        Assert.Equal(expected.LeftClickWindow, actual.LeftClickWindow);
        Assert.Equal(expected.DoubleLeftClickAction, actual.DoubleLeftClickAction);
        Assert.Equal(expected.DoubleLeftClickWindow, actual.DoubleLeftClickWindow);
        Assert.Equal(expected.MiddleClickAction, actual.MiddleClickAction);
        Assert.Equal(expected.MiddleClickWindow, actual.MiddleClickWindow);
        Assert.Equal(expected.NotifyBeforeClosing, actual.NotifyBeforeClosing);
        Assert.Equal(expected.DisableHardwareAcceleration, actual.DisableHardwareAcceleration);
        Assert.Equal(expected.AskBeforeDeleteFrames, actual.AskBeforeDeleteFrames);
        Assert.Equal(expected.AskBeforeDiscardProject, actual.AskBeforeDiscardProject);
        Assert.Equal(expected.AskBeforeCloseEditor, actual.AskBeforeCloseEditor);
        Assert.Equal(expected.DropFramesWhenBehind, actual.DropFramesWhenBehind);
        Assert.Equal(expected.UndoLimit, actual.UndoLimit);
        Assert.Equal(expected.DeleteCacheOnClose, actual.DeleteCacheOnClose);
        Assert.Equal(expected.RemoveOldProjects, actual.RemoveOldProjects);
        Assert.Equal(expected.ProjectRetentionDays, actual.ProjectRetentionDays);
        Assert.Equal(expected.FfmpegPath, actual.FfmpegPath);
    }

    [Fact]
    public void CorruptSettingsFallBackToSafeDefaults()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "settings.json");
        File.WriteAllText(path, "{not-json");

        var settings = new LinuxApplicationSettingsStore(path).Load();

        Assert.True(settings.SingleInstance);
        Assert.Equal(LinuxAppTheme.Dark, settings.Theme);
        Assert.Equal(LinuxStartupWindow.Startup, settings.StartupWindow);
        Assert.False(settings.StartMinimized);
    }

    [Fact]
    public async Task AutostartWritesManagedDesktopEntryAndCanRemoveIt()
    {
        var service = new LinuxAutostartService(_directory, () => "\"/opt/Screen To Gif/ScreenToGif.Linux\"");

        await service.SetEnabledAsync(true);

        Assert.True(service.IsEnabled());
        var desktopEntry = File.ReadAllText(service.DesktopFilePath);
        Assert.Contains("Exec=\"/opt/Screen To Gif/ScreenToGif.Linux\"", desktopEntry);
        Assert.Contains("X-ScreenToGif-Managed=true", desktopEntry);

        await service.SetEnabledAsync(false);

        Assert.False(File.Exists(service.DesktopFilePath));
    }

    [Fact]
    public async Task AutostartDoesNotOverwriteOrDeleteAnUnmanagedEntry()
    {
        var service = new LinuxAutostartService(_directory, () => "\"/opt/screentogif\"");
        Directory.CreateDirectory(Path.GetDirectoryName(service.DesktopFilePath)!);
        await File.WriteAllTextAsync(service.DesktopFilePath, "[Desktop Entry]\nName=User owned\n");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetEnabledAsync(true));
        await service.SetEnabledAsync(false);

        Assert.Contains("User owned", await File.ReadAllTextAsync(service.DesktopFilePath));
    }

    [Theory]
    [InlineData("/opt/Screen To Gif/app", "\"/opt/Screen To Gif/app\"")]
    [InlineData("/opt/a\\b\"c`d$e", "\"/opt/a\\\\b\\\"c\\`d\\$e\"")]
    public void DesktopArgumentsAreQuotedWithoutShellExpansion(string value, string expected)
    {
        Assert.Equal(expected, LinuxAutostartService.QuoteDesktopArgument(value));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
