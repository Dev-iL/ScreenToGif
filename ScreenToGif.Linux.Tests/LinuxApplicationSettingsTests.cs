using ScreenToGif.Linux.Services;
using ScreenToGif.Linux.Services.Capture;
using System.Text.Json;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class LinuxApplicationSettingsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"screentogif-settings-{Guid.NewGuid():N}");

    [Fact]
    public async Task EveryRecorderSettingRoundTripsThroughTheSettingsFile()
    {
        var path = Path.Combine(_directory, "settings.json");
        var store = new LinuxApplicationSettingsStore(path);
        var expected = new LinuxApplicationSettings
        {
            RecorderCaptureMode = RecorderCaptureMode.PerHour,
            RecorderFramesPerSecond = 42,
            RecorderFixedFrameRate = true,
            RecorderShowCursor = false,
            RecorderPreStart = true,
            RecorderPreStartSeconds = 7,
            RecorderManualPlaybackDelayMs = 250,
            RecorderAskBeforeDiscarding = false,
            RecorderRememberSize = false,
            RecorderRememberPosition = false,
            RecorderWidth = 1280,
            RecorderHeight = 720,
            RecorderLeft = 314,
            RecorderTop = 271
        };

        await store.SaveAsync(expected);
        var actual = store.Load();

        AssertRecorderSettingsMatch(expected, actual);
    }

    [Fact]
    public void CopyCarriesEveryRecorderSetting()
    {
        // LinuxSettings.SaveAsync republishes through Copy, so a field missing here is a field
        // that round-trips through the file and is still lost in the running application.
        var expected = new LinuxApplicationSettings
        {
            RecorderCaptureMode = RecorderCaptureMode.Manual,
            RecorderFramesPerSecond = 3,
            RecorderFixedFrameRate = true,
            RecorderShowCursor = false,
            RecorderPreStart = true,
            RecorderPreStartSeconds = 9,
            RecorderManualPlaybackDelayMs = 1500,
            RecorderAskBeforeDiscarding = false,
            RecorderRememberSize = false,
            RecorderRememberPosition = false,
            RecorderWidth = 640,
            RecorderHeight = 480,
            RecorderLeft = -12,
            RecorderTop = 0
        };

        AssertRecorderSettingsMatch(expected, expected.Copy());
    }

    [Fact]
    public void ASettingsFileWrittenBeforeTheRecorderExistedLoadsWithTheWindowsDefaults()
    {
        var path = Path.Combine(_directory, "settings.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(path, """
            {
              "SingleInstance": false,
              "UndoLimit": 25,
              "FfmpegPath": "ffmpeg"
            }
            """);

        var loaded = new LinuxApplicationSettingsStore(path).Load();

        Assert.False(loaded.SingleInstance);
        Assert.Equal(25, loaded.UndoLimit);
        AssertRecorderDefaultsMirrorWindows(loaded);
    }

    [Fact]
    public void ACorruptSettingsFileFallsBackToTheRecorderDefaults()
    {
        var path = Path.Combine(_directory, "settings.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(path, "{ this is not json");

        AssertRecorderDefaultsMirrorWindows(new LinuxApplicationSettingsStore(path).Load());
    }

    /// <summary>
    /// Pins the defaults to the values the Windows recorder ships, rather than to whatever the
    /// Linux type currently declares: comparing a loaded file against a fresh instance passes
    /// however the declarations drift, which is the one thing this assertion exists to catch.
    /// </summary>
    private static void AssertRecorderDefaultsMirrorWindows(LinuxApplicationSettings actual)
    {
        Assert.Equal(RecorderCaptureMode.PerSecond, actual.RecorderCaptureMode);
        Assert.Equal(15, actual.RecorderFramesPerSecond);
        Assert.False(actual.RecorderFixedFrameRate);
        Assert.True(actual.RecorderShowCursor);
        Assert.False(actual.RecorderPreStart);
        Assert.Equal(3, actual.RecorderPreStartSeconds);
        Assert.Equal(1000, actual.RecorderManualPlaybackDelayMs);
        Assert.True(actual.RecorderAskBeforeDiscarding);
        Assert.True(actual.RecorderRememberSize);
        Assert.True(actual.RecorderRememberPosition);
        Assert.Equal(502, actual.RecorderWidth);
        Assert.Equal(203, actual.RecorderHeight);
        Assert.Null(actual.RecorderLeft);
        Assert.Null(actual.RecorderTop);
    }

    [Fact]
    public async Task AnUnplacedRecorderPersistsNoPositionRatherThanAFalseOne()
    {
        var path = Path.Combine(_directory, "settings.json");
        var store = new LinuxApplicationSettingsStore(path);

        await store.SaveAsync(new LinuxApplicationSettings());

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("RecorderLeft").ValueKind);
        Assert.Null(store.Load().RecorderLeft);
        Assert.Null(store.Load().RecorderTop);
    }

    private static void AssertRecorderSettingsMatch(LinuxApplicationSettings expected, LinuxApplicationSettings actual)
    {
        Assert.Equal(expected.RecorderCaptureMode, actual.RecorderCaptureMode);
        Assert.Equal(expected.RecorderFramesPerSecond, actual.RecorderFramesPerSecond);
        Assert.Equal(expected.RecorderFixedFrameRate, actual.RecorderFixedFrameRate);
        Assert.Equal(expected.RecorderShowCursor, actual.RecorderShowCursor);
        Assert.Equal(expected.RecorderPreStart, actual.RecorderPreStart);
        Assert.Equal(expected.RecorderPreStartSeconds, actual.RecorderPreStartSeconds);
        Assert.Equal(expected.RecorderManualPlaybackDelayMs, actual.RecorderManualPlaybackDelayMs);
        Assert.Equal(expected.RecorderAskBeforeDiscarding, actual.RecorderAskBeforeDiscarding);
        Assert.Equal(expected.RecorderRememberSize, actual.RecorderRememberSize);
        Assert.Equal(expected.RecorderRememberPosition, actual.RecorderRememberPosition);
        Assert.Equal(expected.RecorderWidth, actual.RecorderWidth);
        Assert.Equal(expected.RecorderHeight, actual.RecorderHeight);
        Assert.Equal(expected.RecorderLeft, actual.RecorderLeft);
        Assert.Equal(expected.RecorderTop, actual.RecorderTop);
    }

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
        var persisted = await File.ReadAllTextAsync(path);

        Assert.DoesNotContain("NotifyBeforeClosing", persisted, StringComparison.Ordinal);
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
