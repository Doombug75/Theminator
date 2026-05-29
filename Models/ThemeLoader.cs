namespace Theminator.Models;

using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;

public static class ThemeLoader
{
    // Read theme name from filename (strip .xaml)
    public static string NameFromPath(string path) =>
        Path.GetFileNameWithoutExtension(path);

    public static ThemeModel? Load(string path)
    {
        if (!File.Exists(path)) return null;
        var text = File.ReadAllText(path, Encoding.UTF8);
        var model = new ThemeModel { Name = NameFromPath(path) };

        var pattern = new Regex(
            @"x:Key=""(\w+)""\s+Color=""(#[0-9A-Fa-f]{3,8})""",
            RegexOptions.Compiled);

        foreach (Match m in pattern.Matches(text))
        {
            var key = m.Groups[1].Value;
            if (Array.IndexOf(ThemeModel.Keys, key) >= 0)
                model.Colors[key] = ThemeModel.ParseHex(m.Groups[2].Value);
        }

        // Fill any missing keys with Claude's Choice fallback
        var fallback = ThemeModel.ClaudesChoice();
        foreach (var k in ThemeModel.Keys)
            if (!model.Colors.ContainsKey(k))
                model.Colors[k] = fallback.Colors[k];

        return model;
    }

    public static void Save(ThemeModel model, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""
            <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
            """);
        sb.AppendLine($"    <!-- {model.Name} — created with Theminator -->");
        sb.AppendLine();

        foreach (var (group, keys) in ThemeModel.Groups)
        {
            sb.AppendLine($"    <!-- {group} -->");
            foreach (var k in keys)
            {
                var c = model.Colors.TryGetValue(k, out var col) ? col : Colors.Magenta;
                sb.AppendLine($"    <SolidColorBrush x:Key=\"{k}\" Color=\"{ThemeModel.ToHex(c)}\"/>");
            }
            sb.AppendLine();
        }

        sb.AppendLine("</ResourceDictionary>");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
    }
}
