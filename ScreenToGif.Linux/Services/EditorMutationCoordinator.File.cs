using ScreenToGif.Linux.Models;

namespace ScreenToGif.Linux.Services;

public sealed partial class EditorMutationCoordinator
{
    public bool IsPristineEmptySession(EditorSnapshot current) =>
        current.Frames.Count == 0 &&
        !HasUnsavedChanges &&
        !history.CanUndo &&
        !history.CanRedo;

    public void FinalizeImport(EditorSnapshot before)
    {
        if (IsPristineEmptySession(before))
        {
            history.SetBaseline(captureSnapshot());
            Refresh();
            return;
        }

        Commit("Insert media", before);
    }
}
