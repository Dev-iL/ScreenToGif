using System.Diagnostics;

namespace ScreenToGif.Linux.Services;

public enum EditorArtifactKind
{
    Frames,
    Imports,
    Edits,
    Transitions,
    Pastes,
    Clipboard,
    Recordings
}

/// <summary>Owns one editor workspace and the lifetime of every generated artifact batch.</summary>
public sealed class EditorWorkspace : IDisposable
{
    private const string WorkspaceRootName = "screentogif-linux";
    private const string OwnerFileName = ".owner";
    private bool _disposed;

    private EditorWorkspace(string rootPath)
    {
        RootPath = Path.GetFullPath(rootPath);
        try
        {
            Directory.CreateDirectory(RootPath);
            using var process = Process.GetCurrentProcess();
            File.WriteAllText(Path.Combine(RootPath, OwnerFileName), $"{process.Id}|{process.StartTime.ToUniversalTime().Ticks}");
        }
        catch
        {
            ProjectArchive.TryDeleteWorkspace(RootPath);
            throw;
        }
    }

    public string RootPath { get; }

    public static EditorWorkspace Create(string? rootPath = null) => new(rootPath ?? Path.Combine(
        Path.GetTempPath(), WorkspaceRootName, Guid.NewGuid().ToString("N")));

    public string CreateBatch(EditorArtifactKind kind)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var path = Path.Combine(RootPath, FolderName(kind), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    public void PruneUnreachable(IEnumerable<string> referencedFiles)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var referenced = referencedFiles.Select(Path.GetFullPath).ToHashSet(StringComparer.Ordinal);
        foreach (var kind in new[]
                 {
                     EditorArtifactKind.Frames, EditorArtifactKind.Imports, EditorArtifactKind.Edits,
                     EditorArtifactKind.Transitions, EditorArtifactKind.Pastes, EditorArtifactKind.Recordings
                 })
        {
            var root = Path.Combine(RootPath, FolderName(kind));
            try
            {
                if (!Directory.Exists(root))
                    continue;
                foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                {
                    if (!referenced.Contains(Path.GetFullPath(file)))
                        TryDeleteFile(file);
                }

                foreach (var directory in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories)
                             .OrderByDescending(path => path.Length))
                    TryDeleteEmptyDirectory(directory);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        ProjectArchive.TryDeleteWorkspace(RootPath);
    }

    /// <summary>
    /// The directory one artifact kind lives in, relative to a workspace root. Internal because the
    /// workspace scavenger has to recognise a kind from the outside, and one spelling of these
    /// names is what keeps the two agreeing.
    /// </summary>
    internal static string FolderName(EditorArtifactKind kind) => kind switch
    {
        EditorArtifactKind.Frames => "frames",
        EditorArtifactKind.Imports => "imports",
        EditorArtifactKind.Edits => "edits",
        EditorArtifactKind.Transitions => "transitions",
        EditorArtifactKind.Pastes => "pastes",
        EditorArtifactKind.Clipboard => "clipboard",
        EditorArtifactKind.Recordings => "recordings",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static void TryDeleteEmptyDirectory(string path)
    {
        try
        {
            if (!Directory.EnumerateFileSystemEntries(path).Any())
                Directory.Delete(path);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
