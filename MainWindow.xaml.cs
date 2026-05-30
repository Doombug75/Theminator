namespace Theminator;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using Theminator.Models;
using IOPath = System.IO.Path;
using IODirectory = System.IO.Directory;
using IOFile = System.IO.File;

public partial class MainWindow : Window
{
    // ── State ──────────────────────────────────────────────────────────────────
    private ThemeModel _model = ThemeModel.ClaudesChoice();
    private AppSettings _settings = new();
    private string _themesFolder = string.Empty;

    // Left panel controls keyed by brush name
    private readonly Dictionary<string, (Rectangle Swatch, TextBlock HexLabel)> _leftControls = new();

    // Theme-info inputs (top of left panel)
    private TextBox _nameBox        = null!;
    private TextBox _authorBox      = null!;
    private TextBox _descriptionBox = null!;
    private bool    _updatingInfo;

    // ── Preview controls ───────────────────────────────────────────────────────
    // Content Surface
    private Border _pContentBorder = null!;
    private TextBlock _pContentHigh = null!, _pContentText = null!, _pContentDim = null!;

    // Sidebar Surface
    private Border _pSidebarBorder = null!;
    private TextBlock _pSidebarHigh = null!, _pSidebarText = null!, _pSidebarDim = null!;

    // Control Surface
    private Border _pControlBorder = null!, _pControlHoverBorder = null!;
    private TextBlock _pControlText = null!, _pControlDim = null!, _pControlHigh = null!;
    private TextBlock _pControlHoverLabel = null!;

    // Input Surface
    private Border _pInputBorder = null!;
    private TextBlock _pInputText = null!, _pInputDim = null!, _pInputHigh = null!;

    // Accent Colors
    private Border _pAccentBtnBorder = null!;
    private TextBlock _pAccentBtnText = null!;
    private Rectangle _pAccentHighRect = null!, _pPrimaryAccentRect = null!,
                      _pSecondaryAccentRect = null!, _pTertiaryAccentRect = null!;

    // Chat Bubbles — each gets its own border + Text / Dim / High
    private Border _pBubble1 = null!, _pBubble2 = null!, _pBubble3 = null!;
    private TextBlock _pBubble1Text = null!, _pBubble1Dim = null!, _pBubble1High = null!;
    private TextBlock _pBubble2Text = null!, _pBubble2Dim = null!, _pBubble2High = null!;
    private TextBlock _pBubble3Text = null!, _pBubble3Dim = null!, _pBubble3High = null!;
    private Border _pAccentAreaBorder = null!;

    // Extended preview fields
    private Rectangle  _pNavActiveMark       = null!;
    private Border     _pNavActiveItemBorder  = null!;
    private Border     _pInputBorder2         = null!;
    private Ellipse    _pBubble1Avatar        = null!, _pBubble2Avatar = null!, _pBubble3Avatar = null!;
    private TextBlock  _pBubble1Name          = null!, _pBubble2Name  = null!, _pBubble3Name   = null!;

    // Mock-window frame elements
    private Border     _pWindowFrame   = null!;
    private Border     _pFakeTitleBar  = null!;
    private TextBlock  _pFakeTitleText = null!;
    private Border     _pShadowBar     = null!;  // contact shadow below the frame
    // Inner element shadows (DropShadowEffect works inside _pWindowFrame's clip)
    private readonly List<System.Windows.Media.Effects.DropShadowEffect> _elementShadows = new();

    // Geometry sliders (left panel)
    private Slider _cornerRadiusSlider = null!;
    private Slider _shadowDepthSlider  = null!;
    private readonly Dictionary<string, Slider> _thicknessSliders = new();
    private bool _updatingSliders;

    // Accordion state
    private string? _expandedGroupName;
    private readonly Dictionary<string, (Border Body, TextBlock Chevron, TextBlock NameLabel)> _groupCards = new();
    private readonly Dictionary<string, (string[] Keys, Rectangle[] Dots)> _groupDots = new();

    // Randomize confirmation cooldown (skip dialog if < 30 s since last confirm)
    private DateTime?  _lastRandomizeConfirmed;

    // ── Init ───────────────────────────────────────────────────────────────────

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        SourceInitialized += (_, _) => ApplyTitleBarTheme(this);
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Show version in toolbar and title bar
        var asm    = System.Reflection.Assembly.GetExecutingAssembly();
        var ver    = asm.GetName().Version;
        var verStr = ver != null ? $"v{ver.Major}.{ver.Minor}.{ver.Build}" : "v1.0.0";
        VersionLabel.Text = verStr;
        Title = $"OXSUIT Theminator {verStr}";

        _settings = AppSettings.Load();
        _themesFolder = _settings.ResolveThemesFolder();
        StatusFolderText.Text = string.IsNullOrEmpty(_themesFolder)
            ? "No themes folder configured"
            : _themesFolder;

        BuildLeftPanel();
        BuildPreviewPanel();

        // Try to restore the last-used theme; fall back to default on any failure
        if (!string.IsNullOrEmpty(_themesFolder) && !string.IsNullOrEmpty(_settings.LastThemeName))
        {
            var oxsuitPath = IOPath.Combine(_themesFolder, _settings.LastThemeName + ".oxsuit");
            var xamlPath   = IOPath.Combine(_themesFolder, _settings.LastThemeName + ".xaml");
            var path       = IOFile.Exists(oxsuitPath) ? oxsuitPath : xamlPath;
            var loaded = ThemeLoader.Load(path);
            if (loaded != null)
                _model = loaded;
            else if (IOPath.Exists(path))
                // File exists but couldn't be parsed — surface a hint in the status bar
                StatusFolderText.Text = $"⚠ Could not restore '{_settings.LastThemeName}' — using default  |  {_themesFolder}";
        }

