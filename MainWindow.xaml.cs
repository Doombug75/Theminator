namespace Theminator;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    // Chat Bubbles
    private Border _pBubble1 = null!, _pBubble2 = null!, _pBubble3 = null!;
    private TextBlock _pBubble1Text = null!, _pBubble2Text = null!, _pBubble3Text = null!;
    private Border _pBubblesBorder = null!;

    // Text Hierarchy
    private TextBlock _pPrimaryText = null!, _pSecondaryText = null!, _pTertiaryText = null!;
    private Border _pTextHierarchyBorder = null!;
    private Border _pAccentAreaBorder = null!;

    // ── Init ───────────────────────────────────────────────────────────────────

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _settings = AppSettings.Load();
        _themesFolder = _settings.ResolveThemesFolder();
        StatusFolderText.Text = string.IsNullOrEmpty(_themesFolder)
            ? "No themes folder configured"
            : _themesFolder;

        BuildLeftPanel();
        BuildPreviewPanel();

        // Try to load last theme
        if (!string.IsNullOrEmpty(_themesFolder) && !string.IsNullOrEmpty(_settings.LastThemeName))
        {
            var path = IOPath.Combine(_themesFolder, _settings.LastThemeName + ".xaml");
            var loaded = ThemeLoader.Load(path);
            if (loaded != null) _model = loaded;
        }

        ApplyModel();
    }

    // ── Left panel construction ────────────────────────────────────────────────

    private void BuildLeftPanel()
    {
        LeftPanel.Children.Clear();
        _leftControls.Clear();

        foreach (var (groupName, keys) in ThemeModel.Groups)
        {
            // Group header
            var header = new TextBlock
            {
                Text = groupName.ToUpper(),
                Style = (Style)FindResource("GroupHeaderStyle")
            };
            LeftPanel.Children.Add(header);

            foreach (var key in keys)
            {
                var row = CreateLeftRow(key);
                LeftPanel.Children.Add(row);
            }
        }
    }

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
            Text = key.Replace("Brush", ""),
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

        // Header
        var hdr = new TextBlock
        {
            Text = "LIVE PREVIEW",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x4D, 0x62, 0x78)),
            FontFamily = new FontFamily("Segoe UI"),
            Margin = new Thickness(0, 0, 0, 8)
        };
        PreviewPanel.Children.Add(hdr);

        // 1. Content Surface
        {
            var label = MakeSectionLabel("Content Surface");
            _pContentBorder = new Border { Padding = new Thickness(10), CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 2, 0, 10) };
            var sp = new StackPanel();

            _pContentHigh = new TextBlock { FontSize = 13, FontFamily = new FontFamily("Segoe UI"), Text = "✦ ContentHighBrush", Margin = new Thickness(0,0,0,3) };
            _pContentText = new TextBlock { FontSize = 12, FontFamily = new FontFamily("Segoe UI"), Text = "ContentTextBrush — Normal text in content area", Margin = new Thickness(0,0,0,3) };
            _pContentDim  = new TextBlock { FontSize = 11, FontFamily = new FontFamily("Segoe UI"), Text = "ContentDimBrush — Subdued secondary text" };

            sp.Children.Add(_pContentHigh);
            sp.Children.Add(_pContentText);
            sp.Children.Add(_pContentDim);
            _pContentBorder.Child = sp;

            PreviewPanel.Children.Add(label);
            PreviewPanel.Children.Add(_pContentBorder);
        }

        // 2. Sidebar Surface
        {
            var label = MakeSectionLabel("Sidebar Surface");
            _pSidebarBorder = new Border { Padding = new Thickness(10), CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 2, 0, 10) };
            var sp = new StackPanel();

            _pSidebarHigh = new TextBlock { FontSize = 13, FontFamily = new FontFamily("Segoe UI"), Text = "✦ SidebarHighBrush", Margin = new Thickness(0,0,0,3) };
            _pSidebarText = new TextBlock { FontSize = 12, FontFamily = new FontFamily("Segoe UI"), Text = "SidebarTextBrush — Normal text in sidebar", Margin = new Thickness(0,0,0,3) };
            _pSidebarDim  = new TextBlock { FontSize = 11, FontFamily = new FontFamily("Segoe UI"), Text = "SidebarDimBrush — Subdued secondary text" };

            sp.Children.Add(_pSidebarHigh);
            sp.Children.Add(_pSidebarText);
            sp.Children.Add(_pSidebarDim);
            _pSidebarBorder.Child = sp;

            PreviewPanel.Children.Add(label);
            PreviewPanel.Children.Add(_pSidebarBorder);
        }

        // 3. Control Surface (two sub-panels side by side)
        {
            var label = MakeSectionLabel("Control Surface");

            _pControlBorder = new Border { Padding = new Thickness(10), CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 2, 0, 0) };
            var sp1 = new StackPanel();
            _pControlText = new TextBlock { FontSize = 12, FontFamily = new FontFamily("Segoe UI"), Text = "ControlTextBrush" };
            _pControlDim  = new TextBlock { FontSize = 11, FontFamily = new FontFamily("Segoe UI"), Text = "ControlDimBrush — subdued" };
            _pControlHigh = new TextBlock { FontSize = 12, FontFamily = new FontFamily("Segoe UI"), Text = "✦ ControlHighBrush" };
            sp1.Children.Add(_pControlText);
            sp1.Children.Add(_pControlDim);
            sp1.Children.Add(_pControlHigh);
            _pControlBorder.Child = sp1;

            _pControlHoverBorder = new Border { Padding = new Thickness(10), CornerRadius = new CornerRadius(4), Margin = new Thickness(4, 2, 0, 0) };
            var sp2 = new StackPanel();
            _pControlHoverLabel = new TextBlock { FontSize = 11, FontFamily = new FontFamily("Segoe UI"), Text = "ControlHoverBrush", Foreground = new SolidColorBrush(Color.FromRgb(0xA8, 0xB8, 0xCC)) };
            sp2.Children.Add(_pControlHoverLabel);
            _pControlHoverBorder.Child = sp2;

            var row = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(_pControlBorder, 0);
            Grid.SetColumn(_pControlHoverBorder, 1);
            row.Children.Add(_pControlBorder);
            row.Children.Add(_pControlHoverBorder);

            PreviewPanel.Children.Add(label);
            PreviewPanel.Children.Add(row);
        }

        // 4. Input Surface
        {
            var label = MakeSectionLabel("Input Surface");
            _pInputBorder = new Border { Padding = new Thickness(10), CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 2, 0, 10) };
            var sp = new StackPanel();
            _pInputText = new TextBlock { FontSize = 12, FontFamily = new FontFamily("Segoe UI"), Text = "InputTextBrush — user typed text" };
            _pInputDim  = new TextBlock { FontSize = 11, FontFamily = new FontFamily("Segoe UI"), Text = "InputDimBrush — placeholder hint..." };
            _pInputHigh = new TextBlock { FontSize = 12, FontFamily = new FontFamily("Segoe UI"), Text = "✦ InputHighBrush — symbol glow" };
            sp.Children.Add(_pInputText);
            sp.Children.Add(_pInputDim);
            sp.Children.Add(_pInputHigh);
            _pInputBorder.Child = sp;

            PreviewPanel.Children.Add(label);
            PreviewPanel.Children.Add(_pInputBorder);
        }

        // 5. Accent Colors
        {
            var label = MakeSectionLabel("Accent Colors");
            _pAccentAreaBorder = new Border { Padding = new Thickness(10), CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 2, 0, 10) };
            var sp = new StackPanel();

            // Button preview
            _pAccentBtnBorder = new Border
            {
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12, 6, 12, 6),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 8)
            };
            _pAccentBtnText = new TextBlock { FontSize = 12, FontFamily = new FontFamily("Segoe UI"), FontWeight = FontWeights.SemiBold, Text = "AccentBgBrush Button" };
            _pAccentBtnBorder.Child = _pAccentBtnText;
            sp.Children.Add(_pAccentBtnBorder);

            // Swatch row
            var swatchRow = new StackPanel { Orientation = Orientation.Horizontal };
            swatchRow.Children.Add(MakeAccentSwatch("AccentHighlightBrush", out _pAccentHighRect));
            swatchRow.Children.Add(MakeAccentSwatch("PrimaryAccentBrush", out _pPrimaryAccentRect));
            swatchRow.Children.Add(MakeAccentSwatch("SecondaryAccentBrush", out _pSecondaryAccentRect));
            swatchRow.Children.Add(MakeAccentSwatch("TertiaryAccentBrush", out _pTertiaryAccentRect));
            sp.Children.Add(swatchRow);

            _pAccentAreaBorder.Child = sp;
            PreviewPanel.Children.Add(label);
            PreviewPanel.Children.Add(_pAccentAreaBorder);
        }

        // 6. Chat Bubbles
        {
            var label = MakeSectionLabel("Chat Bubbles");
            _pBubblesBorder = new Border { Padding = new Thickness(10), CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 2, 0, 10) };
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition());

            _pBubble1 = new Border { Padding = new Thickness(8, 6, 8, 6), CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 0, 4, 0) };
            _pBubble1Text = new TextBlock { FontSize = 11, FontFamily = new FontFamily("Segoe UI"), Text = "Primary bubble", TextWrapping = TextWrapping.Wrap };
            _pBubble1.Child = _pBubble1Text;
            Grid.SetColumn(_pBubble1, 0);

            _pBubble2 = new Border { Padding = new Thickness(8, 6, 8, 6), CornerRadius = new CornerRadius(4), Margin = new Thickness(2, 0, 2, 0) };
            _pBubble2Text = new TextBlock { FontSize = 11, FontFamily = new FontFamily("Segoe UI"), Text = "Secondary bubble", TextWrapping = TextWrapping.Wrap };
            _pBubble2.Child = _pBubble2Text;
            Grid.SetColumn(_pBubble2, 1);

            _pBubble3 = new Border { Padding = new Thickness(8, 6, 8, 6), CornerRadius = new CornerRadius(4), Margin = new Thickness(4, 0, 0, 0) };
            _pBubble3Text = new TextBlock { FontSize = 11, FontFamily = new FontFamily("Segoe UI"), Text = "Tertiary bubble", TextWrapping = TextWrapping.Wrap };
            _pBubble3.Child = _pBubble3Text;
            Grid.SetColumn(_pBubble3, 2);

            row.Children.Add(_pBubble1);
            row.Children.Add(_pBubble2);
            row.Children.Add(_pBubble3);

            _pBubblesBorder.Child = row;
            PreviewPanel.Children.Add(label);
            PreviewPanel.Children.Add(_pBubblesBorder);
        }

        // 7. Text Hierarchy
        {
            var label = MakeSectionLabel("Text Hierarchy");
            _pTextHierarchyBorder = new Border { Padding = new Thickness(10), CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 2, 0, 10) };
            var sp = new StackPanel();

            _pPrimaryText   = new TextBlock { FontSize = 16, FontFamily = new FontFamily("Segoe UI"), FontWeight = FontWeights.SemiBold, Text = "PrimaryTextBrush — Heading" };
            _pSecondaryText = new TextBlock { FontSize = 14, FontFamily = new FontFamily("Segoe UI"), Text = "SecondaryTextBrush — Body" };
            _pTertiaryText  = new TextBlock { FontSize = 12, FontFamily = new FontFamily("Segoe UI"), Text = "TertiaryTextBrush — Caption" };

            sp.Children.Add(_pPrimaryText);
            sp.Children.Add(_pSecondaryText);
            sp.Children.Add(_pTertiaryText);
            _pTextHierarchyBorder.Child = sp;

            PreviewPanel.Children.Add(label);
            PreviewPanel.Children.Add(_pTextHierarchyBorder);
        }
    }

    private static TextBlock MakeSectionLabel(string text) => new TextBlock
    {
        Text = text,
        FontSize = 11,
        FontWeight = FontWeights.SemiBold,
        FontFamily = new FontFamily("Segoe UI"),
        Foreground = new SolidColorBrush(Color.FromRgb(0xA8, 0xB8, 0xCC)),
        Margin = new Thickness(0, 0, 0, 3)
    };

    private static StackPanel MakeAccentSwatch(string label, out Rectangle rect)
    {
        rect = new Rectangle { Width = 40, Height = 20, RadiusX = 3, RadiusY = 3 };
        var tb = new TextBlock
        {
            Text = label.Replace("Brush", ""),
            FontSize = 9,
            FontFamily = new FontFamily("Segoe UI"),
            Foreground = new SolidColorBrush(Color.FromRgb(0x68, 0x78, 0x88)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 2, 0, 0)
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
        UpdateLeftPanel();
        UpdatePreview();
        _settings.LastThemeName = _model.Name;
        _settings.Save();
    }

    private void UpdateLeftPanel()
    {
        foreach (var key in ThemeModel.Keys)
        {
            if (!_leftControls.TryGetValue(key, out var ctrl)) continue;
            var c = GetColor(key);
            ctrl.Swatch.Fill = new SolidColorBrush(c);
            ctrl.HexLabel.Text = ThemeModel.ToHex(c);
        }
    }

    private void UpdatePreview()
    {
        // Content Surface
        _pContentBorder.Background = Brush(GetColor("ContentBgBrush"));
        _pContentHigh.Foreground   = Brush(GetColor("ContentHighBrush"));
        _pContentText.Foreground   = Brush(GetColor("ContentTextBrush"));
        _pContentDim.Foreground    = Brush(GetColor("ContentDimBrush"));

        // Sidebar Surface
        _pSidebarBorder.Background = Brush(GetColor("SidebarBgBrush"));
        _pSidebarHigh.Foreground   = Brush(GetColor("SidebarHighBrush"));
        _pSidebarText.Foreground   = Brush(GetColor("SidebarTextBrush"));
        _pSidebarDim.Foreground    = Brush(GetColor("SidebarDimBrush"));

        // Control Surface
        _pControlBorder.Background     = Brush(GetColor("ControlBgBrush"));
        _pControlText.Foreground       = Brush(GetColor("ControlTextBrush"));
        _pControlDim.Foreground        = Brush(GetColor("ControlDimBrush"));
        _pControlHigh.Foreground       = Brush(GetColor("ControlHighBrush"));
        _pControlHoverBorder.Background = Brush(GetColor("ControlHoverBrush"));

        // Input Surface
        _pInputBorder.Background = Brush(GetColor("InputBgBrush"));
        _pInputText.Foreground   = Brush(GetColor("InputTextBrush"));
        _pInputDim.Foreground    = Brush(GetColor("InputDimBrush"));
        _pInputHigh.Foreground   = Brush(GetColor("InputHighBrush"));

        // Accent Colors
        _pAccentAreaBorder.Background  = Brush(GetColor("ContentBgBrush"));
        _pAccentBtnBorder.Background   = Brush(GetColor("AccentBgBrush"));
        _pAccentBtnText.Foreground     = Brush(GetColor("AccentTextBrush"));
        _pAccentHighRect.Fill          = Brush(GetColor("AccentHighlightBrush"));
        _pPrimaryAccentRect.Fill       = Brush(GetColor("PrimaryAccentBrush"));
        _pSecondaryAccentRect.Fill     = Brush(GetColor("SecondaryAccentBrush"));
        _pTertiaryAccentRect.Fill      = Brush(GetColor("TertiaryAccentBrush"));

        // Chat Bubbles
        var contentText = Brush(GetColor("ContentTextBrush"));
        _pBubblesBorder.Background = Brush(GetColor("ContentBgBrush"));
        _pBubble1.Background   = Brush(GetColor("PrimaryBubbleBrush"));
        _pBubble1Text.Foreground = contentText;
        _pBubble2.Background   = Brush(GetColor("SecondaryBubbleBrush"));
        _pBubble2Text.Foreground = contentText;
        _pBubble3.Background   = Brush(GetColor("TertiaryBubbleBrush"));
        _pBubble3Text.Foreground = contentText;

        // Text Hierarchy
        _pTextHierarchyBorder.Background = Brush(GetColor("ContentBgBrush"));
        _pPrimaryText.Foreground   = Brush(GetColor("PrimaryTextBrush"));
        _pSecondaryText.Foreground = Brush(GetColor("SecondaryTextBrush"));
        _pTertiaryText.Foreground  = Brush(GetColor("TertiaryTextBrush"));
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
        if (dlg.ShowDialog() == true)
        {
            _model.Colors[key] = dlg.ColorResult;
            if (_leftControls.TryGetValue(key, out var ctrl))
            {
                ctrl.Swatch.Fill = new SolidColorBrush(dlg.ColorResult);
                ctrl.HexLabel.Text = ThemeModel.ToHex(dlg.ColorResult);
            }
            UpdatePreview();
        }
    }

    // ── Toolbar handlers ──────────────────────────────────────────────────────

    private void BtnLoadTheme_Click(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu();

        if (!string.IsNullOrEmpty(_themesFolder) && IODirectory.Exists(_themesFolder))
        {
            var files = IODirectory.GetFiles(_themesFolder, "*.xaml")
                                 .OrderBy(f => IOPath.GetFileNameWithoutExtension(f))
                                 .ToList();
            if (files.Count == 0)
            {
                menu.Items.Add(new MenuItem { Header = "(No themes found)", IsEnabled = false });
            }
            else
            {
                foreach (var file in files)
                {
                    var name = ThemeLoader.NameFromPath(file);
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
        _model = ThemeModel.ClaudesChoice();
        _model.Name = "New Theme";
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

        // Simple name input dialog
        var input = ShowInputDialog("Save Theme As", "Theme name:", _model.Name);
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

        var savePath = IOPath.Combine(_themesFolder, input + ".xaml");
        if (IOFile.Exists(savePath))
        {
            var confirm = MessageBox.Show(this,
                $"'{input}.xaml' already exists. Overwrite?",
                "Confirm Overwrite", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;
        }

        _model.Name = input;
        ThemeLoader.Save(_model, savePath);
        ApplyModel();
        MessageBox.Show(this, $"Theme saved to:\n{savePath}", "Saved",
            MessageBoxButton.OK, MessageBoxImage.None);
    }

    private void BtnOptions_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OptionsWindow(_settings) { Owner = this };
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
            Title = title,
            Width = 360,
            Height = 140,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            Background = new SolidColorBrush(Color.FromRgb(0x0D, 0x11, 0x17)),
            ShowInTaskbar = false
        };

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
}
