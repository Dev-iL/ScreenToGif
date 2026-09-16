using Avalonia.Controls;
using ScreenToGif.Linux.Services;

namespace ScreenToGif.Linux;

public partial class WebcamWindow : Window, ICaptureShellWindow
{
    public WebcamWindow() => InitializeComponent();
}
