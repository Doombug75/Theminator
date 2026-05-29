namespace Theminator.Models;

using System.IO;
using System.Text.Json;

public class AppSettings
{
    public string ThemesFolder { get; set; } = string.Empty;
    public string LastThemeName { get; set; } = "Claude's Choice";

    private static string SettingsPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Theminator", "settings.json");

    public static AppSettings Load()
    {
        try {
            if (File.Exists(SettingsPath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath))
                       ?? new AppSettings();
        } catch { }
        return new AppSettings();
    }

    public void Save()
    {
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath,
                JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        } catch { }
    }

    // Resolve the themes folder: stored path first, then exe-adjacent Themes\, then empty
    public string ResolveThemesFolder()
    {
        if (!string.IsNullOrEmpty(ThemesFolder) && Directory.Exists(ThemesFolder))
            return ThemesFolder;
        var exeDir = Path.GetDirectoryName(Environment.ProcessPath) ?? "";
        var adj = Path.Combine(exeDir, "Themes");
        return Directory.Exists(adj) ? adj : string.Empty;
    }
}
