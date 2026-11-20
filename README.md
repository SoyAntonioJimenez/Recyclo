# Recyclo

Recyclo puts the recycle bin in the Windows notification area, next to the clock, and
lets you empty it from there. The icon shows at a glance whether the bin has something
in it.

![Recyclo](docs/hero.png)

Windows 10 and 11.

## Why

I like the desktop to hold only what I am working on, so the recycle bin was the first
icon to go. Removing it left the bin three clicks away inside Explorer, which is worse
than having it on the desktop. The notification area is always visible and never in
the way.

## Features

- Empty and full states in the tray icon, drawn at the exact size Windows asks for.
- Follows the system theme: the icon, the menu and the confirmation dialog switch
  between light and dark.
- Material-style right-click menu instead of the stock WinForms look.
- *Empty recycle bin* shows the number of items and is disabled when the bin is empty.
- Tooltip with the item count and the space it takes.
- Double-click opens the recycle bin.
- Autostart with Windows, toggleable from the menu.
- Refreshes every 1.5 seconds, so it changes as soon as you delete or restore something.
- One executable, no installer and no services.

## Install

Download one of these from [Releases](../../releases):

| File | Requirements |
|---|---|
| `RecycloTray.exe` | None, the runtime is bundled (about 53 MB) |
| `RecycloTray-net.exe` | .NET 11 or newer (under 1 MB) |

Put it in a folder you keep, for example `%LOCALAPPDATA%\Programs\RecycloTray\`, and
run it. It registers itself to start with Windows the first time.

Windows 11 hides new tray icons in the overflow menu. Drag the icon onto the taskbar,
or open *Settings → Personalization → Taskbar → Other system tray icons* and enable
Recyclo there.

## Usage

| Action | Result |
|---|---|
| Right-click | Opens the menu |
| Open recycle bin | Opens the native window |
| Empty recycle bin (n) | Asks for confirmation, then empties every drive |
| Refresh | Re-reads the bin state |
| Start with Windows | Toggles the autostart entry |
| Exit | Closes the application |
| Double-click | Opens the recycle bin |

## How it works

- `SHQueryRecycleBin` returns the combined state of the bin on every drive.
- `SHEmptyRecycleBin` empties it without the native dialogs; Recyclo draws its own
  confirmation and count.
- The icons are drawn with GDI+ and packed into multi-size `.ico` files by a small
  writer in `IcoWriter.cs`, so the repository ships no image assets and every size is
  rendered at its exact resolution.
- The tray icon is requested at `SystemInformation.SmallIconSize` so the shell never
  rescales it.
- The menu is a `ContextMenuStrip` with a custom renderer that reads its palette from
  `SystemUsesLightTheme`.

One detail that cost me an afternoon: on Windows 11, `SHQUERYRBINFO` has to be
declared with natural alignment (24 bytes). With `Pack = 4` it is 20 bytes,
`SHQueryRecycleBin` returns `E_INVALIDARG` and the bin always looks empty.

## Build

```powershell
git clone https://github.com/SoyAntonioJimenez/Recyclo.git
cd Recyclo
dotnet build -c Release
dotnet run -c Release
```

Publishing:

```powershell
# Single file, includes the runtime
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true -o dist

# Needs .NET 11
dotnet publish -c Release -r win-x64 --self-contained false `
  -p:PublishSingleFile=true -o dist-light
```

Pushing a `v*` tag makes GitHub Actions build both executables and publish them.

There are also a few command-line modes, useful for scripts or for checking the icon
while it is being designed:

| Command | Result |
|---|---|
| `RecycloTray.exe --dump [file]` | Writes the bin state as JSON and exits |
| `RecycloTray.exe --empty` | Empties the bin silently |
| `RecycloTray.exe --preview [png]` | Renders a contact sheet of every state and size |
| `RecycloTray.exe --menu [png] [light\|dark]` | Renders the context menu |
| `RecycloTray.exe --make-icon [ico]` | Regenerates the application icon |

## License

MIT © 2026 Antonio Jiménez Abadía.
