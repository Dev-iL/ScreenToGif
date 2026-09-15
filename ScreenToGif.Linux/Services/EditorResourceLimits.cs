namespace ScreenToGif.Linux.Services;

public static class EditorResourceLimits
{
    public const int MaximumMediaDimension = 8192;
    public const int MaximumProjectFrames = 10_000;
    public const long MaximumImportedPixels = 1_000_000_000;
    public const long MaximumProjectManifestBytes = 1_048_576;
    public const long MaximumProjectFrameBytes = 300_000_000;
    public const long MaximumProjectArchiveBytes = 4_294_967_296;

    public static void EnsureCanInsertFrames(int currentFrameCount, int additionalFrameCount, string operation)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(currentFrameCount);
        ArgumentOutOfRangeException.ThrowIfNegative(additionalFrameCount);

        if (currentFrameCount > MaximumProjectFrames ||
            additionalFrameCount > MaximumProjectFrames - currentFrameCount)
            throw new InvalidOperationException($"{operation} would exceed the {MaximumProjectFrames}-frame project limit; remove frames or add fewer frames.");
    }
}
