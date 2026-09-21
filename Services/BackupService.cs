namespace BackAuto.Services;

public sealed record BackupResult(int Copied, int Skipped, int Failed, TimeSpan Duration, string? Error);

public sealed class BackupService
{
    public async Task<BackupResult> RunAsync(IEnumerable<string> sourceFiles, string destination, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        var started = DateTime.UtcNow;
        var copied = 0; var skipped = 0; var failed = 0; string? lastError = null;
        Directory.CreateDirectory(destination);
        foreach (var source in sourceFiles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (!File.Exists(source)) { skipped++; progress?.Report($"Skipped (not found): {source}"); continue; }
                var target = Path.Combine(destination, Path.GetFileName(source));
                var shouldCopy = !File.Exists(target) || File.GetLastWriteTimeUtc(source) > File.GetLastWriteTimeUtc(target) || new FileInfo(source).Length != new FileInfo(target).Length;
                if (!shouldCopy) { skipped++; progress?.Report($"Up to date: {Path.GetFileName(source)}"); continue; }
                await using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, true);
                await using var output = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024, true);
                await input.CopyToAsync(output, cancellationToken);
                File.SetLastWriteTimeUtc(target, File.GetLastWriteTimeUtc(source));
                copied++; progress?.Report($"Copied: {Path.GetFileName(source)}");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            { failed++; lastError = ex.Message; progress?.Report($"Failed: {Path.GetFileName(source)} — {ex.Message}"); }
        }
        return new BackupResult(copied, skipped, failed, DateTime.UtcNow - started, lastError);
    }
}
