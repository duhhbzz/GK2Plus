# Changelog

All notable changes to **GK2+ (Graveyard Keeper Plus)** are documented here.

GK2+ follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

### Changed

### Fixed

### Compatibility

### Known Issues


## [0.0.1] - 2026-09-24

### Added

- Initial public GK2+ foundation release.
- BepInEx plugin bootstrap and Harmony integration foundation.
- Modular feature registry and feature base architecture.
- Master GK2+ enable/disable configuration.
- Compatibility scanner for loaded BepInEx plugins.
- Shared framework service scaffold for events, saves, world access, inventory, crafting, farming, zombies, quests, localization, UI, and diagnostics.
- Native-style GK2+ main-menu status badge showing the loaded version and status.
- Native-style F2 mod-menu shell on the Graveyard Keeper 2 main menu.
- Esc and Close-button handling for the menu shell.
- General, Inventory, Crafting, Farming, Zombies, Cheats, and More menu tabs.
- GitHub and bug-report actions on the More page.
- Public reconnaissance tooling under `tools/recon/`.
- Central project-link configuration so the Nexus Mods URL can be enabled after the public page is created.

### Compatibility

- Built for Graveyard Keeper 2 on Windows x64.
- Development baseline uses BepInEx 5.4.23.5.
- Development baseline uses Unity 6000.3.x.
- Targets .NET Standard 2.1.
- Gameplay feature modules are not enabled in this release, minimizing gameplay-system conflicts in the foundation build.

### Known Issues

- The custom F2 menu currently works from the main menu but does not yet persist into active gameplay scenes.
- Gameplay/QoL modules and cheat actions are not included yet.
- Feature settings pages currently show development placeholders.
- The Nexus Mods button remains disabled until the public Nexus page exists; it is intended to be enabled in v0.0.2.
