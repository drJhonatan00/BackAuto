using System.Text.Json;

namespace BackAuto.Models;

public sealed class AppSettings
{
    public List<string> SourceFiles { get; set; } = [];
    public string DestinationFolder { get; set; } = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\", "Backup");
    public int IntervalMinutes { get; set; } = 60;
    public bool DarkMode { get; set; }
    public bool ScheduleEnabled { get; set; }
    public DateTime? LastBackupUtc { get; set; }

    public static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BackAuto", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
        }
        catch { }
        return new AppSettings();
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
