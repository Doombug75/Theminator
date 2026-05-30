namespace Theminator.Models;

using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;

public static class ThemeLoader
{
    public static string NameFromPath(string path) =>
        Path.GetFileNameWithoutExtension(path);

    /// <summary>
    /// Converts a CamelCase filename to a spaced display name.
    /// "ArcticAurora" → "Arctic Aurora", "RSIProtocol" → "RSI Protocol".
    /// </summary>
    public static string FormatName(string fileName) =>
        Regex.Replace(fileName,
            @"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ");

    /// <summary>
    /// Loads a theme from <paramref name="path"/> (native OXSUIT 1.0 XML format).
    /// Returns <c>null</c> when the file does not exist, cannot be read,
    /// or contains no recognisable colour entries.
    /// Missing keys are silently filled with Claude's Choice defaults.
    /// </summary>
    public static ThemeModel? Load(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            var text = File.ReadAllText(path, Encoding.UTF8);
            return LoadOxsuit(text, path);
        }
        catch { return null; }
    }

    private static ThemeModel? LoadOxsuit(string text, string path)
    {
        var model = new ThemeModel { Name = NameFromPath(path) };

        // Read name / author / description from the <oxsuit> root element
        var rootMatch = Regex.Match(text, @"<oxsuit\b[^>]*>");
        if (rootMatch.Success)
        {
            var root    = rootMatch.Value;
            var nameM   = Regex.Match(root, @"\bname=""([^""]*)""");
            var authorM = Regex.Match(root, @"\bauthor=""([^""]*)""");
            var descM   = Regex.Match(root, @"\bdescription=""([^""]*)""");
            if (nameM.Success)   model.Name        = nameM.Groups[1].Value;
            if (authorM.Success) model.Author      = authorM.Groups[1].Value;
            if (descM.Success)   model.Description = descM.Groups[1].Value;
        }

        var colorPattern = new Regex(
            @"<color\s+key=""(\w+)""\s+value=""(#[0-9A-Fa-f]{3,8})""\s*/>",
            RegexOptions.Compiled);

        int matched = 0;
        foreach (Match m in colorPattern.Matches(text))
        {
            var wpfKey = ThemeModel.OxsuitKeyToWpf(m.Groups[1].Value);
            if (Array.IndexOf(ThemeModel.Keys, wpfKey) >= 0)
            {
                model.Colors[wpfKey] = ThemeModel.ParseOxsuitHex(m.Groups[2].Value);
                matched++;
            }
        }

        if (matched == 0) return null;

        // ── Geometry tokens ───────────────────────────────────────────────────
        var tokenPat = new Regex(@"<token\s+key=""([^""]+)""\s+value=""([^""]+)""",
                                  RegexOptions.Compiled);
        foreach (Match m in tokenPat.Matches(text))
        {
            var key = m.Groups[1].Value;
            var val = m.Groups[2].Value;
            if (!double.TryParse(val,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var d)) continue;

            switch (key)
            {
                case "CornerRadius": model.CornerRadius = d; break;
                case "ShadowDepth":  model.ShadowDepth  = d; break;
                default:
                    // per-element border widths — map token key → brush key
                    var be = Array.Find(ThemeModel.BorderEntries, e => e.TokenKey == key);
                    if (be != default) model.Thicknesses[be.BrushKey] = d;
                    break;
            }
        }

        var fallback = ThemeModel.ClaudesChoice();
        foreach (var k in ThemeModel.Keys)
            if (!model.Colors.ContainsKey(k))
                model.Colors[k] = fallback.Colors[k];

        return model;
    }

    /// <summary>
    /// Saves <paramref name="model"/> to <paramref name="path"/> in native OXSUIT 1.0 XML format.
    /// </summary>
    public static void Save(ThemeModel model, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.Append($"<oxsuit version=\"1.0\" name=\"{EscapeXml(model.Name)}\"");
        if (!string.IsNullOrWhiteSpace(model.Author))
            sb.Append($" author=\"{EscapeXml(model.Author)}\"");
        if (!string.IsNullOrWhiteSpace(model.Description))
            sb.Append($" description=\"{EscapeXml(model.Description)}\"");
        sb.AppendLine(">");
        sb.AppendLine();
        sb.AppendLine("  <colors>");
        sb.AppendLine();

        foreach (var (group, keys) in ThemeModel.Groups)
        {
            sb.AppendLine($"    <!-- {group} -->");
            foreach (var k in keys)
            {
                var c      = model.Colors.TryGetValue(k, out var col) ? col : Colors.Magenta;
                var oxKey  = ThemeModel.WpfKeyToOxsuit(k);
                var hexVal = ThemeModel.ToOxsuitHex(c);
                sb.AppendLine($"    <color key=\"{oxKey}\" value=\"{hexVal}\"/>");
            }
            sb.AppendLine();
        }

        sb.AppendLine("  </colors>");
        sb.AppendLine();

        // ── Geometry tokens ───────────────────────────────────────────────────
        sb.AppendLine("  <tokens>");
        sb.AppendLine($"    <token key=\"CornerRadius\" value=\"{model.CornerRadius.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}\"/>");
        sb.AppendLine($"    <token key=\"ShadowDepth\"  value=\"{model.ShadowDepth.ToString("0.##",  System.Globalization.CultureInfo.InvariantCulture)}\"/>");
        foreach (var (brushKey, tokenKey, _) in ThemeModel.BorderEntries)
            sb.AppendLine($"    <token key=\"{tokenKey}\" value=\"{model.GetThickness(brushKey).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}\"/>");
        sb.AppendLine("  </tokens>");
        sb.AppendLine("</oxsuit>");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
    }

    private static string EscapeXml(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
