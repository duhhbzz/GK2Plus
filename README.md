<p align="center">
  <img src="assets/branding/gk2plus-banner.png" alt="GK2+ - Graveyard Keeper Plus">
</p>

<p align="center">
  <strong>A modular, all-in-one quality-of-life and gameplay enhancement suite for Graveyard Keeper 2.</strong>
</p>

<p align="center">
  <a href="CHANGELOG.md">Changelog</a> •
  <a href="CONTRIBUTING.md">Contributing</a> •
  <a href="LICENSE">License</a>
</p>

<p align="center">
  <img alt="Version" src="https://img.shields.io/badge/version-0.0.1-blue">
  <img alt="BepInEx" src="https://img.shields.io/badge/BepInEx-5.4.23.5-purple">
  <img alt=".NET Standard" src="https://img.shields.io/badge/.NET%20Standard-2.1-blueviolet">
  <img alt="Status" src="https://img.shields.io/badge/status-early%20development-orange">
</p>

---

> 🚧 **GK2+ is currently in early development.**
>
> The core plugin, modular feature system, compatibility foundation, versioning, and contribution workflow are in place. Gameplay modules are still being developed.

## What is GK2+?

**GK2+ (Graveyard Keeper Plus)** is designed to be a configurable, all-in-one enhancement suite for **Graveyard Keeper 2**.

The goal is to bring together many of the quality-of-life improvements, gameplay tweaks, and convenience features players commonly request into a single mod.

Rather than forcing one implementation on every player, GK2+ is being built around **independent feature modules**. If you prefer another mod's implementation of a specific mechanic, you should be able to disable the equivalent GK2+ feature and continue using the other mod.

### Design goals

- One primary mod instead of dozens of small tweaks
- Individually configurable gameplay features
- Compatibility-first design
- Sensible vanilla-friendly defaults
- Clear conflict warnings where possible
- Player-facing configuration UI
- Open development and community contributions
- Transparent changelogs and semantic versioning

---

## Current Status

### Foundation

- ✅ BepInEx plugin bootstrap
- ✅ Harmony integration
- ✅ Modular feature registry
- ✅ Master enable/disable configuration
- ✅ Compatibility detection foundation
- ✅ Automatic local deployment during development
- ✅ Semantic versioning
- ✅ Changelog workflow
- ✅ Contribution guidelines

### In Development

- 🚧 Custom in-game GK2+ settings interface
- 🚧 Feature-level configuration and toggles
- 🚧 First gameplay/QoL modules
- 🚧 Known-mod compatibility handling

### Planned Feature Areas

- Inventory
- Storage
- Crafting
- Movement
- Farming
- Automation
- Economy
- User Interface
- Miscellaneous quality-of-life improvements

---

## Modular by Design

GK2+ is structured so major gameplay changes can be independently controlled.

```text
GK2+
├── Core
├── Inventory
├── Storage
├── Crafting
├── Movement
├── Farming
├── Automation
├── Economy
├── UI
└── Misc
```

The long-term goal is for players to be able to disable individual GK2+ mechanics without disabling the entire mod.

This also allows GK2+ to coexist with specialized mods when players prefer another implementation.

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

A feature toggle may not eliminate every possible mod conflict, but coexistence is a core design goal.

---

## Requirements

Current development baseline:

- **Graveyard Keeper 2**
- **BepInEx 5.4.23.5**
- **Harmony**
- **.NET Standard 2.1**
- **Unity 6000.3.x**

Requirements may change as Graveyard Keeper 2 and its modding ecosystem evolve.

---

## Installation

> GK2+ is not yet publicly released.

Installation instructions will be added with the first player-facing release.

Development builds currently deploy to:

```text
BepInEx/plugins/GK2Plus/
```

---

## Configuration

During early development, configuration is exposed through BepInEx configuration support and ConfigurationManager.

The long-term goal is a dedicated in-game **GK2+ settings interface** with:

- category navigation,
- feature toggles,
- configurable values,
- compatibility notices,
- restart-required indicators,
- per-feature descriptions.

---

## Versioning

GK2+ follows **Semantic Versioning**:

```text
MAJOR.MINOR.PATCH
```

The repository root [`VERSION`](VERSION) file is the source of truth for the current version.

Examples:

```text
0.0.x  Early development / foundation work
0.1.0  First usable player-facing feature release
0.x.0  Significant feature milestones
1.0.0  Stable major release
```

See the full [Changelog](CHANGELOG.md) for release history.

---

## Development

Project structure:

```text
src/GK2Plus/
├── Core/
├── Features/
│   ├── Inventory/
│   ├── Storage/
│   ├── Crafting/
│   ├── Movement/
│   ├── Farming/
│   ├── Automation/
│   ├── Economy/
│   └── Misc/
├── Patches/
├── UI/
└── Plugin.cs
```

GK2+ currently builds against the game's managed assemblies and automatically deploys the compiled DLL to the local BepInEx plugin directory during development.

---

## Contributing

GK2+ is open source and community contributions are welcome.

Pull requests are reviewed before merge to help protect:

- stability,
- compatibility,
- maintainability,
- modular design,
- licensing and attribution requirements.

Please read the [Contribution Guidelines](CONTRIBUTING.md) before submitting changes.

---

## Changelog

All notable player-facing changes are documented in:

[CHANGELOG.md](CHANGELOG.md)

Upcoming changes are tracked under the **Unreleased** section until a release is prepared.

---

## License

GK2+ is licensed under the terms in:

[LICENSE](LICENSE)

---

## Credits

Thanks to:

- the Graveyard Keeper 2 modding community,
- the BepInEx contributors,
- the Harmony contributors,
- tool and framework authors supporting the GK2 modding ecosystem,
- everyone who reports bugs, suggests features, tests builds, or contributes code.

---

<p align="center">
  <strong>GK2+ — one mod, your way.</strong>
</p>
