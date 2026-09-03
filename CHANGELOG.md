# Changelog

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
