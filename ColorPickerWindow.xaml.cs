namespace Theminator;

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Theminator.Models;

public partial class ColorPickerWindow : Window
{
    public Color ColorResult { get; private set; }

    private bool _updating;
    private double _hue;
    private double _sat;
    private double _val;
    private byte _alpha = 255;

    private bool _svDragging;
    private bool _hueDragging;
    private bool _alphaDragging;

    private const double SvSize = 220;
    private const double BarHeight = 220;

    public ColorPickerWindow(Color initialColor, string keyName)
    {
        InitializeComponent();
        TitleText.Text = $"Color Picker — {keyName}";
        ColorResult = initialColor;

        OldColorSwatch.Fill = new SolidColorBrush(initialColor);

        ColorToHsv(initialColor, out _hue, out _sat, out _val);
        _alpha = initialColor.A;

        LoadState();
    }

    // ── HSV conversion ────────────────────────────────────────────────────────

    public static void ColorToHsv(Color c, out double h, out double s, out double v)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;

        v = max;
        s = max < 1e-6 ? 0 : delta / max;

        if (delta < 1e-6) { h = 0; return; }

        if (max == r)      h = 60 * (((g - b) / delta) % 6);
        else if (max == g) h = 60 * (((b - r) / delta) + 2);
        else               h = 60 * (((r - g) / delta) + 4);

