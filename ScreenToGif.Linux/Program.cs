using Avalonia;
using Avalonia.X11;
using ScreenToGif.Linux.Services;

namespace ScreenToGif.Linux;

internal static class Program
{
    public static bool StartInEditor { get; private set; }
    public static bool StartInOptions { get; private set; }
    public static bool StartInWebcam { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        LinuxSettings.ApplyEnvironment();
        if (LinuxSettings.Current.RemoveOldProjects)
            ProjectArchive.ScavengeStaleWorkspaces(LinuxSettings.Current.ProjectRetentionDays);
        if (args.Any(arg => arg is "--help" or "-h"))
        {
            Console.WriteLine("ScreenToGif for Linux");
            Console.WriteLine("Record your webcam straight into the editor, configure the application, or edit and export media.");
            Console.WriteLine("Recorder and Board remain navigable previews.");
            Console.WriteLine();
            Console.WriteLine("Usage: dotnet run --project ScreenToGif.Linux [--editor | --options | --webcam] [--new-instance]");
            Console.WriteLine();
            Console.WriteLine("  --webcam   Open the webcam recorder instead of the StartUp window.");
            return;
        }

        StartInEditor = args.Any(arg => arg == "--editor");
        StartInOptions = args.Any(arg => arg == "--options");
        StartInWebcam = args.Any(arg => arg == "--webcam");

        var allowNewInstance = args.Any(arg => arg == "--new-instance");
        using var instance = SingleInstanceGuard.TryAcquire(LinuxSettings.Current.SingleInstance && !allowNewInstance);
        if (instance is null)
            return;

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>().UsePlatformDetect();
        if (LinuxSettings.Current.DisableHardwareAcceleration)
            builder.With(new X11PlatformOptions { RenderingMode = [X11RenderingMode.Software] });
        return builder.LogToTrace();
    }
}
