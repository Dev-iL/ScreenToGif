using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScreenToGif.Linux.Services;

public enum LinuxStartupWindow
{
    Startup,
    Recorder,
    Webcam,
    Board,
    Editor
}

public enum LinuxAppTheme
{
    Light,
    Medium,
    Dark,
    FollowSystem
}

public enum LinuxTrayAction
{
    Nothing,
    OpenWindow,
    ToggleWindows,
    MinimizeWindows,
    RestoreWindows
}

public enum LinuxTrayWindow
{
    None,
    Startup,
    Recorder,
    Webcam,
    Board,
    Editor
}

public sealed class LinuxApplicationSettings
{
    public bool SingleInstance { get; set; } = true;
    public bool StartMinimized { get; set; }
    public LinuxStartupWindow StartupWindow { get; set; } = LinuxStartupWindow.Startup;
    public LinuxAppTheme Theme { get; set; } = LinuxAppTheme.Dark;
    public bool ShowNotificationIcon { get; set; }
    public bool KeepOpen { get; set; } = true;
    public LinuxTrayAction LeftClickAction { get; set; } = LinuxTrayAction.ToggleWindows;
    public LinuxTrayWindow LeftClickWindow { get; set; } = LinuxTrayWindow.Startup;
    public LinuxTrayAction DoubleLeftClickAction { get; set; } = LinuxTrayAction.OpenWindow;
    public LinuxTrayWindow DoubleLeftClickWindow { get; set; } = LinuxTrayWindow.Editor;
    public LinuxTrayAction MiddleClickAction { get; set; } = LinuxTrayAction.MinimizeWindows;
    public LinuxTrayWindow MiddleClickWindow { get; set; } = LinuxTrayWindow.None;
    public bool NotifyBeforeClosing { get; set; } = true;
    public bool DisableHardwareAcceleration { get; set; }
    public bool AskBeforeDeleteFrames { get; set; } = true;
    public bool AskBeforeDiscardProject { get; set; } = true;
    public bool AskBeforeCloseEditor { get; set; } = true;
    public bool DropFramesWhenBehind { get; set; }
    public int UndoLimit { get; set; } = 50;
    public bool DeleteCacheOnClose { get; set; }
    public bool RemoveOldProjects { get; set; } = true;
    public int ProjectRetentionDays { get; set; } = 5;
    public string FfmpegPath { get; set; } = "ffmpeg";

    public LinuxApplicationSettings Copy() => new()
    {
        SingleInstance = SingleInstance,
        StartMinimized = StartMinimized,
        StartupWindow = StartupWindow,
        Theme = Theme,
        ShowNotificationIcon = ShowNotificationIcon,
        KeepOpen = KeepOpen,
        LeftClickAction = LeftClickAction,
        LeftClickWindow = LeftClickWindow,
        DoubleLeftClickAction = DoubleLeftClickAction,
        DoubleLeftClickWindow = DoubleLeftClickWindow,
        MiddleClickAction = MiddleClickAction,
        MiddleClickWindow = MiddleClickWindow,
        NotifyBeforeClosing = NotifyBeforeClosing,
        DisableHardwareAcceleration = DisableHardwareAcceleration,
        AskBeforeDeleteFrames = AskBeforeDeleteFrames,
        AskBeforeDiscardProject = AskBeforeDiscardProject,
        AskBeforeCloseEditor = AskBeforeCloseEditor,
        DropFramesWhenBehind = DropFramesWhenBehind,
        UndoLimit = UndoLimit,
        DeleteCacheOnClose = DeleteCacheOnClose,
        RemoveOldProjects = RemoveOldProjects,
        ProjectRetentionDays = ProjectRetentionDays,
        FfmpegPath = FfmpegPath
    };
}

public sealed class LinuxApplicationSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public LinuxApplicationSettingsStore(string? path = null)
    {
        Path = path ?? GetDefaultPath();
    }

    public string Path { get; }

    public LinuxApplicationSettings Load()
    {
        try
        {
            if (!File.Exists(Path))
                return new LinuxApplicationSettings();

            return JsonSerializer.Deserialize<LinuxApplicationSettings>(File.ReadAllText(Path), JsonOptions)
                   ?? new LinuxApplicationSettings();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new LinuxApplicationSettings();
        }
    }

    public async Task SaveAsync(LinuxApplicationSettings settings, CancellationToken cancellationToken = default)
    {
        var directory = System.IO.Path.GetDirectoryName(Path)
                        ?? throw new InvalidOperationException("The settings path has no parent directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = Path + ".tmp";
        try
        {
            await File.WriteAllTextAsync(
                temporaryPath,
                JsonSerializer.Serialize(settings, JsonOptions),
                cancellationToken);
            File.Move(temporaryPath, Path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private static string GetDefaultPath()
    {
        var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (string.IsNullOrWhiteSpace(configHome))
            configHome = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        return System.IO.Path.Combine(configHome, "ScreenToGif", "settings.json");
    }
}

public static class LinuxSettings
{
    private static readonly LinuxApplicationSettingsStore Store = new();

    public static LinuxApplicationSettings Current { get; private set; } = Store.Load();

    public static async Task SaveAsync(LinuxApplicationSettings settings, CancellationToken cancellationToken = default)
    {
        await Store.SaveAsync(settings, cancellationToken);
        Current = settings.Copy();
        ApplyEnvironment();
    }

    public static void ApplyEnvironment()
    {
        var ffmpeg = string.IsNullOrWhiteSpace(Current.FfmpegPath) ? "ffmpeg" : Current.FfmpegPath.Trim();
        Environment.SetEnvironmentVariable("SCREENTOGIF_FFMPEG", ffmpeg);
        if (Path.IsPathRooted(ffmpeg))
        {
            var directory = Path.GetDirectoryName(ffmpeg);
            if (!string.IsNullOrWhiteSpace(directory))
                Environment.SetEnvironmentVariable("SCREENTOGIF_FFPROBE", Path.Combine(directory, "ffprobe"));
        }
        else
            Environment.SetEnvironmentVariable("SCREENTOGIF_FFPROBE", "ffprobe");
    }
}
