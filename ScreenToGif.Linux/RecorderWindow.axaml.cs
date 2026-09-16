using Avalonia.Controls;
using Avalonia.Threading;
using ScreenToGif.Linux.Services;

namespace ScreenToGif.Linux;

public partial class RecorderWindow : Window, ICaptureShellWindow
{
    private readonly X11WindowInputRegion _inputRegion = new();
    private bool _inputRegionUpdateQueued;
    private bool _isClosed;

    public RecorderWindow()
    {
        InitializeComponent();
        Opened += RecorderOpened;
        SizeChanged += RecorderSizeChanged;
        PositionChanged += RecorderPositionChanged;
        Activated += RecorderActivated;
        Closed += RecorderClosed;
    }

    private void RecorderOpened(object? sender, EventArgs e) => QueueInputRegionUpdate();

    private void RecorderSizeChanged(object? sender, SizeChangedEventArgs e) => QueueInputRegionUpdate();

    private void RecorderPositionChanged(object? sender, PixelPointEventArgs e) => QueueInputRegionUpdate();

    private void RecorderActivated(object? sender, EventArgs e) => QueueInputRegionUpdate();

    private void RecorderClosed(object? sender, EventArgs e)
    {
        _isClosed = true;
        _inputRegion.Dispose();
    }

    private void QueueInputRegionUpdate()
    {
        ApplyInputRegion();
        if (_inputRegionUpdateQueued)
            return;

        _inputRegionUpdateQueued = true;
        Dispatcher.UIThread.Post(() =>
        {
            _inputRegionUpdateQueued = false;
            if (!_isClosed)
                ApplyInputRegion();
        }, DispatcherPriority.Background);
    }

    private void ApplyInputRegion()
    {
        var handle = TryGetPlatformHandle();
        _inputRegion.TryApply(handle?.Handle ?? IntPtr.Zero, handle?.HandleDescriptor, ClientSize, RenderScaling,
            CommandBar.Bounds.Height);
    }
}
