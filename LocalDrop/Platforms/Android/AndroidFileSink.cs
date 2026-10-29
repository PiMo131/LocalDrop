using Android.Content;
using Android.Provider;
using Android.Webkit;
using LocalDrop.Services;
using AndroidEnvironment = Android.OS.Environment;

namespace LocalDrop;

/// <summary>Writes into the public Downloads collection via MediaStore (no storage permission needed on Android 10+).</summary>
public sealed class AndroidFileSink : IFileSink
{
    private const string Folder = "LocalDrop";

    public string RootDescription => $"{AndroidEnvironment.DirectoryDownloads}/{Folder}/";

    public async Task<string> SaveAsync(string sender, string fileName, Stream content, long length, IProgress<long>? progress, CancellationToken ct)
    {
        var resolver = Platform.AppContext.ContentResolver
            ?? throw new InvalidOperationException("No ContentResolver");

        var relativePath = $"{AndroidEnvironment.DirectoryDownloads}/{Folder}/{sender}/";
        var mime = GuessMime(fileName);

        var values = new ContentValues();
        values.Put(MediaStore.IMediaColumns.DisplayName, fileName);
        values.Put(MediaStore.IMediaColumns.MimeType, mime);
        values.Put(MediaStore.IMediaColumns.RelativePath, relativePath);
        values.Put(MediaStore.IMediaColumns.IsPending, 1);

        var collection = MediaStore.Downloads.ExternalContentUri
            ?? throw new InvalidOperationException("No Downloads collection");
        var uri = resolver.Insert(collection, values)
            ?? throw new IOException("MediaStore insert failed");

        try
        {
            using (var os = resolver.OpenOutputStream(uri, "w") ?? throw new IOException("Cannot open output stream"))
            {
                await StreamCopy.CopyAsync(content, os, length, progress, ct);
                await os.FlushAsync(ct);
            }
            values.Clear();
            values.Put(MediaStore.IMediaColumns.IsPending, 0);
            resolver.Update(uri, values, null, null);
        }
        catch
        {
            try { resolver.Delete(uri, null, null); } catch { }
            throw;
        }

        // MediaStore may have renamed on collision; read the final display name.
        var finalName = fileName;
        try
        {
            using var cursor = resolver.Query(uri, new[] { MediaStore.IMediaColumns.DisplayName }, null, null, null);
            if (cursor != null && cursor.MoveToFirst())
                finalName = cursor.GetString(0) ?? fileName;
        }
        catch { }

        return relativePath + finalName;
    }

    public Task OpenRootAsync()
    {
        try
        {
            var intent = new Intent(Android.App.DownloadManager.ActionViewDownloads);
            intent.AddFlags(ActivityFlags.NewTask);
            Platform.AppContext.StartActivity(intent);
        }
        catch { }
        return Task.CompletedTask;
    }

    private static string GuessMime(string fileName)
    {
        var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        var mime = string.IsNullOrEmpty(ext) ? null : MimeTypeMap.Singleton?.GetMimeTypeFromExtension(ext);
        return mime ?? "application/octet-stream";
    }
}
