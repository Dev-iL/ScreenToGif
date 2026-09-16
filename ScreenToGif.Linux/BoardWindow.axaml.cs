using Avalonia.Controls;
using ScreenToGif.Linux.Services;

namespace ScreenToGif.Linux;

public partial class BoardWindow : Window, ICaptureShellWindow
{
    public BoardWindow() => InitializeComponent();
}
