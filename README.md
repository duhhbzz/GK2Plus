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
  <a href="docs/RELEASE_CHECKLIST.md">Release Checklist</a> •
  <a href="docs/THUNDERSTORE.md">Thunderstore</a> •
  <a href="LICENSE">License</a>
</p>

<p align="center">
  <img alt="Version" src="https://img.shields.io/badge/version-0.1.0-blue">
  <img alt="BepInEx" src="https://img.shields.io/badge/BepInEx-5.4.23.5-purple">
  <img alt=".NET Standard" src="https://img.shields.io/badge/.NET%20Standard-2.1-blueviolet">
  <img alt="Status" src="https://img.shields.io/badge/status-first%20gameplay%20release-brightgreen">
</p>

---

## What is GK2+?

**GK2+ (Graveyard Keeper Plus)** is a configurable all-in-one enhancement suite for **Graveyard Keeper 2**.

The project combines quality-of-life improvements, gameplay tweaks, management tools, and optional cheats behind one modular framework. The goal is to reduce the need for many tiny overlapping mods while still allowing players to disable individual GK2+ features when another mod provides an implementation they prefer.

**v0.1.0 is the first functional gameplay release.** It moves GK2+ beyond the original framework/menu preview with a tested Manual Save feature, functional Cheats tools, save-safety checkpoints, and per-save achievement protection for cheat use.

### Design goals

- One primary mod instead of dozens of tiny tweaks
- Independently configurable feature modules
- Compatibility-first design
- Native-style in-game UI
- Save-safe persistent mutations
- Low CPU, GPU, RAM, GC, and disk overhead
- Narrow Harmony patches and small blast radius
- Clear conflict/safety behavior
- Open development and semantic versioning

---

## v0.1.0 Features

### Manual Save

GK2+ adds a native-style **Save Game** button to the in-game pause menu.

The feature delegates the actual save to Graveyard Keeper 2's own save system rather than implementing custom serialization.

Validated behavior includes:

- manual mid-day saves;
- no forced sleep/day transition;
- player location persistence;
- inventory/money persistence;
- moved world-object persistence;
- native saving indicator;
- keyboard/mouse and controller navigation.

The native **Exit to Main Menu** confirmation is also extended with the save slot's real last-save time, for example:

~~~text
Last saved: 10 minutes ago (4:26 AM).
~~~

### Functional Cheats

Press **F2** during gameplay and open the **Cheats** tab.

Current actions:

~~~text
Silver
+1   +5   +10   +100

Gold
+1   +5   +10   +100

Heal Player
Refill Energy
~~~

Money changes use GK2's native resource path, including the game's normal money-change feedback.

**Refill Energy** targets the normal work/action energy resource and has been runtime validated.

**Heal Player** uses GK2's native full-heal path, but still needs a hands-on damage-state validation pass before it is considered fully verified.

### Cheat / Achievement Integrity

Using the Cheats tab is intentionally not consequence-free.

On the **first cheat used on a save**, GK2+ shows a confirmation explaining that platform achievements will be disabled for that save and its GK2+ backup lineage.

If confirmed:

- the active save receives a persistent GK2+ cheat-taint sidecar;
- retained GK2+ backups for that slot are marked tainted;
- future backups inherit the taint marker;
- future cheat actions on that save do not ask again;
- GK2+ blocks the game's platform achievement progress/unlock boundary while that tainted save is active.

GK2+ does **not** modify Graveyard Keeper 2's serialized save schema to store this marker.

The taint system is an integrity feature, not DRM. A user who deliberately removes GK2+, deletes metadata, or manually manipulates files can bypass a mod-level restriction.

---

## In-Game UI

GK2+ uses a persistent UI controller attached to the game's persistent GUI root.

Press:

~~~text
F2  Open / close GK2+
ESC Close GK2+
~~~

The menu works from both:

- the main menu;
- active gameplay.

Current top-level tabs:

- General
- Inventory
- Crafting
- Farming
- Zombies
- Cheats
- More

The overlay has its own sorting Canvas so native windows and prompts do not unexpectedly render over GK2+ while it is open.

---

## Save Safety

Persistent mutations should not directly change a live save without a safety gate.

GK2+ uses a generation/checkpoint model:

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

GK2+ currently retains **5 backup directories per save slot**.

Money cheats are Moderate-risk persistent mutations and use this checkpoint system. Repeated money actions before another native save reuse the same checkpoint instead of repeatedly copying the save.

Cheat-tainted slots propagate their taint marker to GK2+ backups.

See [docs/SAVE_SAFETY.md](docs/SAVE_SAFETY.md).

---

## Performance and Resource Use

Performance is an architecture requirement.

GK2+ follows these rules:

