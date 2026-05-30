<div align="center">

<img src="Assets/Oxminator_1024.png" width="280" alt="Oxminator — The Bull in a Suit"/>

# OXSUIT Theminator

**Put the bull in a suit.**  
***BEEF up your UI.***

*Visual theme editor for the [OXSUIT 1.0](https://github.com/Doombug75/OXSUIT) open theme standard*

</div>

---

Theminator is a standalone WPF desktop application for creating, editing, and previewing `.oxsuit`
theme files. Every color slot is editable with a purpose-built HSV color picker. Changes appear
live in a built-in mock-window preview. Save a theme, drop it in your app's `Themes/` folder — done.

---

## Features

- **42 semantic color slots** organized into collapsible accordion groups:  
  Content · Sidebar · Control · Input · Accent · Primary / Secondary / Tertiary slots
- **Live preview** — a full mock app window (title bar, sidebar, chat area, input bar, accent palette)
  updates instantly on every edit; click any element to open its color picker
- **Full HSV color picker** — saturation/value canvas, hue bar, brightness bar, alpha bar,
  RGBA sliders, hex input, old/new color swatches
- **Theme metadata** — name, author, and description saved directly in the file; theme name
  is the default suggestion for Save As
- **Geometry tokens** — global Corner Radius and Shadow Depth sliders, plus per-surface
  border thickness sliders for all 7 border-capable surfaces
- **Randomizer** — one-click dark / mid / light palette generation with contrast-aware
  text colors; 30-second cooldown confirmation guard
- **Load & Save** — reads and writes the OXSUIT 1.0 `.oxsuit` format natively
- **Error handling** — corrupt or invalid theme files show a clear warning;
  the current theme is never lost
- **Claude's Choice** — built-in deep blue-teal starter theme, every color slot unique,
  designed by Claude (Anthropic)
- **Dark UI throughout** — including DWM-themed title bars, dark scroll bars, and
  dark context menus on Windows 11

---

## Screenshots

| Main editor | Color picker |
|:-----------:|:------------:|
| *(coming soon)* | *(coming soon)* |

---

## Requirements

- Windows 10 (build 17763) or later
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)

No installer needed. Copy the `.exe` and run.

---

## Getting started

1. Launch `Theminator.exe`
2. Click **⚙ Options** and point the themes folder to any directory on your machine
3. Use **New** to start from a blank light-mode template, or **Load Theme ▾** to open an existing `.oxsuit` file
4. Edit colors — click any swatch on the left panel, or click any element in the live preview
5. Click **Save As…** to write the finished theme

---

## Building from source

```
git clone https://github.com/Doombug75/Theminator
cd Theminator
dotnet build
```

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

Publish as a single-file executable:

```
dotnet publish -c Release -r win-x64 --self-contained false
```

---

## The OXSUIT format

Themes are saved as UTF-8 XML files with the `.oxsuit` extension:

```xml
<?xml version="1.0" encoding="utf-8"?>
<oxsuit version="1.0"
        name="My Theme"
        author="Your Name"
        description="A great dark theme.">

  <colors>
    <color key="ContentBg"   value="#0D1117"/>
    <color key="ContentText" value="#E6EDF3"/>
    <!-- 27 core slots + 15 optional surface slots -->
  </colors>

  <tokens>
    <token key="CornerRadius"         value="6"  unit="px"/>
    <token key="ShadowDepth"          value="2"/>
    <token key="ContentBorderWidth"   value="1"  unit="px"/>
    <token key="SidebarBorderWidth"   value="1"  unit="px"/>
    <token key="ControlBorderWidth"   value="1"  unit="px"/>
    <token key="InputBorderWidth"     value="1"  unit="px"/>
    <token key="PrimaryBorderWidth"   value="1"  unit="px"/>
    <token key="SecondaryBorderWidth" value="1"  unit="px"/>
    <token key="TertiaryBorderWidth"  value="1"  unit="px"/>
  </tokens>

</oxsuit>
```

See the [OXSUIT specification](https://github.com/Doombug75/OXSUIT/blob/master/SPEC.md) for the
complete format reference, and the
[WPF loader](https://github.com/Doombug75/OXSUIT/blob/master/loaders/wpf/README.md)
for the loader that reads these files into a `ResourceDictionary`.

---

## Color key reference

| Group | Keys |
|-------|------|
| **Content** | `ContentBg` · `ContentBorder` · `ContentText` · `ContentHigh` · `ContentDim` |
| **Sidebar** | `SidebarBg` · `SidebarBorder` · `SidebarText` · `SidebarHigh` · `SidebarDim` |
| **Control** | `ControlBg` · `ControlBorder` · `ControlText` · `ControlHigh` · `ControlDim` · `ControlHover` |
| **Input** | `InputBg` · `InputBorder` · `InputText` · `InputHigh` · `InputDim` |
| **Accent** | `AccentBg` · `AccentText` · `AccentHighlight` · `PrimaryAccent` · `SecondaryAccent` · `TertiaryAccent` |
| **Primary slot** | `PrimaryBg` · `PrimaryBorder` · `PrimaryText` · `PrimaryHigh` · `PrimaryDim` |
| **Secondary slot** | `SecondaryBg` · `SecondaryBorder` · `SecondaryText` · `SecondaryHigh` · `SecondaryDim` |
| **Tertiary slot** | `TertiaryBg` · `TertiaryBorder` · `TertiaryText` · `TertiaryHigh` · `TertiaryDim` |

---

## Mascot

**Oxminator** — a chrome bull in a black suit with golden horns and aviator shades.  
The name is a portmanteau of *OXSUIT* + *Theminator* (and a nod to a certain movie cyborg).  
*The bull in a suit* is the unofficial motto of the OXSUIT project.

---

## Credits

Made by **H.-R. Matthes** & **Claude (Anthropic)**  
With God's help and a lot of caffeine. ☕

---

## License

MIT — use freely in any project, open or commercial.
