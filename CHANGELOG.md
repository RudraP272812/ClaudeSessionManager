# Changelog

## v1.0.3

- **Fixed the app not running at all on other machines.** v1.0.0–v1.0.2 shipped only `ClaudeSessionManager.exe`, but the self-contained build actually needed six native DLLs (WPF's rendering/input interop layer, SQLite) sitting next to it — those were never uploaded, so the app couldn't start once downloaded anywhere but this dev machine. The build now sets `IncludeNativeLibrariesForSelfExtract`/`IncludeAllContentForSelfExtract` so everything is bundled into the single `.exe`, and this has been verified by copying only that file to an empty folder and running it from there.

## v1.0.2

- Added a real TraceFix taskbar/app icon (hand-drawn multi-size `.ico`, built directly from the mark's own path data) — the app no longer shows the generic default icon in the taskbar and Alt-Tab.
- Gave session rows more breathing room: wider checkbox column, more row padding, and the group header now lines up with row content instead of sitting flush against the edge.

## v1.0.1

- Fixed the app shell chrome: the branded navbar and the native OS title bar were rendering as two disconnected bars. The TraceFix branding now lives in a real title bar (`ui:TitleBar` with `ExtendsContentIntoTitleBar`), so the icon, name, and window buttons render as one themed bar.
- Fixed session row checkboxes rendering clipped to a sliver (WPF-UI's Fluent checkbox needed more room than the old column width gave it).

## v1.0.0

- Animated TraceFix-branded start screen with a real (not simulated) loading state tied to the session scan.
- New Fluent/Windows-11 app shell (WPF-UI) with a branded top navbar.
- Session list, search, and bulk delete extracted into their own page; scanning/deletion logic unchanged.
- `README.md` rewritten with download, feature, and how-it-works documentation.
- Windows installer script (Inno Setup) and winget/Scoop package manifests added for terminal installs.
- Zero network access — no analytics, no telemetry, no update checks.
