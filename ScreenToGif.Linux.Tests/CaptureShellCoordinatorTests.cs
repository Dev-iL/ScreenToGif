using ScreenToGif.Linux.Services;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class CaptureShellCoordinatorTests
{
    [Fact]
    public void OpenAndCloseHidesThenRestoresSameStartupHost()
    {
        var host = new FakeHost();
        var coordinator = new CaptureShellCoordinator(host);

        Assert.True(coordinator.Open(CaptureShellKind.Recorder));
        Assert.False(host.StartupVisible);
        Assert.Equal(CaptureShellKind.Recorder, coordinator.ActiveKind);
        Assert.Single(host.Created);
        Assert.Equal(CaptureShellKind.Recorder, host.Created[0].Kind);
        Assert.True(host.Created[0].WasShown);

        host.Created[0].Close();

        Assert.True(host.StartupVisible);
        Assert.Equal(1, host.RestoreCount);
        Assert.Null(coordinator.ActiveKind);
    }

    [Fact]
    public void RepeatedVisitsCreateOneShellAtATimeWithoutLeakingState()
    {
        var host = new FakeHost();
        var coordinator = new CaptureShellCoordinator(host);

        Assert.True(coordinator.Open(CaptureShellKind.Board));
        Assert.False(coordinator.Open(CaptureShellKind.Webcam));
        Assert.Single(host.Created);
        Assert.Equal(1, host.Created[0].ActivateCount);

        host.Created[0].Close();
        Assert.True(coordinator.Open(CaptureShellKind.Webcam));
        host.Created[1].Close();

        Assert.Equal(2, host.Created.Count);
        Assert.Equal(2, host.HideCount);
        Assert.Equal(2, host.RestoreCount);
        Assert.True(host.StartupVisible);
        Assert.Null(coordinator.ActiveKind);
    }

    [Fact]
    public void ShowFailureRestoresStartupAndAllowsRetry()
    {
        var host = new FakeHost { FailNextShow = true };
        var coordinator = new CaptureShellCoordinator(host);

        Assert.Throws<InvalidOperationException>(() => coordinator.Open(CaptureShellKind.Recorder));
        Assert.True(host.StartupVisible);
        Assert.Null(coordinator.ActiveKind);

        Assert.True(coordinator.Open(CaptureShellKind.Recorder));
        Assert.Equal(2, host.Created.Count);
    }

    private sealed class FakeHost : ICaptureShellHost
    {
        public List<FakeShell> Created { get; } = [];

        public bool StartupVisible { get; private set; } = true;

        public bool FailNextShow { get; set; }

        public int HideCount { get; private set; }

        public int RestoreCount { get; private set; }

        public ICaptureShellWindow CreateShell(CaptureShellKind kind)
        {
            var shell = new FakeShell(kind, FailNextShow);
            FailNextShow = false;
            Created.Add(shell);
            return shell;
        }

        public void HideStartup()
        {
            StartupVisible = false;
            HideCount++;
        }

        public void RestoreStartup()
        {
            StartupVisible = true;
            RestoreCount++;
        }
    }

    private sealed class FakeShell(CaptureShellKind kind, bool failShow) : ICaptureShellWindow
    {
        public event EventHandler? Closed;

        public CaptureShellKind Kind { get; } = kind;

        public bool WasShown { get; private set; }

        public int ActivateCount { get; private set; }

        public void Show()
        {
            if (failShow)
                throw new InvalidOperationException("Synthetic show failure.");

            WasShown = true;
        }

        public void Activate() => ActivateCount++;

        public void Close() => Closed?.Invoke(this, EventArgs.Empty);
    }
}
