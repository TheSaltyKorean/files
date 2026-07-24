# QuickFiles

A tiny Windows taskbar flyout for the files you keep coming back to. Pin it to
the taskbar, left-click it, and get an instant list of your most recent
**downloads** and **recently opened files** — right above the icon you clicked.

## Install

1. Download **`QuickFiles-win-Setup.exe`** from the
   [latest release](https://github.com/TheSaltyKorean/files/releases/latest).
2. Run it (no elevation prompt — it installs under your user profile; admin
   rights are never required).
3. QuickFiles launches and shows the flyout. **Right-click its taskbar icon →
   "Pin to taskbar"** (or find QuickFiles in the Start menu and pin from there).

From then on, a left-click on the pinned icon pops the flyout directly above
it. Each release also ships `QuickFiles-win-Portable.zip` — a no-install copy
you can run from anywhere (it does not auto-update).

## Using the flyout

Each row shows the file's icon, name, folder location, when it was downloaded
or last opened (hover for the exact date and time), and a "Downloaded" or
"Opened" tag.

| Action | What it does |
| --- | --- |
| Click a row | Opens the file in its default app |
| Folder button (📂, left of the trash can) | Reveals the file in Explorer |
| Click the folder path under the name | Also reveals the file in Explorer |
| Trash button (🗑) | Deletes the file to the **Recycle Bin** (recoverable, so no confirmation prompt) |
| ↻ button | Refreshes the list |
| ⚙ button | Opens Settings |
| `Esc` or click elsewhere | Dismisses the flyout |

The flyout anchors to whichever taskbar edge you use (bottom, top, left,
right) and to whichever monitor you clicked on.

## Settings

Open with the ⚙ button:

- **Number of files to show** — 5 to 50.
- **Sort order** — Newest first, Oldest first, or Name (A–Z). Sorting always
  applies to the *N most recent* files, so "Name" alphabetizes your recent
  files rather than showing the alphabetically-first files of all time.
- **Sources** — toggle the Downloads folder and/or Windows recent items.
- **Check for updates** — applies any pending update immediately.
- **Quit app** — fully exits QuickFiles (it relaunches on the next taskbar
  click; it otherwise stays resident so the flyout appears instantly).

Settings are stored in `%LOCALAPPDATA%\QuickFiles\settings.json`.

## How it works

- **Downloads**: files in your Downloads folder, timestamped by when they
  arrived.
- **Recently opened**: Windows' own recent-items list
  (`%APPDATA%\Microsoft\Windows\Recent`), so anything you open through
  Explorer or standard file dialogs shows up.
- Duplicates are merged — a file that was downloaded *and* opened appears
  once, with its most recent timestamp.
- **Auto-update**: the app checks this repo's GitHub Releases every 4 hours,
  downloads updates (as small deltas), and applies them with a silent
  background restart. No prompts, no elevation.

## Development

Built with .NET 8 / WPF, packaged with [Velopack](https://velopack.io).
Layout:

- `src/QuickFiles/` — the app. `MainWindow` is the flyout, `SettingsWindow`
  the configuration UI.
- `src/QuickFiles/Services/` — `FileScanner` (merges Downloads + Recent),
  `ShellInterop` (Win32/COM: shortcut resolution, file icons, Recycle Bin,
  monitor info), `UpdateService` (Velopack), `AppSettings` (JSON persistence).
- `scripts/make_icon.py` — regenerates the app icon.
- `.github/workflows/release.yml` — CI release pipeline.

```sh
# compiles on Windows, and on Linux/macOS too (EnableWindowsTargeting is set)
dotnet build src/QuickFiles/QuickFiles.csproj
```

The app is single-instance: clicking the pinned icon while it is running
signals the existing process (via a named event) to show the flyout.

### Releasing

Push a version tag; GitHub Actions builds on a Windows runner, packages with
Velopack, and publishes the GitHub Release. Installed apps pick the update up
automatically within a few hours (or immediately via Settings → "Check for
updates"). The repo must remain public — installed apps read the Releases
feed without authentication.

```sh
git tag v0.2.0
git push origin v0.2.0
```

See [CHANGELOG.md](CHANGELOG.md) for release history.