        if (h < 0) h += 360;
    }

    public static Color HsvToColor(double h, double s, double v, byte a)
    {
        h = ((h % 360) + 360) % 360;
        int hi = (int)(h / 60) % 6;
        double f = (h / 60) - Math.Floor(h / 60);
        double p = v * (1 - s);
        double q = v * (1 - f * s);
        double t = v * (1 - (1 - f) * s);

        double r, g, b;
        switch (hi)
        {
            case 0: (r, g, b) = (v, t, p); break;
            case 1: (r, g, b) = (q, v, p); break;
            case 2: (r, g, b) = (p, v, t); break;
            case 3: (r, g, b) = (p, q, v); break;
            case 4: (r, g, b) = (t, p, v); break;
            default:(r, g, b) = (v, p, q); break;
        }
        return Color.FromArgb(a,
            (byte)Math.Round(r * 255),
            (byte)Math.Round(g * 255),
            (byte)Math.Round(b * 255));
    }

    // ── State → UI ────────────────────────────────────────────────────────────

    private void LoadState()
    {
        _updating = true;
        try
        {
            var c = HsvToColor(_hue, _sat, _val, _alpha);
            ColorResult = c;

            // Update SV canvas background (pure hue)
            var hueColor = HsvToColor(_hue, 1, 1, 255);
            SvHueBg.Fill = new SolidColorBrush(hueColor);

            // Update SV cursor
            double cx = _sat * SvSize - 6;
            double cy = (1 - _val) * SvSize - 6;
            Canvas.SetLeft(SvCursor, cx);
            Canvas.SetTop(SvCursor, cy);

            // Hue cursor
            double hy = (_hue / 360.0) * BarHeight;
            Canvas.SetTop(HueCursor, hy);

            // Alpha bar gradient
            var opaqueColor = Color.FromArgb(255, c.R, c.G, c.B);
            var transparentColor = Color.FromArgb(0, c.R, c.G, c.B);
            AlphaBarRect.Fill = new LinearGradientBrush(
                new GradientStopCollection {
                    new GradientStop(opaqueColor, 0),
                    new GradientStop(transparentColor, 1)
                },
                new Point(0, 0), new Point(0, 1));

            // Alpha cursor
            double ay = (1 - _alpha / 255.0) * BarHeight;
            Canvas.SetTop(AlphaCursor, ay);

            // New swatch
            NewColorSwatch.Fill = new SolidColorBrush(c);

            // Hex
            HexBox.Text = ThemeModel.ToHex(c).TrimStart('#');

            // Sliders + boxes
            SliderR.Value = c.R; BoxR.Text = c.R.ToString();
            SliderG.Value = c.G; BoxG.Text = c.G.ToString();
            SliderB.Value = c.B; BoxB.Text = c.B.ToString();
            SliderA.Value = _alpha; BoxA.Text = _alpha.ToString();
        }
        finally { _updating = false; }
    }

    // ── SV Canvas ─────────────────────────────────────────────────────────────

    private void SvCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _svDragging = true;
        SvCanvas.CaptureMouse();
        UpdateSvFromPoint(e.GetPosition(SvCanvas));
    }

    private void SvCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_svDragging) UpdateSvFromPoint(e.GetPosition(SvCanvas));
    }

    private void SvCanvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _svDragging = false;
        SvCanvas.ReleaseMouseCapture();
    }

    private void UpdateSvFromPoint(Point p)
    {
        _sat = Math.Clamp(p.X / SvSize, 0, 1);
        _val = Math.Clamp(1 - p.Y / SvSize, 0, 1);
        LoadState();
    }

    // ── Hue Bar ───────────────────────────────────────────────────────────────

    private void HueCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _hueDragging = true;
        HueCanvas.CaptureMouse();
        UpdateHueFromPoint(e.GetPosition(HueCanvas));
    }

    private void HueCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_hueDragging) UpdateHueFromPoint(e.GetPosition(HueCanvas));
    }

    private void HueCanvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _hueDragging = false;
        HueCanvas.ReleaseMouseCapture();
    }

    private void UpdateHueFromPoint(Point p)
    {
        _hue = Math.Clamp(p.Y / BarHeight, 0, 1) * 360;
        LoadState();
    }

    // ── Alpha Bar ─────────────────────────────────────────────────────────────

    private void AlphaCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _alphaDragging = true;
        AlphaCanvas.CaptureMouse();
        UpdateAlphaFromPoint(e.GetPosition(AlphaCanvas));
    }

    private void AlphaCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_alphaDragging) UpdateAlphaFromPoint(e.GetPosition(AlphaCanvas));
    }

    private void AlphaCanvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _alphaDragging = false;
        AlphaCanvas.ReleaseMouseCapture();
    }

    private void UpdateAlphaFromPoint(Point p)
    {
        _alpha = (byte)(255 - Math.Clamp(p.Y / BarHeight, 0, 1) * 255);
        LoadState();
    }

    // ── Hex Box ───────────────────────────────────────────────────────────────

    private void HexBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating) return;
        var txt = HexBox.Text.Trim();
        Color? parsed = null;
        if (txt.Length == 6)
        {
            try { parsed = ThemeModel.ParseHex("#" + txt); } catch { }
        }
        else if (txt.Length == 8)
        {
            try { parsed = ThemeModel.ParseHex("#" + txt); } catch { }
        }
        if (parsed.HasValue)
        {
            var c = parsed.Value;
            ColorToHsv(c, out _hue, out _sat, out _val);
            _alpha = c.A;
            LoadState();
        }
    }

    // ── RGB/A Sliders & Boxes ─────────────────────────────────────────────────

    private void SliderR_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating) return;
        var c = HsvToColor(_hue, _sat, _val, _alpha);
        var nc = Color.FromArgb(_alpha, (byte)SliderR.Value, c.G, c.B);
        ColorToHsv(nc, out _hue, out _sat, out _val);
        LoadState();
    }

    private void SliderG_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating) return;
        var c = HsvToColor(_hue, _sat, _val, _alpha);
        var nc = Color.FromArgb(_alpha, c.R, (byte)SliderG.Value, c.B);
        ColorToHsv(nc, out _hue, out _sat, out _val);
        LoadState();
    }

    private void SliderB_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating) return;
        var c = HsvToColor(_hue, _sat, _val, _alpha);
        var nc = Color.FromArgb(_alpha, c.R, c.G, (byte)SliderB.Value);
        ColorToHsv(nc, out _hue, out _sat, out _val);
        LoadState();
    }

    private void SliderA_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating) return;
        _alpha = (byte)SliderA.Value;
        LoadState();
    }

    private void BoxR_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating) return;
        if (byte.TryParse(BoxR.Text, out var v)) { SliderR.Value = v; }
    }

    private void BoxG_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating) return;
        if (byte.TryParse(BoxG.Text, out var v)) { SliderG.Value = v; }
    }

    private void BoxB_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating) return;
        if (byte.TryParse(BoxB.Text, out var v)) { SliderB.Value = v; }
    }

    private void BoxA_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating) return;
        if (byte.TryParse(BoxA.Text, out var v)) { _alpha = v; SliderA.Value = v; LoadState(); }
    }

    // ── Buttons ───────────────────────────────────────────────────────────────

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        ColorResult = HsvToColor(_hue, _sat, _val, _alpha);
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
