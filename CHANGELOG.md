# Changelog

All notable changes to **GK2+ (Graveyard Keeper Plus)** will be documented in this file.

GK2+ follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

### Changed

### Fixed

### Compatibility

### Known Issues


## [0.0.1] - 2026-09-23

### Added

- Initial GK2+ BepInEx plugin bootstrap.
- Modular feature architecture for independently configurable gameplay changes.
- Feature registry foundation.
- Per-feature configuration foundation.
- Master GK2+ enable/disable toggle.
- Compatibility manager foundation for detecting other installed BepInEx plugins.
- Initial module structure for:
  - Inventory
  - Storage
  - Crafting
  - Movement
  - Farming
  - Automation
  - Economy
  - Miscellaneous features
- UI and menu development structure.
- Harmony patching foundation.
- Automatic local deployment of `GK2Plus.dll` to the BepInEx plugins directory during development.
- Initial GitHub and Nexus Mods project branding.

### Compatibility

- Built for Graveyard Keeper 2.
- Tested with BepInEx 5.4.23.5.
- Tested with Unity 6000.3.9.
- Targets .NET Standard 2.1.

### Known Issues

- No gameplay enhancement modules are included yet.
- The custom GK2+ configuration interface has not yet been implemented.
- Configuration currently relies on BepInEx configuration support / ConfigurationManager during development.
