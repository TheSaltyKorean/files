# QuickFiles

A tiny Windows taskbar flyout for the files you keep coming back to. Pin it to
the taskbar, left-click it, and get an instant list of your most recent
**downloads** and **recently opened files** — with the file name, when it was
downloaded/last opened, and its folder.

- Click a file to open it; click the folder path to reveal it in Explorer.
- The flyout pops up directly above the taskbar icon you clicked (works with
  the taskbar docked to any edge, and on any monitor).
- Delete a file straight from the list (trash button — it goes to the Recycle
  Bin, so it's recoverable).
- Configure how many files you see (5–50), the sort order, and which sources
  are included (Downloads folder, Windows recent items).
- Per-user install — no admin rights needed.
- Auto-updates silently in the background from GitHub Releases.

## Install

1. Download **`QuickFiles-win-Setup.exe`** from the
   [latest release](https://github.com/TheSaltyKorean/files/releases/latest).
2. Run it (no elevation prompt — it installs under your user profile).
3. QuickFiles launches and shows the flyout. **Right-click its taskbar icon →
   "Pin to taskbar"** (or find QuickFiles in the Start menu and pin from there).

From then on, a left-click on the pinned icon pops the flyout. Press `Esc` or
click anywhere else to dismiss it. The gear icon opens settings.

The app stays resident in the background so the flyout appears instantly; it
uses negligible memory and you can fully quit it from Settings → "Quit app".

## How it works

- **Downloads**: files in your Downloads folder, timestamped by when they
  arrived.
- **Recently opened**: Windows' own recent-items list
  (`%APPDATA%\Microsoft\Windows\Recent`), so anything you open through
  Explorer or standard file dialogs shows up.
- Duplicates are merged (a file that was downloaded *and* opened appears once,
  with its most recent timestamp).
- Settings are stored in `%LOCALAPPDATA%\QuickFiles\settings.json`.

## Development

Built with .NET 8 / WPF, packaged with [Velopack](https://velopack.io).

```sh
# compiles on Windows, and on Linux/macOS too (EnableWindowsTargeting is set)
dotnet build src/QuickFiles/QuickFiles.csproj
```

### Releasing

Push a version tag; GitHub Actions builds, packages with Velopack, and
publishes the release. Installed apps pick the update up automatically within
a few hours (or immediately via Settings → "Check for updates").

```sh
git tag v0.1.1
git push origin v0.1.1
```
