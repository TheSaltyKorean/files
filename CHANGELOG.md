# Changelog

## v0.1.3 — 2026-07-24

- Added a folder button on each row (left of the trash can) that reveals the
  file in Explorer.
- Documentation overhaul: README now covers all flyout actions, settings, and
  project layout; added this changelog.

## v0.1.2 — 2026-07-24

- Readable rows: the file name now owns the row width (two full-width lines —
  name + compact time, folder + origin tag).
- Fixed a date-format bug that rendered "Wed P" instead of a proper time.
- Compact timestamps ("3:23 PM", "Yesterday", "Wed", "Jul 3"); hover for the
  full date and time.

## v0.1.1 — 2026-07-24

- The flyout now opens directly above the taskbar icon you clicked (any
  taskbar edge, any monitor, per-monitor DPI aware).
- Added delete-to-Recycle-Bin (trash button on each row).

## v0.1.0 — 2026-07-24

- Initial release: taskbar flyout listing recent Downloads + Windows recent
  items with name, timestamp, and folder; click to open, folder link to
  reveal in Explorer.
- Configurable item count (5–50), sort order, and sources.
- Per-user Velopack installer (no elevation) with silent auto-updates from
  GitHub Releases; CI publishes on version tags.