        ApplyModel();
    }

    // ── Left panel construction ────────────────────────────────────────────────

    private void BuildLeftPanel()
    {
        LeftPanel.Children.Clear();
        _leftControls.Clear();

        // ── Theme Info section ────────────────────────────────────────────────
        LeftPanel.Children.Add(new TextBlock
        {
            Text  = "THEME INFO",
            Style = (Style)FindResource("GroupHeaderStyle")
        });
        LeftPanel.Children.Add(MakeInfoRow("Name",        out _nameBox,        "Theme display name"));
        LeftPanel.Children.Add(MakeInfoRow("Author",      out _authorBox,      "Your name or alias"));
        LeftPanel.Children.Add(MakeInfoRow("Description", out _descriptionBox, "Short description of this theme"));

        // Wire live model updates (guard with _updatingInfo so ApplyModel won't re-write)
        _nameBox.TextChanged        += (_, _) => { if (!_updatingInfo) _model.Name        = _nameBox.Text; };
        _authorBox.TextChanged      += (_, _) => { if (!_updatingInfo) _model.Author      = _authorBox.Text; };
        _descriptionBox.TextChanged += (_, _) => { if (!_updatingInfo) _model.Description = _descriptionBox.Text; };

        // ── Randomize section ─────────────────────────────────────────────────
        LeftPanel.Children.Add(new Border
        {
            Height     = 1,
            Background = new SolidColorBrush(Color.FromRgb(0x1C, 0x23, 0x33)),
            Margin     = new Thickness(0, 8, 0, 4)
        });
        LeftPanel.Children.Add(new TextBlock
        {
            Text  = "RANDOMIZE",
            Style = (Style)FindResource("GroupHeaderStyle")
        });

        Button MakeRandBtn(string label, string tip, Action onClick)
        {
            var btn = new Button
            {
                Content    = label,
                ToolTip    = tip,
                Style      = (Style)FindResource("ToolbarButtonStyle"),
                Margin     = new Thickness(0, 0, 4, 0),
                Padding    = new Thickness(8, 4, 8, 4),
                FontSize   = 11
            };
            btn.Click += (_, _) => onClick();
            return btn;
        }

        var randRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 4) };
        randRow.Children.Add(MakeRandBtn("🎲 Dark",  "Randomize all surfaces in dark mode",    () => { if (ConfirmRandomize()) RandomizeTheme(RandomizeMode.Dark);  }));
        randRow.Children.Add(MakeRandBtn("🎲 Mid",   "Randomize all surfaces in mid-tone mode", () => { if (ConfirmRandomize()) RandomizeTheme(RandomizeMode.Mid);   }));
        randRow.Children.Add(MakeRandBtn("🎲 Light", "Randomize all surfaces in light mode",   () => { if (ConfirmRandomize()) RandomizeTheme(RandomizeMode.Light); }));
        LeftPanel.Children.Add(randRow);

        // ── Geometry section ──────────────────────────────────────────────────
        LeftPanel.Children.Add(new Border
        {
            Height = 1, Background = new SolidColorBrush(Color.FromRgb(0x1C, 0x23, 0x33)),
            Margin = new Thickness(0, 4, 0, 4)
        });
        LeftPanel.Children.Add(new TextBlock
        {
            Text  = "GEOMETRY",
            Style = (Style)FindResource("GroupHeaderStyle")
        });

        var crRow = MakeSliderRow("Corner radius", 20, out _cornerRadiusSlider);
        var sdRow = MakeSliderRow("Shadow depth",  12, out _shadowDepthSlider);
        _cornerRadiusSlider.ValueChanged += (_, e) => { if (!_updatingSliders) { _model.CornerRadius = e.NewValue; UpdatePreview(); } };
        _shadowDepthSlider.ValueChanged  += (_, e) => { if (!_updatingSliders) { _model.ShadowDepth  = e.NewValue; UpdatePreview(); } };
        LeftPanel.Children.Add(crRow);
        LeftPanel.Children.Add(sdRow);

        // ── Accordion colour groups ───────────────────────────────────────────
        LeftPanel.Children.Add(new Border
        {
            Height     = 1,
            Background = new SolidColorBrush(Color.FromRgb(0x1C, 0x23, 0x33)),
            Margin     = new Thickness(0, 4, 0, 6)
        });

        _groupCards.Clear();
        _groupDots.Clear();

        foreach (var (groupName, keys) in ThemeModel.Groups)
            BuildGroupCard(groupName, keys);
    }

    private void BuildGroupCard(string groupName, string[] keys)
    {
        // ── Mini header dots ──────────────────────────────────────────────────
        var dotCount = Math.Min(keys.Length, 6);
        var dots     = new Rectangle[dotCount];
        var dotPanel = new StackPanel
        {
            Orientation       = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Margin            = new Thickness(8, 0, 0, 0)
        };
        for (int i = 0; i < dotCount; i++)
        {
            dots[i] = new Rectangle
            {
                Width               = 10, Height = 10,
                RadiusX             = 2,  RadiusY = 2,
                Margin              = new Thickness(0, 0, 3, 0),
                SnapsToDevicePixels = true
            };
            dotPanel.Children.Add(dots[i]);
        }
        _groupDots[groupName] = (keys[..dotCount], dots);

        // ── Chevron + name ────────────────────────────────────────────────────
        var chevron = new TextBlock
        {
            Text              = "▶",
            FontSize          = 9,
            Foreground        = new SolidColorBrush(Color.FromRgb(0x18, 0xC0, 0xB4)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin            = new Thickness(0, 0, 7, 0)
        };

        var nameLabel = new TextBlock
        {
            Text              = groupName.ToUpper(),
            FontSize          = 10,
            FontWeight        = FontWeights.Bold,
            FontFamily        = new FontFamily("Segoe UI"),
            Foreground        = new SolidColorBrush(Color.FromRgb(0x6E, 0x80, 0x94)),
            VerticalAlignment = VerticalAlignment.Center
        };

        var headerRow = new StackPanel { Orientation = Orientation.Horizontal };
        headerRow.Children.Add(chevron);
        headerRow.Children.Add(nameLabel);
        headerRow.Children.Add(dotPanel);

        var headerBorder = new Border
        {
            Padding    = new Thickness(8, 6, 8, 6),
            Cursor     = Cursors.Hand,
            Child      = headerRow
        };
        headerBorder.MouseEnter += (_, _) =>
            headerBorder.Background = new SolidColorBrush(Color.FromRgb(0x1C, 0x23, 0x33));
        headerBorder.MouseLeave += (_, _) =>
            headerBorder.Background = Brushes.Transparent;

        // ── Body: colour rows + thickness sliders ─────────────────────────────
        var bodyPanel = new StackPanel { Margin = new Thickness(4, 4, 4, 6) };

        foreach (var key in keys)
        {
            bodyPanel.Children.Add(CreateLeftRow(key));

            var be = Array.Find(ThemeModel.BorderEntries, e => e.BrushKey == key);
            if (be != default)
            {
                var captured  = be;
                var sliderRow = MakeSliderRow(captured.Label + " px", 8, out var sl);
                sl.ValueChanged += (_, e) =>
                {
                    if (!_updatingSliders)
                    {
                        _model.Thicknesses[captured.BrushKey] = e.NewValue;
                        UpdatePreview();
                    }
                };
                _thicknessSliders[captured.BrushKey] = sl;
                bodyPanel.Children.Add(sliderRow);
            }
        }

        var body = new Border
        {
            Child           = bodyPanel,
            Visibility      = Visibility.Collapsed,
            BorderBrush     = new SolidColorBrush(Color.FromRgb(0x24, 0x30, 0x44)),
            BorderThickness = new Thickness(0, 1, 0, 0)
        };

        // ── Toggle expand / collapse ──────────────────────────────────────────
        headerBorder.MouseDown += (_, _) =>
        {
            bool wasOpen = _expandedGroupName == groupName;
            CollapseAllGroups();
            if (!wasOpen)
            {
                body.Visibility      = Visibility.Visible;
                chevron.Text         = "▼";
                nameLabel.Foreground = new SolidColorBrush(Color.FromRgb(0xC8, 0xD8, 0xE8));
                _expandedGroupName   = groupName;
            }
        };

        // ── Card shell ────────────────────────────────────────────────────────
        var cardContent = new StackPanel();
        cardContent.Children.Add(headerBorder);
        cardContent.Children.Add(body);

        var card = new Border
        {
            BorderBrush     = new SolidColorBrush(Color.FromRgb(0x24, 0x30, 0x44)),
            BorderThickness = new Thickness(1),
            CornerRadius    = new CornerRadius(5),
            Margin          = new Thickness(0, 0, 0, 4),
            ClipToBounds    = true,
            Child           = cardContent
        };

        _groupCards[groupName] = (body, chevron, nameLabel);
        LeftPanel.Children.Add(card);
    }

    private void CollapseAllGroups()
    {
        foreach (var (_, (body, chevron, nameLbl)) in _groupCards)
        {
            body.Visibility  = Visibility.Collapsed;
            chevron.Text     = "▶";
            nameLbl.Foreground = new SolidColorBrush(Color.FromRgb(0x6E, 0x80, 0x94));
        }
        _expandedGroupName = null;
    }

    private static string OxsuitName(string key) => ThemeModel.WpfKeyToOxsuit(key);

    private Border CreateLeftRow(string key)
    {
        var swatch = new Rectangle
        {
            Width = 14, Height = 14,
            RadiusX = 2, RadiusY = 2,
            Margin = new Thickness(0, 0, 6, 0),
            Cursor = Cursors.Hand,
            SnapsToDevicePixels = true
        };
        swatch.MouseDown += (_, _) => OpenColorPicker(key);

        var keyLabel = new TextBlock
        {
            Text = OxsuitName(key),
            FontSize = 11,
            FontFamily = new FontFamily("Segoe UI"),
            Foreground = new SolidColorBrush(Color.FromRgb(0xA8, 0xB8, 0xCC)),
            VerticalAlignment = VerticalAlignment.Center,
            Width = 160,
            TextTrimming = TextTrimming.CharacterEllipsis,
            ToolTip = ThemeModel.Descriptions.TryGetValue(key, out var desc) ? desc : key
        };

        var hexLabel = new TextBlock
        {
            FontSize = 10,
            FontFamily = new FontFamily("Consolas"),
            Foreground = new SolidColorBrush(Color.FromRgb(0x68, 0x78, 0x88)),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var sp = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        sp.Children.Add(swatch);
        sp.Children.Add(keyLabel);
        sp.Children.Add(hexLabel);

        var row = new Border
        {
            Padding = new Thickness(4, 3, 4, 3),
            CornerRadius = new CornerRadius(3),
            Background = Brushes.Transparent,
            Child = sp,
            Cursor = Cursors.Hand
        };
        row.MouseEnter += (_, _) => row.Background = new SolidColorBrush(Color.FromArgb(80, 0x24, 0x30, 0x44));
        row.MouseLeave += (_, _) => row.Background = Brushes.Transparent;
        row.MouseDown += (_, _) => OpenColorPicker(key);

        _leftControls[key] = (swatch, hexLabel);
        return row;
    }

    // ── Preview panel construction ─────────────────────────────────────────────

    private void BuildPreviewPanel()
    {
        PreviewPanel.Children.Clear();
        _elementShadows.Clear();

        // ── Oxminator mascot — round elevated frame ───────────────────────────
        try
        {
            var bmp = new System.Windows.Media.Imaging.BitmapImage(
                new Uri("pack://application:,,,/Assets/Oxminator_256.png"));
            var img = new System.Windows.Controls.Image
            {
                Source = bmp,
                Width  = 108,
                Height = 108
            };
            System.Windows.Media.RenderOptions.SetBitmapScalingMode(
                img, System.Windows.Media.BitmapScalingMode.HighQuality);

            // Round clip + elevated frame
            var frame = new Border
            {
                Width               = 116,
                Height              = 116,
                CornerRadius        = new CornerRadius(58),
                Background          = new SolidColorBrush(Color.FromRgb(0x1C, 0x23, 0x33)),
                BorderBrush         = new SolidColorBrush(Color.FromRgb(0x18, 0xC0, 0xB4)),
                BorderThickness     = new Thickness(2),
                ClipToBounds        = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin              = new Thickness(0, 0, 0, 10),
                Effect              = new System.Windows.Media.Effects.DropShadowEffect
                    { ShadowDepth = 5, BlurRadius = 14, Color = Colors.Black, Opacity = 0.45 },
                Child = new Border
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center,
                    Child = img
                }
            };
            PreviewPanel.Children.Add(frame);
        }
        catch { /* logo not critical */ }

        PreviewPanel.Children.Add(new TextBlock
        {
            Text                = "LIVE PREVIEW  ·  click any colour to edit it",
            FontSize            = 11,
            FontWeight          = FontWeights.SemiBold,
            Foreground          = new SolidColorBrush(Color.FromRgb(0x4D, 0x62, 0x78)),
            FontFamily          = new FontFamily("Segoe UI"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin              = new Thickness(0, 0, 0, 14)
        });

        // ── Fake title bar ────────────────────────────────────────────────────
        _pFakeTitleText = MakePreviewText("OXMINATOR", 12, bold: true);
        _pFakeTitleText.VerticalAlignment = VerticalAlignment.Center;
        MakeClickable(_pFakeTitleText, "SidebarTextBrush");

        var titleDots = new TextBlock
        {
            Text = "● ● ●", FontSize = 9, Foreground = new SolidColorBrush(Color.FromRgb(0x68, 0x78, 0x88)),
            VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0)
        };
        MakeClickable(titleDots, "SidebarDimBrush");

        var winBtns = new TextBlock
        {
            Text = "─  □  ✕", FontSize = 9, Foreground = new SolidColorBrush(Color.FromRgb(0x68, 0x78, 0x88)),
            VerticalAlignment = VerticalAlignment.Center
        };
        MakeClickable(winBtns, "SidebarDimBrush");

        var titleGrid = new Grid { Margin = new Thickness(10, 0, 10, 0) };
        titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(titleDots,       0);
        Grid.SetColumn(_pFakeTitleText, 1);
        Grid.SetColumn(winBtns,         2);
        titleGrid.Children.Add(titleDots);
        titleGrid.Children.Add(_pFakeTitleText);
        titleGrid.Children.Add(winBtns);

        _pFakeTitleBar = new Border
        {
            Height          = 32,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child           = titleGrid
        };
        MakeBgClickable(_pFakeTitleBar, "SidebarBgBrush");

        // ── Sidebar panel ─────────────────────────────────────────────────────
        _pNavActiveMark = new Rectangle
        {
            Width = 3, RadiusX = 1.5, RadiusY = 1.5,
            Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Stretch
        };
        _pSidebarHigh = MakePreviewText("Home", 12, bold: true);
        _pSidebarHigh.Margin = new Thickness(0);
        MakeClickable(_pNavActiveMark, "SidebarHighBrush");
        MakeClickable(_pSidebarHigh,   "SidebarHighBrush");

        var activeInner = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        activeInner.Children.Add(_pNavActiveMark);
        activeInner.Children.Add(_pSidebarHigh);

        _pNavActiveItemBorder = new Border
        {
            CornerRadius = new CornerRadius(4), Padding = new Thickness(4, 5, 8, 5),
            Margin = new Thickness(0, 0, 0, 2), Child = activeInner
        };
        MakeBgClickable(_pNavActiveItemBorder, "ControlHoverBrush");

        _pSidebarText = MakePreviewText("  Projects", 12);
        _pSidebarText.Margin = new Thickness(0, 2, 0, 2);
        _pSidebarDim = MakePreviewText("  Archive", 11);
        _pSidebarDim.Margin = new Thickness(0, 0, 0, 8);
        MakeClickable(_pSidebarText, "SidebarTextBrush");
        MakeClickable(_pSidebarDim,  "SidebarDimBrush");

        // Control card inside the sidebar
        _pControlText = MakePreviewText("ControlText", 11);
        _pControlDim  = MakePreviewText("ControlDim",  10);
        _pControlHigh = MakePreviewText("✦ High",      11, bold: true);
        _pControlText.Margin = _pControlDim.Margin = _pControlHigh.Margin = new Thickness(0, 0, 0, 2);
        MakeClickable(_pControlText, "ControlTextBrush");
        MakeClickable(_pControlDim,  "ControlDimBrush");
        MakeClickable(_pControlHigh, "ControlHighBrush");

        var ctrlSp = new StackPanel();
        ctrlSp.Children.Add(_pControlText);
        ctrlSp.Children.Add(_pControlDim);
        ctrlSp.Children.Add(_pControlHigh);

        _pControlBorder = new Border
        {
            Padding = new Thickness(8, 6, 8, 6), CornerRadius = new CornerRadius(5),
            BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 0, 4), Child = ctrlSp
        };
        MakeBgClickable(_pControlBorder, "ControlBgBrush");
        { var s = new System.Windows.Media.Effects.DropShadowEffect { Direction=315, ShadowDepth=0, BlurRadius=4, Color=Colors.Black, Opacity=0 }; _pControlBorder.Effect = s; _elementShadows.Add(s); }

        _pControlHoverLabel = MakePreviewText("Hover state", 10);
        MakeClickable(_pControlHoverLabel, "ControlHoverBrush");
        _pControlHoverBorder = new Border
        {
            Padding = new Thickness(8, 5, 8, 5), CornerRadius = new CornerRadius(5),
            Child = _pControlHoverLabel
        };
        MakeBgClickable(_pControlHoverBorder, "ControlHoverBrush");

        var sidebarContent = new StackPanel { Margin = new Thickness(6) };
        sidebarContent.Children.Add(_pNavActiveItemBorder);
        sidebarContent.Children.Add(_pSidebarText);
        sidebarContent.Children.Add(_pSidebarDim);
        sidebarContent.Children.Add(_pControlBorder);
        sidebarContent.Children.Add(_pControlHoverBorder);

        _pSidebarBorder = new Border { Width = 136, BorderThickness = new Thickness(0, 0, 1, 0), Child = sidebarContent };
        MakeBgClickable(_pSidebarBorder, "SidebarBgBrush");

        // ── Content area ──────────────────────────────────────────────────────
        _pContentHigh = MakePreviewText("✦  ContentHigh  —  headline / icon", 12, bold: true);
        _pContentText = MakePreviewText("ContentText: Normal body text in the main content area.", 12);
        _pContentText.TextWrapping = TextWrapping.Wrap;
        _pContentText.HorizontalAlignment = HorizontalAlignment.Stretch;
        _pContentDim  = MakePreviewText("ContentDim: Subdued metadata · secondary info", 11);
        MakeClickable(_pContentHigh, "ContentHighBrush");
        MakeClickable(_pContentText, "ContentTextBrush");
        MakeClickable(_pContentDim,  "ContentDimBrush");

        var contentHeader = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
        contentHeader.Children.Add(_pContentHigh);
        contentHeader.Children.Add(_pContentText);
        contentHeader.Children.Add(_pContentDim);

        // Primary bubble
        _pBubble1Avatar = new Ellipse { Width = 8, Height = 8, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center };
        _pBubble1Name   = MakePreviewText("Agent Alpha", 11, bold: true); _pBubble1Name.Margin = new Thickness(0);
        MakeClickable(_pBubble1Avatar, "PrimaryHighBrush");
        MakeClickable(_pBubble1Name,   "PrimaryHighBrush");
        var hdr1 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
        hdr1.Children.Add(_pBubble1Avatar); hdr1.Children.Add(_pBubble1Name);

        _pBubble1Text = MakePreviewText("Primary slot message content. PrimaryText for readable body.", 12);
        _pBubble1Text.TextWrapping = TextWrapping.Wrap; _pBubble1Text.HorizontalAlignment = HorizontalAlignment.Stretch; _pBubble1Text.Margin = new Thickness(0, 0, 0, 2);
        _pBubble1Dim  = MakePreviewText("Agent Alpha · just now  ·  PrimaryDim", 10);
        _pBubble1High = MakePreviewText("✦ PrimaryHigh", 10); _pBubble1High.Margin = new Thickness(0, 3, 0, 0);
        MakeClickable(_pBubble1Text, "PrimaryTextBrush");
        MakeClickable(_pBubble1Dim,  "PrimaryDimBrush");
        MakeClickable(_pBubble1High, "PrimaryHighBrush");
        var b1sp = new StackPanel();
        b1sp.Children.Add(hdr1); b1sp.Children.Add(_pBubble1Text); b1sp.Children.Add(_pBubble1Dim); b1sp.Children.Add(_pBubble1High);
        _pBubble1 = new Border { Padding = new Thickness(10, 8, 10, 8), CornerRadius = new CornerRadius(6), BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 0, 6), Child = b1sp };
        MakeBgClickable(_pBubble1, "PrimaryBubbleBrush");
        { var s = new System.Windows.Media.Effects.DropShadowEffect { Direction=315, ShadowDepth=0, BlurRadius=4, Color=Colors.Black, Opacity=0 }; _pBubble1.Effect = s; _elementShadows.Add(s); }

        // Secondary bubble
        _pBubble2Avatar = new Ellipse { Width = 8, Height = 8, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center };
        _pBubble2Name   = MakePreviewText("Agent Beta", 11, bold: true); _pBubble2Name.Margin = new Thickness(0);
        MakeClickable(_pBubble2Avatar, "SecondaryHighBrush");
        MakeClickable(_pBubble2Name,   "SecondaryHighBrush");
        var hdr2 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
        hdr2.Children.Add(_pBubble2Avatar); hdr2.Children.Add(_pBubble2Name);

        _pBubble2Text = MakePreviewText("Secondary slot message. SecondaryText on a different surface.", 12);
        _pBubble2Text.TextWrapping = TextWrapping.Wrap; _pBubble2Text.HorizontalAlignment = HorizontalAlignment.Stretch; _pBubble2Text.Margin = new Thickness(0, 0, 0, 2);
        _pBubble2Dim  = MakePreviewText("Agent Beta · just now  ·  SecondaryDim", 10);
        _pBubble2High = MakePreviewText("✦ SecondaryHigh", 10); _pBubble2High.Margin = new Thickness(0, 3, 0, 0);
        MakeClickable(_pBubble2Text, "SecondaryTextBrush");
        MakeClickable(_pBubble2Dim,  "SecondaryDimBrush");
        MakeClickable(_pBubble2High, "SecondaryHighBrush");
        var b2sp = new StackPanel();
        b2sp.Children.Add(hdr2); b2sp.Children.Add(_pBubble2Text); b2sp.Children.Add(_pBubble2Dim); b2sp.Children.Add(_pBubble2High);
        _pBubble2 = new Border { Padding = new Thickness(10, 8, 10, 8), CornerRadius = new CornerRadius(6), BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 0, 6), Child = b2sp };
        MakeBgClickable(_pBubble2, "SecondaryBubbleBrush");
        { var s = new System.Windows.Media.Effects.DropShadowEffect { Direction=315, ShadowDepth=0, BlurRadius=4, Color=Colors.Black, Opacity=0 }; _pBubble2.Effect = s; _elementShadows.Add(s); }

        // Tertiary bubble
        _pBubble3Avatar = new Ellipse { Width = 8, Height = 8, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center };
        _pBubble3Name   = MakePreviewText("System", 11, bold: true); _pBubble3Name.Margin = new Thickness(0);
        MakeClickable(_pBubble3Avatar, "TertiaryHighBrush");
        MakeClickable(_pBubble3Name,   "TertiaryHighBrush");
        var hdr3 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
        hdr3.Children.Add(_pBubble3Avatar); hdr3.Children.Add(_pBubble3Name);

        _pBubble3Text = MakePreviewText("Tertiary slot — system notification or status message.", 12);
        _pBubble3Text.TextWrapping = TextWrapping.Wrap; _pBubble3Text.HorizontalAlignment = HorizontalAlignment.Stretch; _pBubble3Text.Margin = new Thickness(0, 0, 0, 2);
        _pBubble3Dim  = MakePreviewText("System · TertiaryDim", 10);
        _pBubble3High = MakePreviewText("✦ TertiaryHigh", 10); _pBubble3High.Margin = new Thickness(0, 3, 0, 0);
        MakeClickable(_pBubble3Text, "TertiaryTextBrush");
        MakeClickable(_pBubble3Dim,  "TertiaryDimBrush");
        MakeClickable(_pBubble3High, "TertiaryHighBrush");
        var b3sp = new StackPanel();
        b3sp.Children.Add(hdr3); b3sp.Children.Add(_pBubble3Text); b3sp.Children.Add(_pBubble3Dim); b3sp.Children.Add(_pBubble3High);
        _pBubble3 = new Border { Padding = new Thickness(10, 8, 10, 8), CornerRadius = new CornerRadius(6), BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 0, 0), Child = b3sp };
        MakeBgClickable(_pBubble3, "TertiaryBubbleBrush");
        { var s = new System.Windows.Media.Effects.DropShadowEffect { Direction=315, ShadowDepth=0, BlurRadius=4, Color=Colors.Black, Opacity=0 }; _pBubble3.Effect = s; _elementShadows.Add(s); }

        var contentBody = new StackPanel { Margin = new Thickness(8, 8, 8, 8) };
        contentBody.Children.Add(contentHeader);
        contentBody.Children.Add(_pBubble1);
        contentBody.Children.Add(_pBubble2);
        contentBody.Children.Add(_pBubble3);

        _pContentBorder = new Border { Child = contentBody };
        MakeBgClickable(_pContentBorder, "ContentBgBrush");

        // ── Input bar ─────────────────────────────────────────────────────────
        _pInputText = MakePreviewText("Hello, this is typed text…", 12);
        _pInputDim  = MakePreviewText("InputDim: placeholder", 11);
        _pInputHigh = MakePreviewText("✦ InputHigh", 11);
        _pInputText.Margin = new Thickness(0);
        MakeClickable(_pInputText, "InputTextBrush");
        MakeClickable(_pInputDim,  "InputDimBrush");
        MakeClickable(_pInputHigh, "InputHighBrush");

        _pInputBorder = new Border
        {
            Padding = new Thickness(10, 6, 10, 6), CornerRadius = new CornerRadius(5),
            BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 6, 0), Child = _pInputText
        };
        MakeBgClickable(_pInputBorder, "InputBgBrush");

        _pAccentBtnText = MakePreviewText("Send", 12, bold: true);
        _pAccentBtnText.Margin = new Thickness(0);
        MakeClickable(_pAccentBtnText, "AccentTextBrush");

        _pAccentBtnBorder = new Border
        {
            Padding = new Thickness(14, 6, 14, 6), CornerRadius = new CornerRadius(5),
            Cursor = Cursors.Hand, ToolTip = "Edit: AccentBg", Child = _pAccentBtnText
        };
        _pAccentBtnBorder.MouseDown  += (_, e) => { OpenColorPicker("AccentBgBrush"); e.Handled = true; };
        _pAccentBtnBorder.MouseEnter += (_, _) => _pAccentBtnBorder.Opacity = 0.78;
        _pAccentBtnBorder.MouseLeave += (_, _) => _pAccentBtnBorder.Opacity = 1.0;
        MakeBgClickable(_pAccentBtnBorder, "AccentBgBrush");
        { var s = new System.Windows.Media.Effects.DropShadowEffect { Direction=315, ShadowDepth=0, BlurRadius=4, Color=Colors.Black, Opacity=0 }; _pAccentBtnBorder.Effect = s; _elementShadows.Add(s); }

        // ── Input bar: typed-text row + dim/high samples below ───────────────
        var inputRowGrid = new Grid();
        inputRowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        inputRowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(_pInputBorder,     0);
        Grid.SetColumn(_pAccentBtnBorder, 1);
        inputRowGrid.Children.Add(_pInputBorder);
        inputRowGrid.Children.Add(_pAccentBtnBorder);

        // Dim + High shown on their own InputBg line below the text-entry row
        var inputSamplesRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin      = new Thickness(0, 5, 0, 0)
        };
        inputSamplesRow.Children.Add(_pInputDim);
        inputSamplesRow.Children.Add(new TextBlock
        {
            Text              = "  ·  ",
            FontSize          = 10,
            FontFamily        = new FontFamily("Segoe UI"),
            Foreground        = new SolidColorBrush(Color.FromRgb(0x4D, 0x62, 0x78)),
            VerticalAlignment = VerticalAlignment.Center
        });
        inputSamplesRow.Children.Add(_pInputHigh);

        var inputInner = new StackPanel();
        inputInner.Children.Add(inputRowGrid);
        inputInner.Children.Add(inputSamplesRow);

        _pInputBorder2 = new Border
        {
            Padding         = new Thickness(8, 6, 8, 8),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Child           = inputInner
        };
        MakeBgClickable(_pInputBorder2, "InputBgBrush");

        // ── Accent footer — lives inside the window frame below input bar ─────
        _pAccentHighRect      = new Rectangle { Width = 36, Height = 18, RadiusX = 3, RadiusY = 3, Cursor = Cursors.Hand };
        _pPrimaryAccentRect   = new Rectangle { Width = 36, Height = 18, RadiusX = 3, RadiusY = 3, Cursor = Cursors.Hand };
        _pSecondaryAccentRect = new Rectangle { Width = 36, Height = 18, RadiusX = 3, RadiusY = 3, Cursor = Cursors.Hand };
        _pTertiaryAccentRect  = new Rectangle { Width = 36, Height = 18, RadiusX = 3, RadiusY = 3, Cursor = Cursors.Hand };
        MakeClickable(_pAccentHighRect,      "AccentHighlightBrush");
        MakeClickable(_pPrimaryAccentRect,   "PrimaryAccentBrush");
        MakeClickable(_pSecondaryAccentRect, "SecondaryAccentBrush");
        MakeClickable(_pTertiaryAccentRect,  "TertiaryAccentBrush");

        StackPanel AccentCol(Rectangle rect, string lbl)
        {
            var tb = new TextBlock
            {
                Text                = lbl,
                FontSize            = 9,
                FontFamily          = new FontFamily("Segoe UI"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin              = new Thickness(0, 2, 0, 0)
            };
            MakeClickable(tb, lbl.Replace(" ", "") + "Brush");   // best-effort; no-op if key not found
            var sp = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
            sp.Children.Add(rect);
            sp.Children.Add(tb);
            return sp;
        }

        var accentSwatches = new StackPanel { Orientation = Orientation.Horizontal };
        accentSwatches.Children.Add(AccentCol(_pAccentHighRect,      "AccentHigh"));
        accentSwatches.Children.Add(AccentCol(_pPrimaryAccentRect,   "PrimaryAccent"));
        accentSwatches.Children.Add(AccentCol(_pSecondaryAccentRect, "SecondaryAccent"));
        accentSwatches.Children.Add(AccentCol(_pTertiaryAccentRect,  "TertiaryAccent"));

        var accentFooterLabel = new TextBlock
        {
            Text       = "ACCENT PALETTE",
            FontSize   = 9,
            FontWeight = FontWeights.Bold,
            FontFamily = new FontFamily("Segoe UI"),
            Foreground = new SolidColorBrush(Color.FromRgb(0x4D, 0x62, 0x78)),
            Margin     = new Thickness(0, 0, 0, 6)
        };

        var accentFooterContent = new StackPanel { Margin = new Thickness(0, 2, 0, 0) };
        accentFooterContent.Children.Add(accentFooterLabel);
        accentFooterContent.Children.Add(accentSwatches);

        _pAccentAreaBorder = new Border
        {
            Padding         = new Thickness(8, 8, 8, 10),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Child           = accentFooterContent
        };
        MakeBgClickable(_pAccentAreaBorder, "ContentBgBrush");

        // ── Assemble mock window ──────────────────────────────────────────────
        var bodyGrid = new Grid();
        bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(_pSidebarBorder, 0);
        Grid.SetColumn(_pContentBorder, 1);
        bodyGrid.Children.Add(_pSidebarBorder);
        bodyGrid.Children.Add(_pContentBorder);

        var windowGrid = new Grid();
        windowGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                      // 0 title bar
        windowGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 1 body
        windowGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                      // 2 input bar
        windowGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                      // 3 accent footer
        Grid.SetRow(_pFakeTitleBar,     0);
        Grid.SetRow(bodyGrid,           1);
        Grid.SetRow(_pInputBorder2,     2);
        Grid.SetRow(_pAccentAreaBorder, 3);
        windowGrid.Children.Add(_pFakeTitleBar);
        windowGrid.Children.Add(bodyGrid);
        windowGrid.Children.Add(_pInputBorder2);
        windowGrid.Children.Add(_pAccentAreaBorder);

        _pWindowFrame = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius    = new CornerRadius(7),
            ClipToBounds    = true,
            MinHeight       = 460,
            Margin          = new Thickness(8, 4, 8, 0),
            Child           = windowGrid
        };
        MakeBgClickable(_pWindowFrame, "ContentBgBrush");

        // Contact shadow: a gradient bar directly below the frame in normal
        // layout flow — immune to ScrollViewer clipping (it's a plain element).
        _pShadowBar = new Border
        {
            Height       = 0,
            CornerRadius = new CornerRadius(0, 0, 7, 7),
            Margin       = new Thickness(16, 0, 16, 12),
            IsHitTestVisible = false
        };

        // ── Add to panel ──────────────────────────────────────────────────────
        PreviewPanel.Children.Add(_pWindowFrame);
        PreviewPanel.Children.Add(_pShadowBar);
    }

    // ── Preview helpers ───────────────────────────────────────────────────────

    private static Border MakeSectionCard(UIElement content) => new Border
    {
        Padding         = new Thickness(12),
        CornerRadius    = new CornerRadius(6),
        BorderThickness = new Thickness(1),
        Margin          = new Thickness(0, 2, 0, 12),
        Child           = content
    };

    private static TextBlock MakePreviewText(string text, double size, bool bold = false) => new TextBlock
    {
        Text                = text,
        FontSize            = size,
        FontFamily          = new FontFamily("Segoe UI"),
        FontWeight          = bold ? FontWeights.SemiBold : FontWeights.Normal,
        Margin              = new Thickness(0, 2, 0, 3),
        // Left-align so clicking the empty background area to the right of text
        // falls through to the parent Border's MakeBgClickable handler.
        HorizontalAlignment = HorizontalAlignment.Left
    };

    /// <summary>Adds hand cursor, tooltip, hover-dim, and colour-picker click to any framework element.</summary>
    private void MakeClickable(FrameworkElement el, string key)
    {
        el.Cursor  = Cursors.Hand;
        el.ToolTip = $"Edit: {key.Replace("Brush", "")}";
        el.MouseDown  += (_, e) => { OpenColorPicker(key); e.Handled = true; };
        el.MouseEnter += (_, _) => el.Opacity = 0.65;
        el.MouseLeave += (_, _) => el.Opacity = 1.0;
    }

    /// <summary>Opens the colour picker for <paramref name="key"/> when the border background is clicked
    /// (only fires when no child element has already handled the event).</summary>
    private void MakeBgClickable(Border border, string key)
    {
        border.ToolTip    = $"Click background → Edit: {key.Replace("Brush", "")}";
        border.MouseDown += (_, e) => { if (!e.Handled) { OpenColorPicker(key); e.Handled = true; } };
    }

    private static TextBlock MakeSectionLabel(string text) => new TextBlock
    {
        Text       = text,
        FontSize   = 11,
        FontWeight = FontWeights.SemiBold,
        FontFamily = new FontFamily("Segoe UI"),
        Foreground = new SolidColorBrush(Color.FromRgb(0xA8, 0xB8, 0xCC)),
        Margin     = new Thickness(0, 0, 0, 3)
    };

    /// <summary>Creates a labelled 0–max slider row for the Geometry section.</summary>
    private static Border MakeSliderRow(string label, double max, out Slider slider)
    {
        slider = new Slider
        {
            Minimum             = 0,
            Maximum             = max,
            SmallChange         = 0.5,
            LargeChange         = 1,
            TickFrequency       = 0.5,
            IsSnapToTickEnabled = true,
            VerticalAlignment   = VerticalAlignment.Center
        };

        var valLbl = new TextBlock
        {
            Width             = 34,
            FontSize          = 10,
            FontFamily        = new FontFamily("Consolas"),
            Foreground        = new SolidColorBrush(Color.FromRgb(0x68, 0x78, 0x88)),
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment     = TextAlignment.Right
        };
        slider.ValueChanged += (_, e) => valLbl.Text = $"{e.NewValue:0.#}";

        var lbl2 = new TextBlock
        {
            Text              = label,
            Width             = 88,
            FontSize          = 10,
            FontFamily        = new FontFamily("Segoe UI"),
            Foreground        = new SolidColorBrush(Color.FromRgb(0x4D, 0x62, 0x78)),
            VerticalAlignment = VerticalAlignment.Center
        };

        var dock = new DockPanel { LastChildFill = true, Margin = new Thickness(4, 2, 4, 2) };
        DockPanel.SetDock(lbl2,   Dock.Left);
        DockPanel.SetDock(valLbl, Dock.Right);
        dock.Children.Add(lbl2);
        dock.Children.Add(valLbl);
        dock.Children.Add(slider);

        return new Border { Child = dock };
    }

    /// <summary>Creates a labelled text-box row for the Theme Info section.</summary>
    private static Border MakeInfoRow(string label, out TextBox box, string placeholder)
    {
        var lbl = new TextBlock
        {
            Text                = label,
            Width               = 72,
            FontSize            = 11,
            FontFamily          = new FontFamily("Segoe UI"),
            Foreground          = new SolidColorBrush(Color.FromRgb(0xA8, 0xB8, 0xCC)),
            VerticalAlignment   = VerticalAlignment.Center
        };

        box = new TextBox
        {
            FontSize            = 11,
            FontFamily          = new FontFamily("Segoe UI"),
            Background          = new SolidColorBrush(Color.FromRgb(0x0A, 0x0F, 0x18)),
            Foreground          = new SolidColorBrush(Color.FromRgb(0xD8, 0xE4, 0xF0)),
            CaretBrush          = new SolidColorBrush(Color.FromRgb(0x18, 0xC0, 0xB4)),
            BorderBrush         = new SolidColorBrush(Color.FromRgb(0x1C, 0x23, 0x33)),
            BorderThickness     = new Thickness(1),
            Padding             = new Thickness(5, 3, 5, 3),
            VerticalAlignment   = VerticalAlignment.Center,
            ToolTip             = placeholder
        };

        // Use a DockPanel so the TextBox fills the remaining width
        var dock = new DockPanel { LastChildFill = true, Margin = new Thickness(4, 3, 4, 3) };
        DockPanel.SetDock(lbl, Dock.Left);
        dock.Children.Add(lbl);
        dock.Children.Add(box);

        return new Border { Child = dock };
    }

    private static StackPanel MakeAccentSwatch(string label, out Rectangle rect)
    {
        rect = new Rectangle { Width = 40, Height = 20, RadiusX = 3, RadiusY = 3, Cursor = Cursors.Hand };
        var tb = new TextBlock
        {
            Text                = label.Replace("Brush", ""),
            FontSize            = 9,
            FontFamily          = new FontFamily("Segoe UI"),
            Foreground          = new SolidColorBrush(Color.FromRgb(0x68, 0x78, 0x88)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin              = new Thickness(0, 2, 0, 0)
        };
        var sp = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 8, 0) };
        sp.Children.Add(rect);
        sp.Children.Add(tb);
        return sp;
    }

    // ── Apply model to UI ─────────────────────────────────────────────────────

    private void ApplyModel()
    {
        ThemeNameDisplay.Text = _model.Name;

        // Populate info fields without triggering the TextChanged model-write
        _updatingInfo           = true;
        _nameBox.Text           = _model.Name;
        _authorBox.Text         = _model.Author;
        _descriptionBox.Text    = _model.Description;
        _updatingInfo           = false;

        // Populate geometry sliders
        _updatingSliders            = true;
        _cornerRadiusSlider.Value   = _model.CornerRadius;
        _shadowDepthSlider.Value    = _model.ShadowDepth;
        foreach (var (brushKey, _, _) in ThemeModel.BorderEntries)
            if (_thicknessSliders.TryGetValue(brushKey, out var sl))
                sl.Value = _model.GetThickness(brushKey);
        _updatingSliders            = false;

        UpdateLeftPanel();
        UpdatePreview();
        ApplyTitleBarTheme(this);
        _settings.LastThemeName = _model.Name;
        _settings.Save();
    }

    private void UpdateLeftPanel()
    {
        foreach (var key in ThemeModel.Keys)
        {
            if (!_leftControls.TryGetValue(key, out var ctrl)) continue;
            var c = GetColor(key);
            ctrl.Swatch.Fill   = new SolidColorBrush(c);
            ctrl.HexLabel.Text = ThemeModel.ToHex(c);
        }

        // Update accordion header dots so each card shows its current colours
        // even when its body is collapsed.
        foreach (var (_, (dotKeys, dots)) in _groupDots)
            for (int i = 0; i < dots.Length; i++)
                dots[i].Fill = new SolidColorBrush(GetColor(dotKeys[i]));
    }

    private void UpdatePreview()
    {
        // ── Geometry tokens ────────────────────────────────────────────────────
        var cr = _model.CornerRadius;
        _pWindowFrame.CornerRadius         = new CornerRadius(cr);
        _pBubble1.CornerRadius             = new CornerRadius(cr);
        _pBubble2.CornerRadius             = new CornerRadius(cr);
        _pBubble3.CornerRadius             = new CornerRadius(cr);
        _pControlBorder.CornerRadius       = new CornerRadius(cr);
        _pControlHoverBorder.CornerRadius  = new CornerRadius(cr);
        _pInputBorder.CornerRadius         = new CornerRadius(cr);
        _pAccentBtnBorder.CornerRadius     = new CornerRadius(cr);
        _pNavActiveItemBorder.CornerRadius  = new CornerRadius(Math.Min(cr, 4));

        var sd = _model.ShadowDepth;

        // Inner element shadows (bubbles, cards, send button)
        foreach (var eff in _elementShadows)
        {
            eff.ShadowDepth = sd < 0.5 ? 0 : Math.Min(sd * 0.7, 7);
            eff.BlurRadius  = sd < 0.5 ? 0 : Math.Max(2, sd * 1.5);
            eff.Opacity     = sd < 0.5 ? 0 : Math.Min(0.55, 0.08 + sd * 0.04);
        }

        // Contact shadow bar — teal-tinted so it's clearly visible on the dark
        // app background (#0D1117 ≈ near-black; pure black shadow is invisible there).
        _pShadowBar.Height = sd < 0.5 ? 0 : Math.Max(8, sd * 6);
        _pShadowBar.Margin = new Thickness(12 + sd * 2, 0, 12 + sd * 2, 12);
        var shadowAlpha    = sd < 0.5 ? (byte)0 : (byte)Math.Min(220, 60 + sd * 13);
        _pShadowBar.Background = new LinearGradientBrush(
            Color.FromArgb(shadowAlpha, 0x08, 0x08, 0x08),   // near-black top
            Color.FromArgb(0,           0x08, 0x08, 0x08),   // transparent bottom
            new Point(0, 0), new Point(0, 1));

        // Per-element border thicknesses
        _pBubble1.BorderThickness   = new Thickness(_model.GetThickness("PrimaryBubbleBorderBrush"));
        _pBubble2.BorderThickness   = new Thickness(_model.GetThickness("SecondaryBubbleBorderBrush"));
        _pBubble3.BorderThickness   = new Thickness(_model.GetThickness("TertiaryBubbleBorderBrush"));
        _pControlBorder.BorderThickness = new Thickness(_model.GetThickness("ControlBorderBrush"));
        _pInputBorder.BorderThickness   = new Thickness(_model.GetThickness("InputBorderBrush"));
        _pSidebarBorder.BorderThickness = new Thickness(0, 0, _model.GetThickness("SidebarBorderBrush"), 0);
        _pFakeTitleBar.BorderThickness  = new Thickness(0, 0, 0, _model.GetThickness("SidebarBorderBrush"));
        _pWindowFrame.BorderThickness   = new Thickness(_model.GetThickness("ContentBorderBrush"));

        // Window frame — Background must be set (non-null) for DropShadowEffect to render
        _pWindowFrame.Background     = Brush(GetColor("ContentBgBrush"));
        _pWindowFrame.BorderBrush    = Brush(GetColor("ContentBorderBrush"));

        // Fake title bar (SidebarBg surface)
        _pFakeTitleBar.Background    = Brush(GetColor("SidebarBgBrush"));
        _pFakeTitleBar.BorderBrush   = Brush(GetColor("SidebarBorderBrush"));
        _pFakeTitleText.Foreground   = Brush(GetColor("SidebarTextBrush"));

        // Sidebar panel
        _pSidebarBorder.Background        = Brush(GetColor("SidebarBgBrush"));
        _pSidebarBorder.BorderBrush       = Brush(GetColor("SidebarBorderBrush"));
        _pNavActiveMark.Fill              = Brush(GetColor("SidebarHighBrush"));
        _pNavActiveItemBorder.Background  = Brush(GetColor("ControlHoverBrush"));
        _pSidebarHigh.Foreground          = Brush(GetColor("SidebarHighBrush"));
        _pSidebarText.Foreground          = Brush(GetColor("SidebarTextBrush"));
        _pSidebarDim.Foreground           = Brush(GetColor("SidebarDimBrush"));

        // Control card (inside sidebar)
        _pControlBorder.Background      = Brush(GetColor("ControlBgBrush"));
        _pControlBorder.BorderBrush     = Brush(GetColor("ControlBorderBrush"));
        _pControlText.Foreground        = Brush(GetColor("ControlTextBrush"));
        _pControlDim.Foreground         = Brush(GetColor("ControlDimBrush"));
        _pControlHigh.Foreground        = Brush(GetColor("ControlHighBrush"));
        _pControlHoverBorder.Background = Brush(GetColor("ControlHoverBrush"));

        // Content area
        _pContentBorder.Background   = Brush(GetColor("ContentBgBrush"));
        _pContentHigh.Foreground     = Brush(GetColor("ContentHighBrush"));
        _pContentText.Foreground     = Brush(GetColor("ContentTextBrush"));
        _pContentDim.Foreground      = Brush(GetColor("ContentDimBrush"));

        // Input bar
        _pInputBorder2.Background    = Brush(GetColor("InputBgBrush"));
        _pInputBorder2.BorderBrush   = Brush(GetColor("InputBorderBrush"));
        _pInputBorder.Background     = Brush(GetColor("InputBgBrush"));
        _pInputBorder.BorderBrush    = Brush(GetColor("InputBorderBrush"));
        _pInputText.Foreground       = Brush(GetColor("InputTextBrush"));
        _pInputDim.Foreground        = Brush(GetColor("InputDimBrush"));
        _pInputHigh.Foreground       = Brush(GetColor("InputHighBrush"));

        // Send button (AccentBg)
        _pAccentBtnBorder.Background = Brush(GetColor("AccentBgBrush"));
        _pAccentBtnText.Foreground   = Brush(GetColor("AccentTextBrush"));

        // Accent footer (inside window frame, Row 3)
        _pAccentAreaBorder.Background  = Brush(GetColor("ContentBgBrush"));
        _pAccentAreaBorder.BorderBrush = Brush(GetColor("ContentBorderBrush"));
        _pAccentHighRect.Fill          = Brush(GetColor("AccentHighlightBrush"));
        _pPrimaryAccentRect.Fill       = Brush(GetColor("PrimaryAccentBrush"));
        _pSecondaryAccentRect.Fill     = Brush(GetColor("SecondaryAccentBrush"));
        _pTertiaryAccentRect.Fill      = Brush(GetColor("TertiaryAccentBrush"));

        // Primary bubble
        _pBubble1.Background      = Brush(GetColor("PrimaryBubbleBrush"));
        _pBubble1.BorderBrush     = Brush(GetColor("PrimaryBubbleBorderBrush"));
        _pBubble1Avatar.Fill      = Brush(GetColor("PrimaryHighBrush"));
        _pBubble1Name.Foreground  = Brush(GetColor("PrimaryHighBrush"));
        _pBubble1Text.Foreground  = Brush(GetColor("PrimaryTextBrush"));
        _pBubble1Dim.Foreground   = Brush(GetColor("PrimaryDimBrush"));
        _pBubble1High.Foreground  = Brush(GetColor("PrimaryHighBrush"));

        // Secondary bubble
        _pBubble2.Background      = Brush(GetColor("SecondaryBubbleBrush"));
        _pBubble2.BorderBrush     = Brush(GetColor("SecondaryBubbleBorderBrush"));
        _pBubble2Avatar.Fill      = Brush(GetColor("SecondaryHighBrush"));
        _pBubble2Name.Foreground  = Brush(GetColor("SecondaryHighBrush"));
        _pBubble2Text.Foreground  = Brush(GetColor("SecondaryTextBrush"));
        _pBubble2Dim.Foreground   = Brush(GetColor("SecondaryDimBrush"));
        _pBubble2High.Foreground  = Brush(GetColor("SecondaryHighBrush"));

        // Tertiary bubble
        _pBubble3.Background      = Brush(GetColor("TertiaryBubbleBrush"));
        _pBubble3.BorderBrush     = Brush(GetColor("TertiaryBubbleBorderBrush"));
        _pBubble3Avatar.Fill      = Brush(GetColor("TertiaryHighBrush"));
        _pBubble3Name.Foreground  = Brush(GetColor("TertiaryHighBrush"));
        _pBubble3Text.Foreground  = Brush(GetColor("TertiaryTextBrush"));
        _pBubble3Dim.Foreground   = Brush(GetColor("TertiaryDimBrush"));
        _pBubble3High.Foreground  = Brush(GetColor("TertiaryHighBrush"));
    }

    private Color GetColor(string key)
    {
        if (_model.Colors.TryGetValue(key, out var c)) return c;
        var fb = ThemeModel.ClaudesChoice();
        return fb.Colors.TryGetValue(key, out var fc) ? fc : Colors.Magenta;
    }

    private static SolidColorBrush Brush(Color c) => new(c);

    // ── Color picker ──────────────────────────────────────────────────────────

    private void OpenColorPicker(string key)
    {
        var current = GetColor(key);
        var dlg = new ColorPickerWindow(current, key) { Owner = this };
        ApplyTitleBarTheme(dlg);
        if (dlg.ShowDialog() == true)
        {
            _model.Colors[key] = dlg.ColorResult;
            if (_leftControls.TryGetValue(key, out var ctrl))
            {
                ctrl.Swatch.Fill = new SolidColorBrush(dlg.ColorResult);
                ctrl.HexLabel.Text = ThemeModel.ToHex(dlg.ColorResult);
            }
            UpdatePreview();
            // Title bar updates live when sidebar colors change
            if (key is "SidebarBgBrush" or "SidebarTextBrush")
                ApplyTitleBarTheme(this);
        }
    }

    // ── Toolbar handlers ──────────────────────────────────────────────────────

    private void BtnLoadTheme_Click(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu();

        if (!string.IsNullOrEmpty(_themesFolder) && IODirectory.Exists(_themesFolder))
        {
            var files = IODirectory.GetFiles(_themesFolder, "*.oxsuit")
                .OrderBy(IOPath.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (files.Count == 0)
            {
                menu.Items.Add(new MenuItem { Header = "(No themes found)", IsEnabled = false });
            }
            else
            {
                foreach (var file in files)
                {
                    var name = ThemeLoader.FormatName(ThemeLoader.NameFromPath(file));
                    var item = new MenuItem { Header = name };
                    var capturedFile = file;
                    item.Click += (_, _) =>
                    {
                        var loaded = ThemeLoader.Load(capturedFile);
                        if (loaded != null)
                        {
                            _model = loaded;
                            ApplyModel();
                        }
                        else
                        {
                            MessageBox.Show(this,
                                $"Could not load theme:\n\n{capturedFile}\n\n" +
                                "The file may be corrupt, empty, or not a valid OXSUIT theme.\n" +
                                "The current theme was not changed.",
                                "Theme Load Failed",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    };
                    menu.Items.Add(item);
                }
            }
        }
        else
        {
            menu.Items.Add(new MenuItem { Header = "(No themes folder set — use Options)", IsEnabled = false });
        }

        menu.PlacementTarget = BtnLoadTheme;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    private void BtnNew_Click(object sender, RoutedEventArgs e)
    {
        _model = ThemeModel.BlankTheme();
        ApplyModel();
    }

    private void BtnSaveAs_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_themesFolder))
        {
            MessageBox.Show(this, "Please set a themes folder first (Options).", "No Themes Folder",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Pre-fill with the name the user typed in the Name field
        var input = ShowInputDialog("Save Theme As", "Theme name:", _nameBox.Text.Trim() is { Length: > 0 } n ? n : _model.Name);
        if (input == null) return;

        input = input.Trim();
        if (string.IsNullOrEmpty(input))
        {
            MessageBox.Show(this, "Theme name cannot be empty.", "Invalid Name",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Sanitize filename
        foreach (var ch in IOPath.GetInvalidFileNameChars())
            input = input.Replace(ch, '_');

        var savePath = IOPath.Combine(_themesFolder, input + ".oxsuit");
        if (IOFile.Exists(savePath))
        {
            var confirm = MessageBox.Show(this,
                $"'{input}.oxsuit' already exists. Overwrite?",
                "Confirm Overwrite", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;
        }

        _model.Name = input;
        ThemeLoader.Save(_model, savePath);
        ApplyModel();
        MessageBox.Show(this, $"Theme saved to:\n{savePath}", "Saved",
            MessageBoxButton.OK, MessageBoxImage.None);
    }

    private void BtnAbout_Click(object sender, RoutedEventArgs e)
    {
        var asm    = System.Reflection.Assembly.GetExecutingAssembly();
        var ver    = asm.GetName().Version;
        var verStr = ver != null ? $"v{ver.Major}.{ver.Minor}.{ver.Build}" : "v1.0.0";

        var win = new Window
        {
            Title                 = "About OXSUIT Theminator",
            Width                 = 380,
            SizeToContent         = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner                 = this,
            Background            = new SolidColorBrush(Color.FromRgb(0x0D, 0x11, 0x17)),
            ResizeMode            = ResizeMode.NoResize,
            ShowInTaskbar         = false
        };
        win.SourceInitialized += (_, _) => ApplyTitleBarTheme(win);

        var panel = new StackPanel { Margin = new Thickness(28, 24, 28, 24) };

        // Oxminator logo
        try
        {
            var bmp = new System.Windows.Media.Imaging.BitmapImage(
                new Uri("pack://application:,,,/Assets/Oxminator_256.png"));
            var img = new System.Windows.Controls.Image
            {
                Source              = bmp,
                Width               = 96,
                Height              = 96,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin              = new Thickness(0, 0, 0, 14)
            };
            System.Windows.Media.RenderOptions.SetBitmapScalingMode(
                img, System.Windows.Media.BitmapScalingMode.HighQuality);
            panel.Children.Add(img);
        }
        catch { /* logo not critical */ }

        // App name
        panel.Children.Add(new TextBlock
        {
            Text                = "OXSUIT Theminator",
            FontSize            = 22, FontWeight = FontWeights.Bold,
            FontFamily          = new FontFamily("Segoe UI"),
            Foreground          = new SolidColorBrush(Color.FromRgb(0xE6, 0xED, 0xF3)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin              = new Thickness(0, 0, 0, 2)
        });

        // Version + subtitle
        panel.Children.Add(new TextBlock
        {
            Text                = $"{verStr}  ·  Theme editor for the OXSUIT standard",
            FontSize            = 12, FontFamily = new FontFamily("Segoe UI"),
            Foreground          = new SolidColorBrush(Color.FromRgb(0x4D, 0x62, 0x78)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin              = new Thickness(0, 0, 0, 18)
        });

        // Divider
        panel.Children.Add(new Rectangle
        {
            Height = 1,
            Fill   = new SolidColorBrush(Color.FromRgb(0x1C, 0x23, 0x33)),
            Margin = new Thickness(0, 0, 0, 18)
        });

        // Credits
        panel.Children.Add(new TextBlock
        {
            Text       = "by H.-R. Matthes  &  Claude (Anthropic)",
            FontSize   = 13, FontFamily = new FontFamily("Segoe UI"),
            Foreground = new SolidColorBrush(Color.FromRgb(0xC8, 0xD8, 0xE8)),
            Margin     = new Thickness(0, 0, 0, 8)
        });

        // Caffeine disclaimer
        panel.Children.Add(new TextBlock
        {
            Text         = "Made with God's help and a lot of caffeine.  ☕",
            FontSize     = 11, FontFamily = new FontFamily("Segoe UI"),
            FontStyle    = FontStyles.Italic,
            Foreground   = new SolidColorBrush(Color.FromRgb(0x4D, 0x62, 0x78)),
            Margin       = new Thickness(0, 0, 0, 24),
            TextWrapping = TextWrapping.Wrap
        });

        // Close button
        var closeBtn = new Button
        {
            Content             = "Close",
            IsDefault           = true,
            Height              = 32,
            Padding             = new Thickness(24, 0, 24, 0),
            Style               = (Style)FindResource("ToolbarButtonStyle"),
            Background          = new SolidColorBrush(Color.FromRgb(0x18, 0xC0, 0xB4)),
            Foreground          = new SolidColorBrush(Color.FromRgb(0x06, 0x0C, 0x10)),
            FontWeight          = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        closeBtn.Click += (_, _) => win.Close();
        panel.Children.Add(closeBtn);

        win.Content = panel;
        win.ShowDialog();
    }

    private void BtnOptions_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OptionsWindow(_settings) { Owner = this };
        dlg.SourceInitialized += (_, _) => ApplyTitleBarTheme(dlg);
        if (dlg.ShowDialog() == true)
        {
            _settings.ThemesFolder = dlg.SelectedFolder;
            _settings.Save();
            _themesFolder = _settings.ResolveThemesFolder();
            StatusFolderText.Text = string.IsNullOrEmpty(_themesFolder)
                ? "No themes folder configured"
                : _themesFolder;
        }
    }

    // ── Input dialog helper ───────────────────────────────────────────────────

    private string? ShowInputDialog(string title, string prompt, string defaultValue)
    {
        var dlg = new Window
        {
            Title           = title,
            Width           = 360,
            SizeToContent   = SizeToContent.Height,   // grow to fit — no more cut-off buttons
            ResizeMode      = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner           = this,
            Background      = new SolidColorBrush(Color.FromRgb(0x0D, 0x11, 0x17)),
            ShowInTaskbar   = false
        };
        dlg.SourceInitialized += (_, _) => ApplyTitleBarTheme(dlg);

        var grid = new Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var lbl = new TextBlock
        {
            Text = prompt,
            Foreground = new SolidColorBrush(Color.FromRgb(0xA8, 0xB8, 0xCC)),
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 4)
        };
        Grid.SetRow(lbl, 0);

        var tb = new TextBox
        {
            Text = defaultValue,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12,
            Background = new SolidColorBrush(Color.FromRgb(0x0A, 0x0F, 0x18)),
            Foreground = new SolidColorBrush(Color.FromRgb(0xD8, 0xE4, 0xF0)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x1C, 0x23, 0x33)),
            CaretBrush = new SolidColorBrush(Color.FromRgb(0x18, 0xC0, 0xB4)),
            Padding = new Thickness(4, 2, 4, 2)
        };
        tb.SelectAll();
        Grid.SetRow(tb, 1);

        var btnRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 10, 0, 0)
        };
        Grid.SetRow(btnRow, 3);

        string? result = null;

        var cancelBtn = new Button { Content = "Cancel", Width = 70, Margin = new Thickness(0, 0, 8, 0) };
        var okBtn = new Button
        {
            Content = "OK", Width = 70,
            Background = new SolidColorBrush(Color.FromRgb(0x18, 0xC0, 0xB4)),
            Foreground = new SolidColorBrush(Color.FromRgb(0x06, 0x0C, 0x10)),
            FontWeight = FontWeights.SemiBold
        };

        cancelBtn.Click += (_, _) => dlg.DialogResult = false;
        okBtn.Click += (_, _) =>
        {
            result = tb.Text;
            dlg.DialogResult = true;
        };
        tb.KeyDown += (_, ke) =>
        {
            if (ke.Key == Key.Return) { result = tb.Text; dlg.DialogResult = true; }
            else if (ke.Key == Key.Escape) dlg.DialogResult = false;
        };

        btnRow.Children.Add(cancelBtn);
        btnRow.Children.Add(okBtn);

        grid.Children.Add(lbl);
        grid.Children.Add(tb);
        grid.Children.Add(btnRow);

        dlg.Content = grid;
        dlg.Loaded += (_, _) => tb.Focus();

        return dlg.ShowDialog() == true ? result : null;
    }

    // ── DWM title bar theming ─────────────────────────────────────────────────

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;  // Windows 10 2004+
    private const int DWMWA_CAPTION_COLOR           = 35;  // Windows 11+
    private const int DWMWA_TEXT_COLOR              = 36;  // Windows 11+

    // ── Randomizer ────────────────────────────────────────────────────────────

    /// <summary>
    /// Shows a confirmation dialog the first time and whenever more than 30 s
    /// have passed since the user last confirmed a randomize — so rapid repeated
    /// randomizing doesn't require a click every time, but an accidental hit
    /// after a pause always asks first.
    /// </summary>
    private bool ConfirmRandomize()
    {
        var now = DateTime.Now;
        if (_lastRandomizeConfirmed.HasValue &&
            (now - _lastRandomizeConfirmed.Value).TotalSeconds < 30)
        {
            _lastRandomizeConfirmed = now;  // reset the window on every active press
            return true;
        }

        var result = MessageBox.Show(this,
            "All current theme colours will be overwritten.\n\nContinue?",
            "Randomize Theme",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.OK) return false;
        _lastRandomizeConfirmed = now;
        return true;
    }

    private enum RandomizeMode { Dark, Mid, Light }

    private void RandomizeTheme(RandomizeMode mode)
    {
        var r = new Random();

        // Background channel ranges per mode
        (int lo, int hi) bgR = mode switch {
            RandomizeMode.Dark  => (5,   78),
            RandomizeMode.Mid   => (88,  158),
            RandomizeMode.Light => (182, 248),
            _                   => (5,   78)
        };

        // Generate a random background colour in the mode's brightness band.
        Color RandBg() => Color.FromRgb(
            (byte)r.Next(bgR.lo, bgR.hi),
            (byte)r.Next(bgR.lo, bgR.hi),
            (byte)r.Next(bgR.lo, bgR.hi));

        // Normal text — clearly visible against bg (opposite brightness band).
        Color RandText(Color bg)
        {
            double avg = (bg.R + bg.G + bg.B) / 3.0;
            bool dark  = avg < 128;
            int tlo = dark ? 155 : 0;
            int thi = dark ? 240 : 80;
            return Color.FromRgb(
                (byte)r.Next(tlo, thi),
                (byte)r.Next(tlo, thi),
                (byte)r.Next(tlo, thi));
        }

        // Dim text — midpoint between bg and text with small variance.
        Color RandDim(Color bg, Color text)
        {
            byte Mid(byte a, byte b) => (byte)Math.Clamp((a + b) / 2 + r.Next(-20, 20), 0, 255);
            return Color.FromRgb(Mid(bg.R, text.R), Mid(bg.G, text.G), Mid(bg.B, text.B));
        }

        // Highlight / accent on a surface — vivid pop colour.
        Color RandHigh(Color bg)
        {
            double avg   = (bg.R + bg.G + bg.B) / 3.0;
            int[] ch     = [ r.Next(50, 180), r.Next(50, 180), r.Next(50, 180) ];
            int   dom    = r.Next(3);
            ch[dom]      = avg < 128 ? r.Next(185, 255) : r.Next(30, 120);  // bright on dark, dark on light
            int   sub    = (dom + 1 + r.Next(2)) % 3;
            ch[sub]      = avg < 128 ? r.Next(20, 90)   : r.Next(160, 230); // suppress one for saturation
            return Color.FromRgb((byte)ch[0], (byte)ch[1], (byte)ch[2]);
        }

        // Border — subtle visibility shift from bg.
        Color RandBorder(Color bg)
        {
            double avg = (bg.R + bg.G + bg.B) / 3.0;
            int shift  = r.Next(22, 48) * (avg < 128 ? 1 : -1);
            return Color.FromRgb(
                (byte)Math.Clamp(bg.R + shift, 0, 255),
                (byte)Math.Clamp(bg.G + shift, 0, 255),
                (byte)Math.Clamp(bg.B + shift, 0, 255));
        }

        // Hover — smaller shift from bg for subtle interactive feedback.
        Color RandHover(Color bg)
        {
            double avg = (bg.R + bg.G + bg.B) / 3.0;
            int shift  = r.Next(12, 26) * (avg < 128 ? 1 : -1);
            return Color.FromRgb(
                (byte)Math.Clamp(bg.R + shift, 0, 255),
                (byte)Math.Clamp(bg.G + shift, 0, 255),
                (byte)Math.Clamp(bg.B + shift, 0, 255));
        }

        // Vivid stand-alone accent colour (for AccentBg / PrimaryAccent etc.).
        Color RandAccent()
        {
            int[] ch = [ r.Next(40, 200), r.Next(40, 200), r.Next(40, 200) ];
            int   dom = r.Next(3);
            ch[dom]   = r.Next(130, 220);         // dominant channel
            ch[(dom + 1 + r.Next(2)) % 3] = r.Next(0, 70); // suppressed channel → saturation
            return Color.FromRgb((byte)ch[0], (byte)ch[1], (byte)ch[2]);
        }

        // Text on an accent colour.
        Color AccentTextFor(Color accent)
        {
            double avg = (accent.R + accent.G + accent.B) / 3.0;
            int lo = avg > 140 ? 5 : 210, hi = avg > 140 ? 50 : 252;
            return Color.FromRgb((byte)r.Next(lo, hi), (byte)r.Next(lo, hi), (byte)r.Next(lo, hi));
        }

        // ── Content surface ───────────────────────────────────────────────────
        var cBg = RandBg(); var cTxt = RandText(cBg);
        _model.Colors["ContentBgBrush"]     = cBg;
        _model.Colors["ContentTextBrush"]   = cTxt;
        _model.Colors["ContentDimBrush"]    = RandDim(cBg, cTxt);
        _model.Colors["ContentHighBrush"]   = RandHigh(cBg);
        _model.Colors["ContentBorderBrush"] = RandBorder(cBg);

        // ── Sidebar surface ───────────────────────────────────────────────────
        var sBg = RandBg(); var sTxt = RandText(sBg);
        _model.Colors["SidebarBgBrush"]     = sBg;
        _model.Colors["SidebarTextBrush"]   = sTxt;
        _model.Colors["SidebarDimBrush"]    = RandDim(sBg, sTxt);
        _model.Colors["SidebarHighBrush"]   = RandHigh(sBg);
        _model.Colors["SidebarBorderBrush"] = RandBorder(sBg);

        // ── Control surface ───────────────────────────────────────────────────
        var ctBg = RandBg(); var ctTxt = RandText(ctBg);
        _model.Colors["ControlBgBrush"]     = ctBg;
        _model.Colors["ControlHoverBrush"]  = RandHover(ctBg);
        _model.Colors["ControlTextBrush"]   = ctTxt;
        _model.Colors["ControlDimBrush"]    = RandDim(ctBg, ctTxt);
        _model.Colors["ControlHighBrush"]   = RandHigh(ctBg);
        _model.Colors["ControlBorderBrush"] = RandBorder(ctBg);

        // ── Input surface ─────────────────────────────────────────────────────
        var iBg = RandBg(); var iTxt = RandText(iBg);
        _model.Colors["InputBgBrush"]     = iBg;
        _model.Colors["InputTextBrush"]   = iTxt;
        _model.Colors["InputDimBrush"]    = RandDim(iBg, iTxt);
        _model.Colors["InputHighBrush"]   = RandHigh(iBg);
        _model.Colors["InputBorderBrush"] = RandBorder(iBg);

        // ── Accent ────────────────────────────────────────────────────────────
        var aBg  = RandAccent(); var aTxt = AccentTextFor(aBg);
        _model.Colors["AccentBgBrush"]        = aBg;
        _model.Colors["AccentTextBrush"]      = aTxt;
        _model.Colors["AccentHighlightBrush"] = Color.FromRgb(
            (byte)Math.Clamp(aBg.R + r.Next(20, 45), 0, 255),
            (byte)Math.Clamp(aBg.G + r.Next(20, 45), 0, 255),
            (byte)Math.Clamp(aBg.B + r.Next(20, 45), 0, 255));
        _model.Colors["PrimaryAccentBrush"]   = RandAccent();
        _model.Colors["SecondaryAccentBrush"] = RandAccent();
        _model.Colors["TertiaryAccentBrush"]  = RandAccent();

        // ── Primary / Secondary / Tertiary bubble slots ───────────────────────
        foreach (var prefix in (string[])["Primary", "Secondary", "Tertiary"])
        {
            var bBg = RandBg(); var bTxt = RandText(bBg);
            _model.Colors[$"{prefix}BubbleBrush"]       = bBg;
            _model.Colors[$"{prefix}TextBrush"]         = bTxt;
            _model.Colors[$"{prefix}DimBrush"]          = RandDim(bBg, bTxt);
            _model.Colors[$"{prefix}HighBrush"]         = RandHigh(bBg);
            _model.Colors[$"{prefix}BubbleBorderBrush"] = RandBorder(bBg);
        }

        ApplyModel();
    }

    /// <summary>
    /// Colours the OS title bar of <paramref name="target"/> to match the
    /// currently loaded theme's SidebarBgBrush / SidebarTextBrush.
    /// Silently no-ops on Windows 10 (dark-mode flag still applies there).
    /// </summary>
    private void ApplyTitleBarTheme(Window target)
    {
        try
        {
            if (PresentationSource.FromVisual(target) is not HwndSource src) return;
            var hwnd = src.Handle;

            var bgColor   = GetColor("SidebarBgBrush");
            var textColor = GetColor("SidebarTextBrush");

            int isDark = RelativeLuminance(bgColor) < 0.5 ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref isDark, sizeof(int));

            int captionColor = ToColorRef(bgColor);
            DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));

            int textColorRef = ToColorRef(textColor);
            DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref textColorRef, sizeof(int));

            // Force the non-client area to repaint so DWM changes take effect immediately.
            const uint SWP_NOMOVE       = 0x0002;
            const uint SWP_NOSIZE       = 0x0001;
            const uint SWP_NOZORDER     = 0x0004;
            const uint SWP_FRAMECHANGED = 0x0020;
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
        }
        catch { /* cosmetic-only — never fatal */ }
    }

    /// <summary>Converts a WPF Color to a Win32 COLORREF (0x00BBGGRR).</summary>
    private static int ToColorRef(Color c) => c.R | (c.G << 8) | (c.B << 16);

    /// <summary>Returns relative luminance (0 = black, 1 = white) for dark/light mode detection.</summary>
    private static double RelativeLuminance(Color c)
    {
        static double Lin(double v) =>
            v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        return 0.2126 * Lin(c.R / 255.0) +
               0.7152 * Lin(c.G / 255.0) +
               0.0722 * Lin(c.B / 255.0);
    }
}
