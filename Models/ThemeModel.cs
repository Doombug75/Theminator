namespace Theminator.Models;

using System.Collections.Generic;
using System.Windows.Media;

public class ThemeModel
{
    public string Name        { get; set; } = "New Theme";
    public string Author      { get; set; } = "";
    public string Description { get; set; } = "";
    public Dictionary<string, Color>  Colors     { get; set; } = new();
    public Dictionary<string, double> Thicknesses { get; set; } = new();
    public double CornerRadius { get; set; } = 6.0;
    public double ShadowDepth  { get; set; } = 2.0;

    /// <summary>
    /// Returns the border thickness for a given brush key.
    /// Defaults to 1.0 if not explicitly set.
    /// </summary>
    public double GetThickness(string brushKey) =>
        Thicknesses.TryGetValue(brushKey, out var t) ? t : 1.0;

    /// <summary>
    /// All border-colour keys that have a matching per-element thickness token.
    /// Each entry: (WPF brush key, OXSUIT token key, left-panel label).
    /// </summary>
    public static readonly (string BrushKey, string TokenKey, string Label)[] BorderEntries =
    {
        ("ContentBorderBrush",         "ContentBorderWidth",   "Content border"),
        ("SidebarBorderBrush",         "SidebarBorderWidth",   "Sidebar border"),
        ("ControlBorderBrush",         "ControlBorderWidth",   "Control border"),
        ("InputBorderBrush",           "InputBorderWidth",     "Input border"),
        ("PrimaryBubbleBorderBrush",   "PrimaryBorderWidth",   "Primary bubble"),
        ("SecondaryBubbleBorderBrush", "SecondaryBorderWidth", "Secondary bubble"),
        ("TertiaryBubbleBorderBrush",  "TertiaryBorderWidth",  "Tertiary bubble"),
    };

    public static readonly string[] Keys = {
        "ContentBgBrush","SidebarBgBrush","ControlBgBrush","ControlHoverBrush",
        "ContentTextBrush","ContentDimBrush","ContentHighBrush","ContentBorderBrush",
        "SidebarTextBrush","SidebarDimBrush","SidebarHighBrush","SidebarBorderBrush",
        "ControlTextBrush","ControlDimBrush","ControlHighBrush","ControlBorderBrush",
        "InputBgBrush","InputTextBrush","InputDimBrush","InputHighBrush","InputBorderBrush",
        "AccentBgBrush","AccentTextBrush","AccentHighlightBrush",
        "PrimaryAccentBrush","SecondaryAccentBrush","TertiaryAccentBrush",
        "PrimaryBubbleBrush","PrimaryTextBrush","PrimaryDimBrush","PrimaryHighBrush","PrimaryBubbleBorderBrush",
        "SecondaryBubbleBrush","SecondaryTextBrush","SecondaryDimBrush","SecondaryHighBrush","SecondaryBubbleBorderBrush",
        "TertiaryBubbleBrush","TertiaryTextBrush","TertiaryDimBrush","TertiaryHighBrush","TertiaryBubbleBorderBrush"
    };

