# Contributing to GK2+

Thanks for your interest in contributing to **GK2+ (Graveyard Keeper Plus)**.

GK2+ is a modular, configurable quality-of-life and gameplay enhancement suite for **Graveyard Keeper 2**. Community pull requests are welcome, but changes are reviewed before merge to protect stability, compatibility, maintainability, licensing, and the project's modular design.

---

## Core Contribution Principles

Contributions should:

- keep features modular whenever practical,
- avoid unnecessary changes to unrelated game systems,
- preserve compatibility with existing GK2+ modules,
- prefer configurable behavior over hard-coded behavior,
- allow major gameplay features to be disabled independently where practical,
- avoid unnecessary conflicts with other mods,
- keep player-facing behavior documented,
- keep code readable and maintainable,
- be tested before submission.

GK2+ should remain a mod suite where players choose the features they want rather than being forced into one playstyle.

---

## Development Baseline

GK2+ currently targets:

- Graveyard Keeper 2
- BepInEx 5.4.23.5
- Harmony
- .NET Standard 2.1
- Unity 6000.3.x

Exact supported versions may change as the game and modding ecosystem evolve.

---

## Project Structure

```text
src/GK2Plus/
├── Core/          # plugin metadata, feature system, compatibility metadata
├── Features/      # player-facing gameplay/QoL modules
├── Framework/     # adapters/services that talk to game systems
├── Patches/       # shared/narrow patch infrastructure when needed
├── UI/            # feature-facing UI structure
└── Plugin.cs      # BepInEx entry point
```

The intended separation is:

```text
Framework = how GK2+ talks to the game
Feature   = behavior GK2+ provides
Patch     = smallest unavoidable interception
```

New gameplay functionality should normally live under the appropriate `Features` category. Shared access to game systems should be implemented behind `Framework` services rather than duplicated across features.

---

## Feature Requirements

Major gameplay features should normally:

1. Have a stable feature ID.
2. Have a player-readable name and description.
3. Register through the GK2+ feature system.
4. Expose an enable/disable setting when technically practical.
5. Avoid patching unrelated methods.
6. Document known compatibility concerns.
7. Fail safely when possible.
8. Avoid assuming every other GK2+ feature is enabled.

Features should be designed with coexistence in mind.

---

## Mod Compatibility

GK2+ is designed to coexist with other Graveyard Keeper 2 mods whenever practical.

If your contribution overlaps with another known mod:

- document the overlap,
- avoid unnecessary patch conflicts,
- provide a way to disable the GK2+ implementation when practical,
- note known incompatibilities in the pull request.

Do not intentionally block, remove, or interfere with another mod unless required to prevent a confirmed technical failure. Compatibility warnings and player choice are preferred.

---

## Harmony Patching Guidelines

When Harmony is required:

- patch the narrowest method necessary,
- avoid broad patches when a smaller interception will work,
- avoid modifying unrelated behavior,
- prefer prefixes/postfixes over transpilers when they are sufficient,
- document why the patch is required,
- consider other Harmony patches that may target the same method,
- make toggleable feature patches respect their feature state.

Do not jump directly into broad patching when a framework/API path exists.

---

## UI Contributions

GK2+ is building a native-style in-game interface.

UI contributions should:

- preserve the established visual direction,
- remain usable at common resolutions,
- avoid blocking normal game controls when closed,
- group settings by feature/category,
- clearly identify restart-required settings,
- reuse game-native UI patterns/assets at runtime when appropriate without redistributing proprietary assets.

The current menu lifecycle is still being improved; avoid tightly coupling new feature UI to the main-menu hierarchy.

---

## Reconnaissance and Game Internals

Public recon **tooling** may be contributed under:

```text
tools/recon/
```

Do **not** commit private/proprietary recon output such as:

- decompiled game source,
- game DLLs,
- extracted game assets,
- raw architecture dumps,
- private deep-dive reports,
- runtime reports containing local paths or proprietary data.

Game internals may be referenced by name where necessary for legitimate mod interoperability, but proprietary game content must not be redistributed.

---

## Configuration

Player-facing settings should:

- use clear names,
- include useful descriptions,
- have sensible defaults,
- be exposed through the GK2+ UI when practical,
- document restart requirements.

---

## Versioning

GK2+ follows Semantic Versioning:

```text
MAJOR.MINOR.PATCH
```

The repository-root `VERSION` file is the single source of truth. Do not independently hard-code release version strings elsewhere.

Version bumps are normally handled by project maintainers during release preparation.

---

## Changelog

Player-facing changes belong under:

```md
## [Unreleased]
```

Use the relevant sections:

- Added
- Changed
- Fixed
- Compatibility
- Known Issues

Do not create or alter a numbered release section unless requested by a maintainer.

---

## Before Opening a Pull Request

Please verify:

- the project builds successfully,
- the game launches with GK2+ installed,
- no new unexpected errors appear in `BepInEx/LogOutput.log`,
- existing GK2+ behavior still loads,
- your feature can be disabled if applicable,
- the change is documented,
- `CHANGELOG.md` is updated when appropriate,
- local-only files are not included.

Do not commit:

```text
local.props
bin/
obj/
dist/
*.bak
BepInEx/
game DLLs
game assets
decompiled game source
private recon output
```

---

## Pull Requests

Pull requests should include:

- a clear title,
- a summary of what changed,
- why the change is useful,
- which game systems are affected,
- how the change was tested,
- whether it may conflict with other mods,
- screenshots for visible UI changes when applicable.

Large features should preferably be discussed in an issue before implementation. Submission does not guarantee acceptance, and maintainers may request revisions before merge.

---

## Third-Party Code and Assets

Only contribute code or assets you have the right to contribute.

If work is based on another open-source project:

- verify that its license permits reuse,
- preserve required notices,
- provide required attribution,
- identify the source in the pull request.

Do not copy another mod's implementation simply because it is publicly downloadable.

---

## Reporting Bugs and Requesting Features

Use GitHub Issues for bugs and feature requests.

Bug reports should include:

- GK2+ version,
- Graveyard Keeper 2 version,
- BepInEx version,
- other installed mods,
- steps to reproduce,
- relevant log output.

For feature requests, describe the player problem or quality-of-life issue you want solved, not only a proposed implementation.

---

## License

By contributing to GK2+, you agree that your contribution may be distributed under the project's existing license.

See `LICENSE` for the current terms.
