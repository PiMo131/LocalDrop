namespace LocalDrop.Services;

public static class StreamCopy
{
    /// <summary>Copies with progress; stops at <paramref name="length"/> bytes when known (&gt; 0).</summary>
    public static async Task<long> CopyAsync(Stream source, Stream target, long length, IProgress<long>? progress, CancellationToken ct)
    {
        var buffer = new byte[256 * 1024];
        long total = 0;
        long lastReport = 0;
        while (true)
        {
            int want = buffer.Length;
            if (length > 0) want = (int)Math.Min(want, length - total);
            if (want <= 0) break;
            int n = await source.ReadAsync(buffer.AsMemory(0, want), ct);
            if (n <= 0) break;
            await target.WriteAsync(buffer.AsMemory(0, n), ct);
            total += n;
            if (total - lastReport >= 512 * 1024) { progress?.Report(total); lastReport = total; }
        }
        progress?.Report(total);
        return total;
    }

    /// <summary>Returns "name (2).ext" style variants until <paramref name="exists"/> is false.</summary>
    public static string UniqueName(string fileName, Func<string, bool> exists)
    {
        if (!exists(fileName)) return fileName;
        var stem = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        for (int i = 2; i < 10000; i++)
        {
            var candidate = $"{stem} ({i}){ext}";
            if (!exists(candidate)) return candidate;
        }
        return $"{stem} {Guid.NewGuid():N}{ext}";
    }
}