- no heavy work every frame;
- prefer native game events over polling;
- cache expensive lookups;
- pair subscriptions with cleanup;
- keep hidden UI idle;
- avoid reflection/LINQ/large allocations in hot paths;
- keep Harmony patches narrow and cheap;
- avoid production recon/log spam;
- bound backup retention;
- reuse save checkpoints instead of performing duplicate disk writes.

The F2 menu is retained rather than rebuilt on every open.

See [docs/PERFORMANCE.md](docs/PERFORMANCE.md).

---

## Installation

### Requirements

- **Graveyard Keeper 2** on Windows
- **BepInEx 5.4.23.5**

### Manual installation

1. Install BepInEx for Graveyard Keeper 2.
2. Download the GK2+ release archive.
3. Extract it into the **Graveyard Keeper 2** installation directory.
4. Confirm this file exists:

~~~text
Graveyard Keeper 2/BepInEx/plugins/GK2Plus/GK2Plus.dll
~~~

5. Launch the game normally.
6. Confirm the **GK2+** status badge appears on the main menu.
7. Press **F2** to open GK2+.

### Uninstall

Delete:

~~~text
BepInEx/plugins/GK2Plus/
~~~

Optional GK2+ configuration and safety backups live under:

~~~text
BepInEx/config/GK2Plus/
~~~

The per-save cheat-taint sidecar is stored beside the corresponding native GK2 save. Removing the plugin does not automatically remove GK2+ metadata or backups.

---

## Distribution

GK2+ uses one version number across distribution channels.

### GitHub

GitHub Releases are the canonical release history, source/tag reference, changelog, and downloadable release archive.

### Nexus Mods

Nexus Mods is a player-facing discovery/download channel. The same release archive used for the GitHub release should be uploaded to Nexus.

### Thunderstore / R2ModMan

The **Graveyard Keeper 2** Thunderstore community is live.

GK2+ includes a dedicated Thunderstore package builder that wraps the same release DLL with Thunderstore's required manifest, README, icon, and BepInEx dependency metadata.

The v0.1.0 manifest depends on `BepInEx-BepInExPack-5.4.2305`, which provides the BepInEx 5.4.23.5 runtime used by GK2+.

See [docs/THUNDERSTORE.md](docs/THUNDERSTORE.md).

---

## Compatibility Philosophy

GK2+ is not intended to force players into an all-or-nothing mod setup.

Where practical, GK2+ will:

- detect known overlapping mods;
- warn about possible conflicts;
- allow conflicting GK2+ modules to be disabled;
- avoid touching unrelated game systems;
- use narrowly scoped Harmony patches;
- document known incompatibilities.

A feature toggle cannot guarantee compatibility with every third-party patch, but coexistence is a core project goal.

---

## Planned Feature Areas

Current development direction includes:

~~~text
GK2+
├── General / UI
├── Inventory / Storage
├── Crafting
├── Farming
├── Automation
├── Economy
├── Zombies
├── Cheats
├── Quest / Map tools
└── Misc
~~~

Near-term planned work includes continuous planting, storage/crafting improvements, zombie management, quest tracking, and additional carefully gated convenience/cheat tools.

Planned items may change as game systems are researched and tested.

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

Release tooling lives under:

~~~text
tools/release/
~~~

Development standards:

- [Performance and Resource Standards](docs/PERFORMANCE.md)
- [Save Safety](docs/SAVE_SAFETY.md)
- [Release Checklist](docs/RELEASE_CHECKLIST.md)
- [Thunderstore / R2ModMan Plan](docs/THUNDERSTORE.md)
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
0.1.0  First functional gameplay release
0.x.0  Significant feature milestones
1.0.0  Stable major release
~~~

See [CHANGELOG.md](CHANGELOG.md).

---

## Reporting Bugs

Use GitHub Issues:

https://github.com/duhhbzz/GK2Plus/issues

Please include:

- GK2+ version;
- Graveyard Keeper 2 version;
- BepInEx version;
- other installed mods;
- reproduction steps;
- relevant BepInEx log output.

For performance problems, include what was happening when CPU/RAM/disk behavior changed and whether the issue grows over time.

---

## Contributing

Community contributions are welcome.

Please read [CONTRIBUTING.md](CONTRIBUTING.md) before submitting changes.

---

## Support Development

If you enjoy GK2+ and want to support continued development, testing, and future features:

[☕ Buy Me a Coffee](https://buymeacoffee.com/duhhbzz)

GK2+ is free and open source. Donations are completely optional and do not gate features or support.

---

## License

See [LICENSE](LICENSE).

---

## Credits

Thanks to the Graveyard Keeper 2 modding community, the BepInEx and Harmony contributors, and everyone who tests builds, reports bugs, suggests features, or contributes code.
