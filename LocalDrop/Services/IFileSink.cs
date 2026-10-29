namespace LocalDrop.Services;

/// <summary>Platform-specific destination for received files (Downloads/LocalDrop/&lt;sender&gt;/).</summary>
public interface IFileSink
{
    /// <summary>Human readable description of the root folder, e.g. "C:\Users\me\Downloads\LocalDrop".</summary>
    string RootDescription { get; }

    /// <summary>Streams <paramref name="content"/> to a new file and returns a display path.</summary>
    Task<string> SaveAsync(string sender, string fileName, Stream content, long length, IProgress<long>? progress, CancellationToken ct);

    /// <summary>Opens the root folder in the platform file manager (no-op where unsupported).</summary>
    Task OpenRootAsync();
}

/// <summary>Platform-specific user notification when a file arrives.</summary>
public interface INotifier
{
    Task NotifyReceivedAsync(ReceivedFile file);
}

public sealed record ReceivedFile(string Sender, string FileName, long Size, string DisplayPath, DateTime ReceivedAt);
