# Changelog

## 1.1.0

- renamed the app from StickyNote App to JotTile
- replaced Enter-to-finalize with an explicit Edit/Save workflow
- added committed-text save semantics so typing no longer mutates persisted note text
- added tray lifecycle with `New note`, `Show notes`, and `Exit`
- added `config.exe` for appearance, behavior, and startup settings
- added settings live-apply, backup recovery, migration from `SimpleStickyNotes`, and local logging
- split the repo into `JotTile.Core`, `JotTile`, `JotTile.Config`, and `tests/JotTile.Tests`
- added automated tests and a Windows CI workflow