    public static readonly (string Group, string[] Keys)[] Groups = {
        ("Content Surface",   new[]{"ContentBgBrush","ContentTextBrush","ContentDimBrush","ContentHighBrush","ContentBorderBrush"}),
        ("Sidebar Surface",   new[]{"SidebarBgBrush","SidebarTextBrush","SidebarDimBrush","SidebarHighBrush","SidebarBorderBrush"}),
        ("Control Surface",   new[]{"ControlBgBrush","ControlHoverBrush","ControlTextBrush","ControlDimBrush","ControlHighBrush","ControlBorderBrush"}),
        ("Input Surface",     new[]{"InputBgBrush","InputTextBrush","InputDimBrush","InputHighBrush","InputBorderBrush"}),
        ("Accent",            new[]{"AccentBgBrush","AccentTextBrush","AccentHighlightBrush","PrimaryAccentBrush","SecondaryAccentBrush","TertiaryAccentBrush"}),
        ("Primary Slot",      new[]{"PrimaryBubbleBrush","PrimaryTextBrush","PrimaryDimBrush","PrimaryHighBrush","PrimaryBubbleBorderBrush"}),
        ("Secondary Slot",    new[]{"SecondaryBubbleBrush","SecondaryTextBrush","SecondaryDimBrush","SecondaryHighBrush","SecondaryBubbleBorderBrush"}),
        ("Tertiary Slot",     new[]{"TertiaryBubbleBrush","TertiaryTextBrush","TertiaryDimBrush","TertiaryHighBrush","TertiaryBubbleBorderBrush"})
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
        ["InputBorderBrush"]      = "Border / frame color for input fields",
        ["AccentBgBrush"]         = "Primary accent / button background",
        ["AccentTextBrush"]       = "Text on accent-coloured buttons",
        ["AccentHighlightBrush"]  = "Lighter accent variant (hover glow)",
        ["PrimaryAccentBrush"]    = "Primary brand accent color",
        ["SecondaryAccentBrush"]  = "Secondary accent color",
        ["TertiaryAccentBrush"]   = "Tertiary accent color",
        ["ContentBorderBrush"]          = "Border / frame color for the content area",
        ["SidebarBorderBrush"]          = "Border / frame color for the sidebar",
        ["ControlBorderBrush"]          = "Border / frame color for cards and controls",
        ["InputBorderBrush"]            = "Border / frame color for input fields",
        ["PrimaryBubbleBrush"]          = "Primary bubble background (e.g. user messages)",
        ["PrimaryTextBrush"]            = "Normal text on primary bubble",
        ["PrimaryDimBrush"]             = "Subdued text on primary bubble",
        ["PrimaryHighBrush"]            = "Highlighted symbols on primary bubble",
        ["PrimaryBubbleBorderBrush"]    = "Border / frame color for primary bubbles",
        ["SecondaryBubbleBrush"]        = "Secondary bubble background (e.g. AI messages)",
        ["SecondaryTextBrush"]          = "Normal text on secondary bubble",
        ["SecondaryDimBrush"]           = "Subdued text on secondary bubble",
        ["SecondaryHighBrush"]          = "Highlighted symbols on secondary bubble",
        ["SecondaryBubbleBorderBrush"]  = "Border / frame color for secondary bubbles",
        ["TertiaryBubbleBrush"]         = "Tertiary bubble background (e.g. system messages)",
        ["TertiaryTextBrush"]           = "Normal text on tertiary bubble",
        ["TertiaryDimBrush"]            = "Subdued text on tertiary bubble",
        ["TertiaryHighBrush"]           = "Highlighted symbols on tertiary bubble",
        ["TertiaryBubbleBorderBrush"]   = "Border / frame color for tertiary bubbles"
    };

    /// <summary>
    /// A clean white/grey light-mode starting point — every background is white or
    /// neutral grey, all text is near-black, accent is standard Windows blue.
    /// Designed to be a blank canvas that is immediately usable before any edits.
    /// </summary>
    public static ThemeModel BlankTheme()
    {
        var m = new ThemeModel { Name = "New Theme" };
        var data = new Dictionary<string, string> {
            // Backgrounds
            ["ContentBgBrush"]              = "#FFFFFF",
            ["SidebarBgBrush"]              = "#F3F3F3",
            ["ControlBgBrush"]              = "#E8E8E8",
            ["ControlHoverBrush"]           = "#D8D8D8",
            // Content text
            ["ContentTextBrush"]            = "#1A1A1A",
            ["ContentDimBrush"]             = "#767676",
            ["ContentHighBrush"]            = "#0078D4",
            ["ContentBorderBrush"]          = "#DCDCDC",
            // Sidebar text
            ["SidebarTextBrush"]            = "#1A1A1A",
            ["SidebarDimBrush"]             = "#767676",
            ["SidebarHighBrush"]            = "#0078D4",
            ["SidebarBorderBrush"]          = "#CCCCCC",
            // Control text
            ["ControlTextBrush"]            = "#2C2C2C",
            ["ControlDimBrush"]             = "#767676",
            ["ControlHighBrush"]            = "#0078D4",
            ["ControlBorderBrush"]          = "#C0C0C0",
            // Input
            ["InputBgBrush"]                = "#FFFFFF",
            ["InputTextBrush"]              = "#1A1A1A",
            ["InputDimBrush"]               = "#A0A0A0",
            ["InputHighBrush"]              = "#0078D4",
            ["InputBorderBrush"]            = "#B4B4B4",
            // Accent — standard Windows blue
            ["AccentBgBrush"]               = "#0078D4",
            ["AccentTextBrush"]             = "#FFFFFF",
            ["AccentHighlightBrush"]        = "#106EBE",
            ["PrimaryAccentBrush"]          = "#0078D4",
            ["SecondaryAccentBrush"]        = "#767676",
            ["TertiaryAccentBrush"]         = "#5C5C5C",
            // Primary slot (lightest grey)
            ["PrimaryBubbleBrush"]          = "#F5F5F5",
            ["PrimaryTextBrush"]            = "#1A1A1A",
            ["PrimaryDimBrush"]             = "#767676",
            ["PrimaryHighBrush"]            = "#0078D4",
            ["PrimaryBubbleBorderBrush"]    = "#DCDCDC",
            // Secondary slot
            ["SecondaryBubbleBrush"]        = "#EBEBEB",
            ["SecondaryTextBrush"]          = "#1A1A1A",
            ["SecondaryDimBrush"]           = "#767676",
            ["SecondaryHighBrush"]          = "#0078D4",
            ["SecondaryBubbleBorderBrush"]  = "#DCDCDC",
            // Tertiary slot (slightly darker)
            ["TertiaryBubbleBrush"]         = "#E0E0E0",
            ["TertiaryTextBrush"]           = "#3C3C3C",
            ["TertiaryDimBrush"]            = "#767676",
            ["TertiaryHighBrush"]           = "#0078D4",
            ["TertiaryBubbleBorderBrush"]   = "#DCDCDC",
        };
        foreach (var kv in data)
            m.Colors[kv.Key] = ParseHex(kv.Value);
        return m;
    }

