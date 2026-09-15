using ScreenToGif.Linux.Models;
using ScreenToGif.Linux.Services;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class EditorMutationCoordinatorTests
{
    [Fact]
    public void Commit_RecordsHistoryDirtyStateAndRefreshesOnce()
    {
        var history = new FrameEditHistory();
        var snapshot = Snapshot("before.png");
        var refreshCount = 0;
        var coordinator = new EditorMutationCoordinator(history, () => snapshot, () => refreshCount++);
        history.SetBaseline(snapshot);
        coordinator.MarkClean();

        var before = snapshot;
        snapshot = Snapshot("after.png");
        coordinator.Commit("Edit", before);

        Assert.True(history.CanUndo);
        Assert.True(coordinator.HasUnsavedChanges);
        Assert.Equal(1, refreshCount);
        Assert.True(history.TryUndo(out var undone, out var undoDescription));
        Assert.Equal("Edit", undoDescription);
        Assert.Equal(before, undone);
        Assert.True(history.TryRedo(out var redone, out var redoDescription));
        Assert.Equal("Edit", redoDescription);
        Assert.Equal(snapshot, redone);
    }

    [Fact]
    public void Commit_PreservesSelectionOnlyHistoryEvenWhenFramesAreClean()
    {
        var history = new FrameEditHistory();
        var frames = new[] { new FrameState("same.png", 100), new FrameState("same.png", 100) };
        var before = new EditorSnapshot(frames, [0]);
        var current = before;
        var coordinator = new EditorMutationCoordinator(history, () => current, () => { });
        history.SetBaseline(before);
        coordinator.MarkClean();

        current = new EditorSnapshot(frames, [1]);
        coordinator.Commit("Move equal frames", before);

        Assert.False(coordinator.HasUnsavedChanges);
        Assert.True(history.TryUndo(out var undone, out var description));
        Assert.Equal("Move equal frames", description);
        Assert.Equal([0], undone.SelectedIndices);
        Assert.True(history.TryRedo(out var redone, out _));
        Assert.Equal([1], redone.SelectedIndices);
    }

    [Fact]
    public void Refresh_ReturningToCleanFramesClearsDirtyState()
    {
        var history = new FrameEditHistory();
        var clean = Snapshot("clean.png");
        var snapshot = clean;
        var coordinator = new EditorMutationCoordinator(history, () => snapshot, () => { });
        coordinator.MarkClean();
        snapshot = Snapshot("changed.png");
        coordinator.Refresh();
        Assert.True(coordinator.HasUnsavedChanges);

        snapshot = clean;
        coordinator.Refresh();

        Assert.False(coordinator.HasUnsavedChanges);
    }

    private static EditorSnapshot Snapshot(string path) =>
        new([new FrameState(path, 100)], [0]);
}
