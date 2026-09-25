# Changelog

All notable changes to **GK2+ (Graveyard Keeper Plus)** are documented here.

GK2+ follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- Functional Cheats tab with **+1/+5/+10/+100 Silver**, **+1/+5/+10/+100 Gold**, **Heal Player**, and **Refill Energy**.
- Reusable UI action registration so feature modules can expose controls without embedding feature logic in `ModMenuController`.
- Per-save cheat taint sidecar that permanently marks a save lineage after the first confirmed cheat use.
- Achievement integrity guard that blocks GK2's platform achievement progress/unlock boundary for cheat-tainted saves.
- First-cheat native confirmation dialog warning that achievements will be disabled for the save and its GK2+ backups.
- Cheat-taint propagation to existing and future GK2+ safety backups.
- Optional **Manual Save** feature: adds a native-style `Save Game` button to the in-game pause menu, delegates persistence to GK2's own `SaveSystem`, and augments the Exit to Main Menu confirmation with the native last-save age.
- Persistent GK2+ F2 menu lifecycle across the main menu and active gameplay.
- Dedicated top-level overlay Canvas so the GK2+ menu renders above native game windows while open.
- Save-safety framework for protected persistent mutations.
- Save mutation risk classifications for Low, Moderate, and High-risk actions.
- On-demand save checkpoints that copy the active slot data/info files before Moderate/High-risk mutations.
- Per-slot backup retention capped at five backup directories.
- Performance/resource standards covering CPU, GPU, RAM, GC, disk I/O, logging, and lifecycle cleanup.
- Save-safety architecture documentation.

### Changed

- GK2UIService now owns the persistent mod-menu controller lifecycle.
- GK2+ menu hosting moved from a main-menu-specific presentation hierarchy to the persistent game GUI root.
- Save-safety backups now use a generation/checkpoint model so repeated risky actions reuse one backup until GK2 loads or writes a save.
- Backup copies are streamed instead of buffered as whole-save managed byte arrays.
- Development/release guidance now treats performance and resource efficiency as release requirements.

### Fixed

- F2 menu no longer disappears after entering gameplay.
- Vanilla windows and interaction prompts no longer render above the GK2+ overlay.
- TMP/native-label initialization failure introduced during the first persistent-UI refactor.
- UI shutdown handling during Unity teardown.

### Compatibility

- Persistent UI changes remain isolated to GK2+'s own overlay rather than injecting tabs directly into vanilla windows.
- Runtime recon remains a development-only tool and is not required for normal GK2+ operation.

### Known Issues

- Additional gameplay/QoL feature modules and cheat actions are still under development.
- Heal Player still needs runtime validation once the test save can take damage.
- Achievement blocking/taint persistence still needs runtime validation before release.
- Automated backup restore is intentionally not implemented yet.


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