    public static ThemeModel ClaudesChoice()
    {
        var m = new ThemeModel
        {
            Name        = "Claude's Choice",
            Author      = "Claude (Anthropic)",
            Description = "Deep blue-teal dark theme with amber and violet accents."
        };
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
            ["ContentBorderBrush"]          = "#1518C0B4",
            ["SidebarBorderBrush"]          = "#2518C0B4",
            ["ControlBorderBrush"]          = "#3518C0B4",
            ["InputBorderBrush"]            = "#5018C0B4",
            ["PrimaryBubbleBrush"]          = "#111828",
            ["PrimaryTextBrush"]            = "#E6EDF3",
            ["PrimaryDimBrush"]             = "#6E8094",
            ["PrimaryHighBrush"]            = "#2DD8CE",
            ["PrimaryBubbleBorderBrush"]    = "#2018C0B4",
            ["SecondaryBubbleBrush"]        = "#0E1420",
            ["SecondaryTextBrush"]          = "#A8B8CC",
            ["SecondaryDimBrush"]           = "#6E8094",
            ["SecondaryHighBrush"]          = "#2DD8CE",
            ["SecondaryBubbleBorderBrush"]  = "#2018C0B4",
            ["TertiaryBubbleBrush"]         = "#151C30",
            ["TertiaryTextBrush"]           = "#687888",
            ["TertiaryDimBrush"]            = "#6E8094",
            ["TertiaryHighBrush"]           = "#2DD8CE",
            ["TertiaryBubbleBorderBrush"]   = "#1518C0B4",
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

    /// <summary>Web-standard hex: #RRGGBB (fully opaque) or #RRGGBBAA (alpha last).</summary>
    public static string ToOxsuitHex(Color c) =>
        c.A < 255
            ? $"#{c.R:X2}{c.G:X2}{c.B:X2}{c.A:X2}"
            : $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    /// <summary>Maps a WPF resource key (e.g. "ContentBgBrush") to its OXSUIT colour key ("ContentBg").</summary>
    public static string WpfKeyToOxsuit(string key) => key switch
    {
        "PrimaryBubbleBrush"         => "PrimaryBg",
        "PrimaryBubbleBorderBrush"   => "PrimaryBorder",
        "SecondaryBubbleBrush"       => "SecondaryBg",
        "SecondaryBubbleBorderBrush" => "SecondaryBorder",
        "TertiaryBubbleBrush"        => "TertiaryBg",
        "TertiaryBubbleBorderBrush"  => "TertiaryBorder",
        _                            => key.Replace("Brush", "")
    };

    /// <summary>Maps an OXSUIT colour key (e.g. "ContentBg") back to its WPF resource key ("ContentBgBrush").</summary>
    public static string OxsuitKeyToWpf(string key) => key switch
    {
        "PrimaryBg"           => "PrimaryBubbleBrush",
        "PrimaryBorder"       => "PrimaryBubbleBorderBrush",
        "SecondaryBg"         => "SecondaryBubbleBrush",
        "SecondaryBorder"     => "SecondaryBubbleBorderBrush",
        "TertiaryBg"          => "TertiaryBubbleBrush",
        "TertiaryBorder"      => "TertiaryBubbleBorderBrush",
        _                     => key + "Brush"
    };

    /// <summary>Parses an OXSUIT web-standard hex colour (#RRGGBB or #RRGGBBAA, alpha last).</summary>
    public static Color ParseOxsuitHex(string hex)
    {
        hex = hex.TrimStart('#');
        return hex.Length switch {
            3 => Color.FromRgb(
                    Convert.ToByte(new string(hex[0], 2), 16),
                    Convert.ToByte(new string(hex[1], 2), 16),
                    Convert.ToByte(new string(hex[2], 2), 16)),
            6 => Color.FromRgb(
                    Convert.ToByte(hex[..2], 16),
                    Convert.ToByte(hex[2..4], 16),
                    Convert.ToByte(hex[4..6], 16)),
            8 => Color.FromArgb(
                    Convert.ToByte(hex[6..8], 16),   // alpha is LAST in OXSUIT format
                    Convert.ToByte(hex[..2], 16),
                    Convert.ToByte(hex[2..4], 16),
                    Convert.ToByte(hex[4..6], 16)),
            _ => System.Windows.Media.Colors.Magenta
        };
    }
}
