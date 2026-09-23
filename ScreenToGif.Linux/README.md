# ScreenToGif for Linux

This is the Avalonia-based Linux application. It records a region of the screen, a webcam, or a drawing on the Board, edits the result on a timeline, and exports it. The original `GifRecorder.sln` remains the Windows application.

Detailed behavior and limits are documented for the [Recorder](docs/recorder.md), the [Webcam recorder](docs/webcam.md), the [Board](docs/board.md), and each editor ribbon tab in [`docs/editor`](docs/editor/).

## Requirements

To build from source, install:

- a 64-bit Linux desktop session (X11 or Wayland);
- the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or newer;
- [FFmpeg](https://ffmpeg.org/), including both `ffmpeg` and `ffprobe` on `PATH`.

On Ubuntu, once the Microsoft .NET package feed is configured, the usual development dependencies are:

```bash
sudo apt update
sudo apt install dotnet-sdk-9.0 ffmpeg
```

Most desktop distributions already include the native libraries Avalonia needs. For a minimal installation, install the distribution equivalents of `fontconfig`, `freetype`, `libX11`, `libX11-xcb`, `libXrender`, `libICE`, and `libSM` as well.

FFmpeg is required at runtime for animated-image/video import and for every export. MP4 and WebM export additionally require an FFmpeg build containing the `libx264` and `libvpx-vp9` encoders. Check the setup with:

```bash
dotnet --version
ffmpeg -version
ffprobe -version
ffmpeg -hide_banner -encoders | grep -E 'libx264|libvpx-vp9'
```

If FFmpeg is installed outside `PATH`, point the app at it explicitly:

```bash
export SCREENTOGIF_FFMPEG=/path/to/ffmpeg
export SCREENTOGIF_FFPROBE=/path/to/ffprobe
```

## Build and run

Run these commands from the repository root:

```bash
dotnet restore ScreenToGif.Linux.sln
dotnet build ScreenToGif.Linux.sln --no-restore
dotnet test ScreenToGif.Linux.Tests/ScreenToGif.Linux.Tests.csproj --no-restore
dotnet run --project ScreenToGif.Linux/ScreenToGif.Linux.csproj --no-build
```

The default launch opens the StartUp window. Its Recorder, Webcam, Board, and Editor destinations are reachable, and the Options button opens the Linux application settings. To open the editor, webcam recorder, or Board directly:

```bash
dotnet run --project ScreenToGif.Linux/ScreenToGif.Linux.csproj --no-build -- --editor
dotnet run --project ScreenToGif.Linux/ScreenToGif.Linux.csproj --no-build -- --webcam
dotnet run --project ScreenToGif.Linux/ScreenToGif.Linux.csproj --no-build -- --board
```

### Make shortcuts

From this directory, the included Makefile provides the same workflow:

```bash
cd ScreenToGif.Linux
make build
make test
make run
make editor
```

Set `CONFIGURATION=Release` on any target when needed, for example `make publish CONFIGURATION=Release`. The published files are written to `artifacts/linux/<configuration>` at the repository root.

## Install a local desktop launcher

Publish the application and install its per-user desktop entry:

```bash
cd ScreenToGif.Linux
make install-desktop CONFIGURATION=Release
```

This copies the icon and launcher to `~/.local/share`, so no root access is needed. Launch **ScreenToGif** from the applications menu afterwards. To install an already-built executable instead, run:

```bash
./scripts/install-desktop-entry.sh /absolute/path/to/ScreenToGif.Linux
```

## Current scope

The Recorder captures a rectangle of the screen at a chosen frame rate, or one frame at a time, and hands the result to the Editor. It needs an X11 session: under a native Wayland session Record is disabled and says why. Window snapping, user-interaction capture, cursor following, guidelines, and desktop-wide hotkeys are not available; [`docs/recorder.md`](docs/recorder.md) lists each unavailable control beside the subsystem it needs, and [`docs/recorder-followups.md`](docs/recorder-followups.md) describes what each would take.

The Webcam recorder discovers the machine's cameras through V4L2, previews the selected one, and records it into the Editor. It needs read access to a `/dev/video*` node, which on most distributions means membership in the `video` group. See [`docs/webcam.md`](docs/webcam.md).

The Editor imports still images, animated GIF/APNG files, and common video formats; supports frame selection, reordering, deletion, and timing changes; saves self-contained `.stg-linux` projects; and exports GIF, APNG, MP4, and WebM. Options provides the Linux application settings.

The Board draws with a pen, a point eraser and a stroke eraser, records frames while you draw, and hands them to the Editor from the Startup window, configured startup or tray action, and the Editor's New and Insert groups. Insert lets you choose where the recording enters the timeline. Stroke selection is not available on Linux. [`docs/board.md`](docs/board.md) describes the workflow and every place the Board departs from its Windows counterpart.

The Linux application does not support Windows `.stg` compatibility or the Windows editor's advanced annotation/effects commands.
