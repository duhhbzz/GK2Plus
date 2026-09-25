<p align="center">
  <img src="assets/branding/gk2plus-banner.png" alt="GK2+ - Graveyard Keeper Plus">
</p>

<p align="center">
  <strong>One mod, your way.</strong><br>
  A modular quality-of-life and gameplay enhancement suite for Graveyard Keeper 2.
</p>

<p align="center">
  <a href="CHANGELOG.md">Changelog</a> •
  <a href="CONTRIBUTING.md">Contributing</a> •
  <a href="docs/PERFORMANCE.md">Performance</a> •
  <a href="docs/SAVE_SAFETY.md">Save Safety</a> •
  <a href="LICENSE">License</a>
</p>

<p align="center">
  <img alt="Version" src="https://img.shields.io/badge/version-0.0.1-blue">
  <img alt="BepInEx" src="https://img.shields.io/badge/BepInEx-5.4.23.5-purple">
  <img alt=".NET Standard" src="https://img.shields.io/badge/.NET%20Standard-2.1-blueviolet">
  <img alt="Status" src="https://img.shields.io/badge/status-foundation%20preview-orange">
</p>

---

> **GK2+ is still in early development.**
>
> The repository may contain unreleased work beyond the latest packaged build. Current development source includes a persistent F2 menu that works from both the main menu and active gameplay. Gameplay/QoL modules are still being implemented and validated.

## What is GK2+?

**GK2+ (Graveyard Keeper Plus)** is a configurable, all-in-one enhancement suite for **Graveyard Keeper 2**.

The goal is to bring quality-of-life improvements, gameplay tweaks, convenience features, optional cheats, and useful management tools into one modular mod without forcing every feature on every player.

Major systems are being built as independent modules. Where technically practical, players will be able to disable a GK2+ feature and continue using another mod's implementation instead.

### Design goals

- One primary mod instead of dozens of tiny tweaks
- Independently configurable feature modules
- Compatibility-first design
- Sensible, vanilla-friendly defaults
- Native-style in-game configuration UI
- Save-safe persistent mutations
- Low CPU, GPU, RAM, and disk overhead
- Clear conflict warnings where possible
- Open development and community contributions
- Transparent changelogs and semantic versioning

---

## Current Development State

The current source tree includes:

- BepInEx plugin bootstrap
- Harmony integration foundation
- Modular feature registry and feature base classes
- Master GK2+ enable/disable configuration
- Compatibility scan for loaded BepInEx plugins
- Shared framework services for:
  - events
  - saves
  - world access
  - inventory
  - crafting
  - farming
  - zombies
  - quests
  - localization
  - UI
  - diagnostics
- Native-style GK2+ badge on the Graveyard Keeper 2 main menu
- Persistent F2 GK2+ menu shell from:
  - the main menu
  - active gameplay
- Esc and Close-button handling
- Dedicated overlay Canvas so GK2+ renders above native game windows while open
- Category tabs for:
  - General
  - Inventory
  - Crafting
  - Farming
  - Zombies
  - Cheats
  - More
- GitHub and bug-report links from the More tab
- Save-safety infrastructure for future persistent mutations
- Public reconnaissance tooling under tools/recon/

### Not included yet

- Finished gameplay-changing QoL modules
- Functional cheat actions
- Final feature settings/toggles inside the custom menu
- Automated backup restore

These are development targets, not missing dependencies.

---

## UI Lifecycle

GK2+ uses a persistent UI controller and attaches the menu to the game's persistent GUI root rather than a main-menu-only hierarchy.

The menu is built once, retained, and shown/hidden with **F2**.

Current validated behavior:

~~~text
Launch
→ F2 on main menu
→ load save
→ F2 during gameplay
→ Esc / Close
→ open over native game windows
→ return to main menu
→ F2 again
~~~

The GK2+ overlay has its own Canvas/sorting order so vanilla windows and interaction prompts do not render over the mod menu.

---

## Save Safety

Persistent cheats and progression tools should not directly mutate a live save without a safety gate.

GK2+ uses a checkpoint model:

~~~text
Normal launch/load/save
→ no GK2+ backup writes

First Moderate/High-risk GK2+ action
→ create one safety checkpoint

More risky actions
→ reuse that checkpoint

GK2 loads or writes a save
→ invalidate checkpoint

Next risky action
→ create one new checkpoint
~~~

Backups are stored under:

~~~text
BepInEx/config/GK2Plus/SaveBackups/<slot>/
~~~

The current retention target is **5 backups per save slot**.

Save files are copied as streams rather than loaded into one large managed buffer, reducing unnecessary RAM pressure. GK2+ also avoids a second full-file checksum pass solely for backup verification.

See [docs/SAVE_SAFETY.md](docs/SAVE_SAFETY.md) for the full design.

---

## Performance and Resource Use

Performance is an architecture requirement for GK2+.

The project follows these rules:

- no heavy work every frame;
- prefer native game events over polling;
- cache expensive lookups;
- every event subscription must be unsubscribed;
- persistent objects must have explicit ownership and cleanup;
- hidden UI should not perform background work;
- avoid reflection/LINQ/large allocations in hot paths;
- keep Harmony patches narrow and cheap;
- no continuous recon/log writes in normal production use;
- cap backup retention and deduplicate backup writes.

The current menu is retained rather than rebuilt every time F2 is pressed.

See [docs/PERFORMANCE.md](docs/PERFORMANCE.md).

---

## Installation

### Requirements

- **Graveyard Keeper 2** on Windows
- **BepInEx 5.4.23.5**

### Install GK2+

1. Install BepInEx for Graveyard Keeper 2.
2. Download the GK2+ release archive.
3. Extract the archive into your **Graveyard Keeper 2** installation directory.
4. Confirm this file exists:

~~~text
Graveyard Keeper 2/BepInEx/plugins/GK2Plus/GK2Plus.dll
~~~

5. Launch the game normally.
6. Look for the **GK2+** status badge on the main menu.
7. Press **F2** to open the GK2+ menu.

Current development source supports F2 from both the main menu and active gameplay.

### Uninstall

Delete:

~~~text
BepInEx/plugins/GK2Plus/
~~~

Optional GK2+ configuration/backups live under:

~~~text
BepInEx/config/GK2Plus/
~~~

Deleting the plugin does not automatically delete those user-created/configuration files.

---

## Planned Feature Areas

GK2+ is structured around several feature categories:

~~~text
GK2+
├── Inventory
├── Storage
├── Crafting
├── Movement
├── Farming
├── Automation
├── Economy
├── Zombies
├── Cheats
├── UI
└── Misc
~~~

Planned work includes continuous planting, storage/crafting improvements, zombie management, quest tracking, convenience options, and optional cheat utilities.

Planned items may change as the game is researched and tested.

---

## Compatibility Philosophy

GK2+ is not intended to force players into an all-or-nothing mod setup.

Where technically practical, GK2+ will:

- detect known overlapping mods,
- warn players about possible conflicts,
- allow conflicting GK2+ modules to be disabled,
- avoid touching unrelated game systems,
- use narrowly scoped Harmony patches,
- document known incompatibilities.

A feature toggle cannot guarantee compatibility with every third-party patch, but coexistence is a core project goal.

---

## Configuration

The base plugin currently exposes a master enable/disable option through BepInEx configuration.

The custom GK2+ interface is being built to eventually provide:

- category navigation,
- per-feature toggles,
- configurable values,
- compatibility notices,
- restart-required indicators,
- feature descriptions,
- optional cheat tools,
- backup/safety status.

---

## Development

Repository structure:

~~~text
src/GK2Plus/
├── Core/
├── Features/
├── Framework/
│   ├── Crafting/
│   ├── Diagnostics/
│   ├── Events/
│   ├── Farming/
│   ├── Inventory/
│   ├── Localization/
│   ├── Quests/
│   ├── Saves/
│   ├── UI/
│   ├── World/
│   └── Zombies/
├── Patches/
├── UI/
└── Plugin.cs
~~~

Public reconnaissance helpers live under:

~~~text
tools/recon/
~~~

Development standards:

- [Performance and Resource Standards](docs/PERFORMANCE.md)
- [Save Safety](docs/SAVE_SAFETY.md)
- [Release Checklist](docs/RELEASE_CHECKLIST.md)
- [Contributing](CONTRIBUTING.md)

Game assemblies, decompiled source, extracted proprietary assets, private runtime reports, and local development paths are not distributed with the project.

---

## Versioning

GK2+ follows **Semantic Versioning**:

~~~text
MAJOR.MINOR.PATCH
~~~

The repository-root [VERSION](VERSION) file is the single source of truth.

~~~text
0.0.x  Foundation / early development releases
0.1.0  First meaningful gameplay/QoL release target
0.x.0  Significant feature milestones
1.0.0  Stable major release
~~~

See [CHANGELOG.md](CHANGELOG.md) for release history and unreleased development changes.

---

## Contributing

GK2+ is open source and community contributions are welcome.

Pull requests are reviewed for build correctness, in-game behavior, regression risk, compatibility, architecture, maintainability, licensing, attribution, and resource efficiency.

Please read [CONTRIBUTING.md](CONTRIBUTING.md) before submitting changes.

---

## Reporting Bugs

Use GitHub Issues:

https://github.com/duhhbzz/GK2Plus/issues

Please include the GK2+ version, game version, BepInEx version, other installed mods, reproduction steps, and relevant BepInEx log output.

For performance issues, also include what you were doing when CPU/RAM/disk behavior changed and whether the issue grows over time.

---

## License

See [LICENSE](LICENSE).

---

## Credits

Thanks to the Graveyard Keeper 2 modding community, the BepInEx contributors, the Harmony contributors, and everyone who tests builds, reports bugs, suggests features, or contributes code.
