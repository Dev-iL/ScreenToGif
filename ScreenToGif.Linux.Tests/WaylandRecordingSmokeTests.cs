using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ScreenToGif.Linux.Models;
using ScreenToGif.Linux.Controls;
using ScreenToGif.Linux.Services;
using ScreenToGif.Linux.Services.Capture;
using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class WaylandRecordingFactAttribute : FactAttribute
{
    public WaylandRecordingFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("SCREENTOGIF_WAYLAND_RECORDING_SMOKE") != "1")
            Skip = "Opt in on an unlocked native Wayland desktop with a test-owned source and portal consent.";
    }
}

public sealed partial class DesktopWindowSmokeTests
{
    [WaylandRecordingFact]
    public async Task Native_Wayland_source_records_into_editor_and_exports_measured_delays()
    {
        Assert.Equal("wayland", Environment.GetEnvironmentVariable("XDG_SESSION_TYPE"));
        var config = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        Assert.True(config is not null && Path.GetFullPath(config).StartsWith(Path.GetTempPath(), StringComparison.Ordinal));
        var directory = Environment.GetEnvironmentVariable("SCREENTOGIF_DESKTOP_SCREENSHOT_DIR")!;
        Assert.False(string.IsNullOrWhiteSpace(directory));
        Directory.CreateDirectory(directory);
        var script = Environment.GetEnvironmentVariable("SCREENTOGIF_WAYLAND_SOURCE_SCRIPT")!;
        Assert.True(File.Exists(script), "Provide the absolute path to scripts/wayland-test-source.py.");
        var sourceStart = new ProcessStartInfo("/usr/bin/python3")
            { RedirectStandardOutput = true, RedirectStandardError = true };
        sourceStart.ArgumentList.Add(script);
        sourceStart.Environment["GDK_BACKEND"] = "wayland";
        using var source = Process.Start(sourceStart)!;
        var sourceError = source.StandardError.ReadToEndAsync();
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var uiFailure = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        Thread? uiThread = null;
        try
        {
            Assert.Equal("NATIVE_WAYLAND_SOURCE_READY", await source.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(20)));
            await LinuxSettings.UpdateAsync(settings =>
            {
                settings.StartupWindow = LinuxStartupWindow.Startup;
                settings.RecorderCaptureMode = RecorderCaptureMode.PerSecond;
                settings.RecorderFramesPerSecond = 10;
                settings.RecorderFixedFrameRate = false;
                settings.RecorderShowCursor = Environment.GetEnvironmentVariable("SCREENTOGIF_WAYLAND_CURSOR") == "1";
                settings.RecorderPreStart = false;
                settings.AskBeforeCloseEditor = false;
                settings.AskBeforeDiscardProject = false;
                settings.ShowNotificationIcon = false;
                settings.KeepOpen = true;
            });
            uiThread = new Thread(() =>
            {
                try
                {
                    AppBuilder.Configure<App>().UsePlatformDetect()
                        .AfterSetup(_ => Dispatcher.UIThread.Post(() => ready.TrySetResult()))
                        .StartWithClassicDesktopLifetime([]);
                }
                catch (Exception exception) { uiFailure.TrySetResult(exception); }
            }) { IsBackground = true };
            uiThread.Start();
            var started = await Task.WhenAny(ready.Task, uiFailure.Task).WaitAsync(TimeSpan.FromSeconds(20));
            if (started == uiFailure.Task) throw await uiFailure.Task;
            var app = await Ui(() => (App)Application.Current!);
            var recorder = await Ui(() => (RecorderWindow)app.OpenCaptureShell(CaptureShellKind.Recorder)!);
            await UntilAsync(() => recorder.IsVisible && recorder.Bounds.Width > 0);
            await CaptureAsync(recorder, "wayland-idle");
            File.WriteAllText(Path.Combine(directory, "phase.txt"), "Select only: ScreenToGif Wayland test source\n");
            await Ui(() => Click(recorder.FindControl<Button>("RecordButton")!));
            var consentWait = Stopwatch.StartNew();
            while (await Ui(() => Count(recorder)) < 6)
            {
                var problem = await Ui(() => Desktop.Windows.OfType<ConfirmDialog>().FirstOrDefault());
                if (problem is not null)
                {
                    await CaptureAsync(problem, "wayland-start-failure");
                    Assert.True(false, "Native recording failed: " + await Ui(() => recorder.FindControl<TextBlock>("WaylandStatus")!.Text));
                }
                Assert.True(consentWait.Elapsed < TimeSpan.FromMinutes(3), "Portal consent/recording did not complete; see retained phase and application evidence.");
                await Task.Delay(100);
            }
            await Ui(() => Click(recorder.FindControl<Button>("RecordButton")!));
            await UntilAsync(() => recorder.FindControl<TextBlock>("RecordButtonLabel")!.Text == "Resume");
            var beforePause = await Ui(() => Count(recorder));
            var pause = Stopwatch.StartNew();
            await Task.Delay(1000);
            Assert.Equal(beforePause, await Ui(() => Count(recorder)));
            await Ui(() => Click(recorder.FindControl<Button>("RecordButton")!));
            var pauseMs = pause.ElapsedMilliseconds;
            await UntilAsync(() => Count(recorder) >= beforePause + 4);
            await Ui(() => Click(recorder.FindControl<Button>("RecordButton")!));
            await UntilAsync(() => recorder.FindControl<TextBlock>("RecordButtonLabel")!.Text == "Resume");
            await CaptureAsync(recorder, "wayland-paused");
            var selected = await Ui(() => new PixelSize((int)recorder.FindControl<NumericUpDown>("CaptureWidthInput")!.Value!,
                (int)recorder.FindControl<NumericUpDown>("CaptureHeightInput")!.Value!));
            await Ui(() => Click(recorder.FindControl<Button>("StopButton")!));
            await UntilAsync(() => Desktop.Windows.OfType<MainWindow>().Any(window => window.FindControl<ListBox>("FrameListBox")!.ItemCount > 0));
            var editor = await Ui(() => Desktop.Windows.OfType<MainWindow>().Single());
            var list = await Ui(() => editor.FindControl<ListBox>("FrameListBox")!);
            var captured = await Ui(() => list.Items.OfType<EditorFrame>().ToArray());
            Assert.True(captured.Select(frame => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(frame.FilePath)))).Distinct().Count() > 1,
                "The native source must actually change.");
            Assert.All(captured, frame => Assert.True(frame.DelayMs < pauseMs, "Paused time entered playback delays."));
            Assert.True(HasSourceMarkers(ReadPixels(captured[0].FilePath), selected.Width),
                "The chosen native window must contain the fixture's aligned red, green and blue marker blocks.");
            foreach (var frame in captured)
            {
                using var bitmap = new Bitmap(frame.FilePath);
                Assert.Equal(selected, bitmap.PixelSize);
            }
            await ProjectArchive.SaveAsync(Path.Combine(directory, "native-captured.stg-linux"), captured);
            await CaptureAsync(editor, "wayland-editor-recorded");
            await Ui(() => Click(editor.FindControl<Button>("RibbonPlayButton")!));
            await Task.Delay(250);
            await Ui(() => Click(editor.FindControl<Button>("RibbonPlayButton")!));
            await Ui(() =>
            {
                editor.FindControl<TabControl>("RibbonTabControl")!.SelectedIndex = 3;
                list.SelectAll();
                Click(editor.GetVisualDescendants().OfType<RibbonButton>().Single(button => button.Label == "Reverse"));
            });
            await UntilAsync(() => list.Items.OfType<EditorFrame>().First().FilePath == captured[^1].FilePath &&
                                   editor.FindControl<TabControl>("RibbonTabControl")!.IsEnabled);
            var edited = await Ui(() => list.Items.OfType<EditorFrame>().ToArray());
            await ProjectArchive.SaveAsync(Path.Combine(directory, "native-edited.stg-linux"), edited);
            var ffmpeg = new FfmpegTool();
            var exported = Path.Combine(directory, "native-edited.apng");
            await new FfmpegExporter(ffmpeg).ExportAsync(edited, exported);
            using var decodedWorkspace = EditorWorkspace.Create();
            var decoded = await new MediaImporter(ffmpeg).ImportAsync([exported], decodedWorkspace);
            Assert.Equal(edited.Select(frame => frame.DelayMs), decoded.Select(frame => frame.DelayMs));
            for (var index = 0; index < edited.Length; index++)
                Assert.Equal(ReadPixels(edited[index].FilePath), ReadPixels(decoded[index].FilePath));
            await CaptureAsync(editor, "wayland-editor-edited");
            File.WriteAllText(Path.Combine(directory, "native-result.json"), JsonSerializer.Serialize(new
            {
                NativeSource = "GdkWayland.WaylandDisplay", selected.Width, selected.Height,
                Frames = captured.Length, CaptureDelaysMs = captured.Select(frame => frame.DelayMs),
                PauseMs = pauseMs, Cursor = LinuxSettings.Current.RecorderShowCursor,
                Export = "APNG decoded pixel order and millisecond delays equal edited project"
            }, new JsonSerializerOptions { WriteIndented = true }));
            await Ui(editor.Close);
            await UntilAsync(() => Desktop.Windows.OfType<StartupWindow>().Count(window => window.IsVisible) == 1);
            await Ui(app.ExitApplication);
            Assert.True(uiThread.Join(TimeSpan.FromSeconds(20)));
            File.WriteAllText(Path.Combine(directory, "phase.txt"), "PASS: native Recorder → pause/resume → Editor playback/reverse → independently decoded APNG → StartUp → Exit\n");
        }
        finally
        {
            if (uiThread is not null && uiThread.IsAlive && ready.Task.IsCompleted)
            {
                await Ui(() => Desktop.Shutdown());
                uiThread.Join(TimeSpan.FromSeconds(20));
            }
            if (!source.HasExited) source.Kill(entireProcessTree: true);
            await source.WaitForExitAsync();
            File.WriteAllText(Path.Combine(directory, "source.log"), await sourceError);
        }
    }

    private static int Count(RecorderWindow recorder)
    {
        var status = recorder.FindControl<TextBlock>("StatusLabel")!.Text ?? string.Empty;
        return int.TryParse(status.Split(' ')[0], out var count) ? count : 0;
    }

    private static bool HasSourceMarkers(byte[] pixels, int width)
    {
        for (var row = 0; row < pixels.Length / (width * 4); row++)
        {
            var starts = new List<int>();
            var lastColor = -1;
            for (var column = 0; column < width; column++)
            {
                var offset = (row * width + column) * 4;
                var color = pixels[offset + 2] > 240 && pixels[offset + 1] < 10 && pixels[offset] < 10 ? 0
                    : pixels[offset + 1] > 240 && pixels[offset + 2] < 10 && pixels[offset] < 10 ? 1
                    : pixels[offset] > 240 && pixels[offset + 1] < 10 && pixels[offset + 2] < 10 ? 2 : -1;
                if (color >= 0 && color != lastColor)
                {
                    if (color == starts.Count) starts.Add(column);
                    else if (color == 0) { starts.Clear(); starts.Add(column); }
                    lastColor = color;
                }
            }
            if (starts.Count == 3 && starts[1] > starts[0] &&
                Math.Abs((starts[2] - starts[1]) - (starts[1] - starts[0])) <= 1) return true;
        }
        return false;
    }

    private static byte[] ReadPixels(string path)
    {
        using var bitmap = new Bitmap(path);
        var bytes = new byte[bitmap.PixelSize.Width * bitmap.PixelSize.Height * 4];
        unsafe
        {
            fixed (byte* buffer = bytes)
                bitmap.CopyPixels(new PixelRect(bitmap.PixelSize), (IntPtr)buffer, bytes.Length, bitmap.PixelSize.Width * 4);
        }
        return bytes;
    }
}
