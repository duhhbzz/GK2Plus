# Changelog

All notable changes to **GK2+ (Graveyard Keeper Plus)** are documented here.

GK2+ follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- Configurable GK2+ menu hotkey with F2 as the default.
- In-game **Change Hotkey** control on the More tab with live input capture.
- Native GK2 keyboard-binding conflict detection with options to keep both bindings or move the key to GK2+ and unbind the vanilla action.

### Changed

- The GK2+ header and main-menu badge now display the configured menu hotkey dynamically.

### Fixed

### Compatibility

### Known Issues

---

## [0.1.0] - 2026-09-25

### Added

- First functional gameplay release of GK2+.
- Native-style **Save Game** button in the in-game pause menu.
- Manual saves use GK2's own save system and preserve mid-day player/world state without forcing sleep or advancing the day.
- Exit-to-main-menu confirmation now shows the active slot's real last-save age/time.
- Functional Cheats tab with **+1/+5/+10/+100 Silver**, **+1/+5/+10/+100 Gold**, **Heal Player**, and **Refill Energy**.
- Reusable UI action registration so feature modules can expose controls without embedding feature logic in `ModMenuController`.
- Per-save cheat-taint sidecar that permanently marks a save lineage after the first confirmed cheat use.
- Achievement integrity guard that blocks GK2's platform achievement progress/unlock boundary while a cheat-tainted save is active.
- First-cheat native confirmation dialog warning that achievements will be disabled for the save and its GK2+ backups.
- Cheat-taint propagation to existing and future GK2+ safety backups.
- Persistent GK2+ F2 menu lifecycle across both the main menu and active gameplay.
- Dedicated top-level overlay Canvas so GK2+ renders above native game windows while open.
- Save-safety framework for protected persistent mutations.
- Save mutation risk classifications for Low, Moderate, and High-risk actions.
- On-demand save checkpoints that copy the active slot data/info files before Moderate/High-risk mutations.
- Per-slot backup retention capped at five backup directories.
- Performance/resource standards covering CPU, GPU, RAM, GC, disk I/O, logging, and lifecycle cleanup.
- Save-safety architecture documentation.

### Changed

- GK2UIService now owns the persistent mod-menu controller lifecycle.
- GK2+ menu hosting moved from a main-menu-specific presentation hierarchy to the persistent game GUI root.
- Save-safety backups use a generation/checkpoint model so repeated risky actions reuse one backup until GK2 loads or writes a save.
- Backup copies are streamed instead of buffered as whole-save managed byte arrays.
- Money cheats use GK2's native money resource path and native resource-change feedback.
- Refill Energy targets the normal work/action energy resource.
- Cheat actions fail closed if the achievement guard cannot initialize.
- Development/release guidance now treats performance, resource efficiency, and cheat-integrity behavior as release requirements.

### Fixed

- F2 menu no longer disappears after entering gameplay.
- Vanilla windows and interaction prompts no longer render above the GK2+ overlay.
- TMP/native-label initialization failure introduced during the first persistent-UI refactor.
- UI shutdown handling during Unity teardown.
- Pause-menu Save Game label no longer reverts to Settings after localization callbacks.
- Cheat action layout now wraps into compact rows instead of overflowing one row.
- Refill action now restores the correct player energy resource rather than combat stamina.

### Validation

Runtime validation completed for:

- Manual Save from the pause menu.
- Manual save reload at the same player location without sleeping.
- Persistence of moved world-object state across manual save/reload.
- Native last-save timestamp display before and after manual save.
- Keyboard/mouse and controller navigation for the pause-menu Save Game flow.
- Silver/Gold cheat increments and native money-change feedback.
- One Moderate-risk safety checkpoint for repeated money actions within the same save generation.
- Reuse of that checkpoint across multiple money actions.
- Refill Energy restoring the visible work-energy bar.
- First-cheat warning dialog.
- Persistent active-slot cheat-taint sidecar.
- Cheat-taint persistence after closing/reopening.
- Propagation of cheat taint to all five retained GK2+ backup directories.

### Compatibility

- Persistent UI changes remain isolated to GK2+'s own overlay rather than injecting tabs directly into vanilla windows.
- Pause-menu Save Game integration clones/reuses native-style controls and delegates persistence to GK2's own save system.
- Achievement protection patches only the platform progress/unlock boundary and leaves GK2's internal achievement bookkeeping intact.
- Runtime recon remains a development-only tool and is not required for normal GK2+ operation.

### Known Issues

- **Heal Player** uses GK2's native full-heal path and validates successfully at full health, but still needs a hands-on test while the player is actually damaged.
- Cheat-taint persistence and the achievement guard are implemented and validated at the save/backup level; a naturally triggered platform achievement attempt has not yet been observed in runtime testing.
- Additional gameplay/QoL modules and cheat actions are still under development.
- Automated backup restore is intentionally not implemented yet.
- The in-game Nexus Mods button remains disabled until the restored/public Nexus page URL is configured.

---

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

- The custom F2 menu in v0.0.1 works from the main menu only.
- Gameplay/QoL modules and cheat actions are not included in v0.0.1.
- Feature settings pages show development placeholders.
- The Nexus Mods button is disabled in v0.0.1.
