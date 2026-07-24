using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuickFiles.Services;

public enum SortOrder
{
    NewestFirst,
    OldestFirst,
    NameAscending,
}

public sealed class AppSettings
{
    public int MaxItems { get; set; } = 15;
    public SortOrder SortOrder { get; set; } = SortOrder.NewestFirst;
    public bool IncludeDownloads { get; set; } = true;
    public bool IncludeRecent { get; set; } = true;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private static string SettingsDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QuickFiles");

    private static string SettingsPath => Path.Combine(SettingsDir, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath), JsonOptions);
                if (loaded != null)
                {
                    loaded.MaxItems = Math.Clamp(loaded.MaxItems, 1, 100);
                    return loaded;
                }
            }
        }
        catch
        {
            // Corrupt settings file: fall back to defaults.
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch
        {
            // Settings are a convenience; never crash the app over them.
        }
    }
}
