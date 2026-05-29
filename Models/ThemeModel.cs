namespace Theminator.Models;

using System.Collections.Generic;
using System.Windows.Media;

public class ThemeModel
{
    public string Name { get; set; } = "New Theme";
    public Dictionary<string, Color> Colors { get; set; } = new();

    public static readonly string[] Keys = {
        "ContentBgBrush","SidebarBgBrush","ControlBgBrush","ControlHoverBrush",
        "ContentTextBrush","ContentDimBrush","ContentHighBrush",
        "SidebarTextBrush","SidebarDimBrush","SidebarHighBrush",
        "ControlTextBrush","ControlDimBrush","ControlHighBrush",
        "InputBgBrush","InputTextBrush","InputDimBrush","InputHighBrush",
        "AccentBgBrush","AccentTextBrush","AccentHighlightBrush",
        "PrimaryAccentBrush","SecondaryAccentBrush","TertiaryAccentBrush",
        "PrimaryBubbleBrush","SecondaryBubbleBrush","TertiaryBubbleBrush",
        "PrimaryTextBrush","SecondaryTextBrush","TertiaryTextBrush"
    };

    public static readonly (string Group, string[] Keys)[] Groups = {
        ("Content Surface",  new[]{"ContentBgBrush","ContentTextBrush","ContentDimBrush","ContentHighBrush"}),
        ("Sidebar Surface",  new[]{"SidebarBgBrush","SidebarTextBrush","SidebarDimBrush","SidebarHighBrush"}),
        ("Control Surface",  new[]{"ControlBgBrush","ControlHoverBrush","ControlTextBrush","ControlDimBrush","ControlHighBrush"}),
        ("Input Surface",    new[]{"InputBgBrush","InputTextBrush","InputDimBrush","InputHighBrush"}),
        ("Accent Colors",    new[]{"AccentBgBrush","AccentTextBrush","AccentHighlightBrush","PrimaryAccentBrush","SecondaryAccentBrush","TertiaryAccentBrush"}),
        ("Chat Bubbles",     new[]{"PrimaryBubbleBrush","SecondaryBubbleBrush","TertiaryBubbleBrush"}),
        ("Text Hierarchy",   new[]{"PrimaryTextBrush","SecondaryTextBrush","TertiaryTextBrush"})
    };

    public static readonly Dictionary<string, string> Descriptions = new() {
        ["ContentBgBrush"]        = "Main chat / content area background",
        ["SidebarBgBrush"]        = "Left sidebar / navigation background",
        ["ControlBgBrush"]        = "Cards, panels, and control backgrounds",
        ["ControlHoverBrush"]     = "Hover state for cards and controls",
        ["ContentTextBrush"]      = "Normal text in content area",
        ["ContentDimBrush"]       = "Subdued / secondary text in content",
        ["ContentHighBrush"]      = "Highlighted symbols / icons in content",
        ["SidebarTextBrush"]      = "Normal text in sidebar",
        ["SidebarDimBrush"]       = "Subdued / secondary text in sidebar",
        ["SidebarHighBrush"]      = "Highlighted symbols / icons in sidebar",
        ["ControlTextBrush"]      = "Normal text on controls and cards",
        ["ControlDimBrush"]       = "Subdued text on controls",
        ["ControlHighBrush"]      = "Highlighted symbols on controls",
        ["InputBgBrush"]          = "Text input field background",
        ["InputTextBrush"]        = "Text typed into input fields",
        ["InputDimBrush"]         = "Placeholder / hint text in inputs",
        ["InputHighBrush"]        = "Highlighted symbols in input areas",
        ["AccentBgBrush"]         = "Primary accent / button background",
        ["AccentTextBrush"]       = "Text on accent-coloured buttons",
        ["AccentHighlightBrush"]  = "Lighter accent variant (hover glow)",
        ["PrimaryAccentBrush"]    = "Primary brand accent color",
        ["SecondaryAccentBrush"]  = "Secondary accent color",
        ["TertiaryAccentBrush"]   = "Tertiary accent color",
        ["PrimaryBubbleBrush"]    = "Chat bubble — user / primary",
        ["SecondaryBubbleBrush"]  = "Chat bubble — AI / secondary",
        ["TertiaryBubbleBrush"]   = "Chat bubble — system / tertiary",
        ["PrimaryTextBrush"]      = "Top-level text hierarchy color",
        ["SecondaryTextBrush"]    = "Mid-level text hierarchy color",
        ["TertiaryTextBrush"]     = "Low-level text hierarchy color"
    };

    public static ThemeModel ClaudesChoice()
    {
        var m = new ThemeModel { Name = "Claude's Choice" };
        var data = new Dictionary<string, string> {
            ["ContentBgBrush"]       = "#0D1117",
            ["SidebarBgBrush"]       = "#161B22",
            ["ControlBgBrush"]       = "#1C2333",
            ["ControlHoverBrush"]    = "#243044",
            ["ContentTextBrush"]     = "#E6EDF3",
            ["ContentDimBrush"]      = "#6E8094",
            ["ContentHighBrush"]     = "#2DD8CE",
            ["SidebarTextBrush"]     = "#CDD6E0",
            ["SidebarDimBrush"]      = "#4D6278",
            ["SidebarHighBrush"]     = "#E8A840",
            ["ControlTextBrush"]     = "#8899B8",
            ["ControlDimBrush"]      = "#4A5870",
            ["ControlHighBrush"]     = "#B060E8",
            ["InputBgBrush"]         = "#0A0F18",
            ["InputTextBrush"]       = "#D8E4F0",
            ["InputDimBrush"]        = "#506080",
            ["InputHighBrush"]       = "#40C8E0",
            ["AccentBgBrush"]        = "#18C0B4",
            ["AccentTextBrush"]      = "#060C10",
            ["AccentHighlightBrush"] = "#30E0D4",
            ["PrimaryAccentBrush"]   = "#18C0B4",
            ["SecondaryAccentBrush"] = "#E8A840",
            ["TertiaryAccentBrush"]  = "#B060E8",
            ["PrimaryBubbleBrush"]   = "#111828",
            ["SecondaryBubbleBrush"] = "#0E1420",
            ["TertiaryBubbleBrush"]  = "#151C30",
            ["PrimaryTextBrush"]     = "#E6EDF3",
            ["SecondaryTextBrush"]   = "#A8B8CC",
            ["TertiaryTextBrush"]    = "#687888",
        };
        foreach (var kv in data)
            m.Colors[kv.Key] = ParseHex(kv.Value);
        return m;
    }

    public static Color ParseHex(string hex)
    {
        hex = hex.TrimStart('#');
        return hex.Length switch {
            3  => Color.FromRgb(
                    Convert.ToByte(new string(hex[0], 2), 16),
                    Convert.ToByte(new string(hex[1], 2), 16),
                    Convert.ToByte(new string(hex[2], 2), 16)),
            6  => Color.FromRgb(
                    Convert.ToByte(hex[..2], 16),
                    Convert.ToByte(hex[2..4], 16),
                    Convert.ToByte(hex[4..6], 16)),
            8  => Color.FromArgb(
                    Convert.ToByte(hex[..2], 16),
                    Convert.ToByte(hex[2..4], 16),
                    Convert.ToByte(hex[4..6], 16),
                    Convert.ToByte(hex[6..8], 16)),
            _  => System.Windows.Media.Colors.Magenta
        };
    }

    public static string ToHex(Color c) =>
        c.A < 255
            ? $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}"
            : $"#{c.R:X2}{c.G:X2}{c.B:X2}";
}
