<div align="center">

<img src="Assets/Oxminator_256.png" width="140" alt="Oxminator — The Bull in a Suit"/>

# OXSUIT Theminator

**Put the bull in a suit.**

*Visual theme editor for the [OXSUIT](../OXSUIT) open theme standard*

</div>

---

Theminator is a standalone WPF desktop application for creating, editing, and previewing `.oxsuit`
theme files. Every color slot is editable with a purpose-built HSV color picker. Changes appear
live in the built-in preview panel. Save a theme, drop it in your app's `Themes/` folder — done.

---

## Features

- **42 semantic color slots** organized by surface group:  
  Content · Sidebar · Control · Input · Accent · Primary / Secondary / Tertiary slots
- **Live preview panel** — every edit is reflected instantly, no Apply button needed
- **Full HSV color picker** — saturation/value canvas, hue bar, brightness bar, alpha bar,
  RGBA sliders, hex input, old/new color swatches
- **Theme metadata** — author name and description saved directly in the file
- **Load & Save** — reads and writes the OXSUIT 1.0 `.oxsuit` format;
  also reads legacy WPF `.xaml` theme files
- **Error handling** — corrupt or invalid theme files show a clear warning;
  the current theme is never lost
- **Claude's Choice** — built-in deep blue-teal starter theme, every color slot unique,
  designed by Claude (Anthropic)
- **Dark UI throughout** — including DWM-themed title bars on Windows 11
- **Options** — configurable themes folder, persisted between sessions

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

## Building from source

```
git clone https://github.com/your-org/Theminator
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
    <token key="CornerRadius" value="6" unit="px"/>
    <token key="BorderWidth"  value="1" unit="px"/>
  </tokens>

</oxsuit>
```

See the [OXSUIT specification](../OXSUIT/SPEC.md) for the complete format reference,
and [`loaders/wpf/`](../OXSUIT/loaders/wpf/) for the WPF loader that reads these files
into a `ResourceDictionary`.

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
