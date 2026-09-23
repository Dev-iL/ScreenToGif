namespace ScreenToGif.Linux.Services;

public enum CaptureShellKind
{
    Recorder,
    Webcam,
    Board
}

public interface ICaptureShellWindow
{
    event EventHandler? Closed;

    /// <summary>
    /// True once this shell has passed its result to another window, which then owns the session.
    /// Startup stays closed in that case rather than reappearing behind the editor. Shells that
    /// produce nothing never set it, which is why it has a default.
    /// </summary>
    bool HandedOffToEditor => false;

    void Show();

    void Activate();
}

public interface ICaptureShellHost
{
    ICaptureShellWindow CreateShell(CaptureShellKind kind);

    void HideStartup();

    void RestoreStartup();

    /// <summary>Closes Startup for good, because another window has taken over the session.</summary>
    void CloseStartup();
}

/// <summary>
/// Owns the single-child capture-shell lifecycle while retaining one Startup window.
/// </summary>
public sealed class CaptureShellCoordinator(ICaptureShellHost host)
{
    private ICaptureShellWindow? _activeShell;

    public CaptureShellKind? ActiveKind { get; private set; }

    public bool Open(CaptureShellKind kind)
    {
        if (_activeShell is not null)
        {
            _activeShell.Activate();
            return false;
        }

        var shell = host.CreateShell(kind);
        _activeShell = shell;
        ActiveKind = kind;
        shell.Closed += ShellClosed;

        try
        {
            host.HideStartup();
            shell.Show();
            return true;
        }
        catch
        {
            shell.Closed -= ShellClosed;
            _activeShell = null;
            ActiveKind = null;
            host.RestoreStartup();
            throw;
        }
    }

    private void ShellClosed(object? sender, EventArgs e)
    {
        var shell = _activeShell;
        if (shell is null || !ReferenceEquals(sender, shell))
            return;

        shell.Closed -= ShellClosed;
        _activeShell = null;
        ActiveKind = null;

        if (shell.HandedOffToEditor)
            host.CloseStartup();
        else
            host.RestoreStartup();
    }
}
