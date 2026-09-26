# Contributing to GK2+

Thanks for your interest in contributing to **GK2+ (Graveyard Keeper Plus)**.

GK2+ is a modular, configurable quality-of-life and gameplay enhancement suite for **Graveyard Keeper 2**. Community pull requests are welcome, but changes are reviewed before merge to protect stability, compatibility, maintainability, licensing, and the project's modular design.

Project ownership, maintainer responsibilities, release authority, and contributor boundaries are documented in [docs/GOVERNANCE.md](docs/GOVERNANCE.md).

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
- treat CPU, GPU, RAM, disk I/O, and cleanup behavior as part of correctness,
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
9. Add or update the feature's section in [docs/FEATURES.md](docs/FEATURES.md).
10. State whether settings are main-menu-only, live-safe, or restart-required.

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

## Performance and Resource Requirements

All contributions must follow [docs/PERFORMANCE.md](docs/PERFORMANCE.md).

In particular:

- do not add heavy work to per-frame Update loops,
- prefer game events over polling,
- cache expensive Unity/reflection lookups,
- pair every event subscription with an unsubscribe,
- avoid retaining stale scene/game objects,
- avoid repeated LINQ/reflection allocations in hot paths,
- do not create continuous production log/recon writes,
- keep hidden UI idle,
- clean up owned GameObjects/components/resources on shutdown,
- document any periodic background work and why it is necessary.

A feature that works functionally but leaks objects, grows memory indefinitely, or performs avoidable continuous I/O is not release-ready.

---

## Save Safety and Persistent Mutations

Features that alter persistent player, economy, progression, quest, or world state must use the shared save-safety layer rather than implementing their own backup behavior.

See [docs/SAVE_SAFETY.md](docs/SAVE_SAFETY.md).

General rules:

- Low-risk/session-only actions should not create unnecessary disk writes.
- Moderate/High-risk persistent actions should pass through GK2SaveService.
- Repeated risky actions should reuse the current safety checkpoint instead of copying the same save repeatedly.
- Normal launch/load/save activity must not create GK2+ safety backups.
- Backup retention must remain bounded.
- Do not automatically restore/overwrite a live save unless a restore workflow has been explicitly designed and tested.

### Cheat Integrity

Player-facing actions intentionally classified as **cheats** must also preserve GK2+'s achievement-integrity policy:

- route first use through the shared cheat-taint/confirmation flow;
- never execute the first cheat if the active save could not be marked tainted;
- do not bypass the achievement platform guard;
- keep the taint marker outside GK2's serialized save schema;
- propagate taint to GK2+ safety backups for the same slot;
- fail closed if achievement protection cannot initialize;
- ordinary QoL features must not taint a save merely because they are implemented by GK2+.

A new Cheats-tab action should use the shared registration/gating path rather than implementing its own confirmation or taint logic.

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

The current menu controller is persistent and hosted under the game's persistent GUI root. Avoid coupling feature UI to scene-specific/main-menu-only hierarchies, and avoid creating duplicate persistent UI roots.

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

Version bumps are maintainer-owned and handled during release preparation. Contributors should not change `VERSION` or `GAME_VERSION` unless the maintainer explicitly asks for it.

---

## Changelog and Release Metadata

`CHANGELOG.md`, version numbers, release notes, and official distribution metadata are **maintainer-owned**.

Contributors should **not** edit the following unless the maintainer explicitly requests it:

- `CHANGELOG.md`;
- `VERSION`;
- `GAME_VERSION`;
- `docs/RELEASE_NOTES_*.md`;
- official Nexus/Thunderstore/GitHub release metadata.

Instead, clearly describe the player-facing change and actual validation in the pull request, and keep [docs/FEATURES.md](docs/FEATURES.md) current for the feature itself.

The maintainer will update the changelog and release metadata during final PR approval/release preparation.

---

## Before Opening a Pull Request

Please verify:

- the project builds successfully,
- the game launches with GK2+ installed,
- no new unexpected errors appear in `BepInEx/LogOutput.log`,
- existing GK2+ behavior still loads,
- your feature can be disabled if applicable,
- player-facing behavior is documented in `docs/FEATURES.md` when applicable,
- feature-specific documentation is current,
- performance/resource impact has been considered,
- persistent mutations use the save-safety service,
- no new unexpected backup/log churn occurs during ordinary play,
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

All official merges are approved by the project maintainer. Community feature ideas and implementations are welcome, but an open PR is not approval to merge or release.

Pull requests should include:

- a clear title,
- a summary of what changed,
- why the change is useful,
- which game systems are affected,
- how the change was tested,
- whether it may conflict with other mods,
- screenshots for visible UI changes when applicable;
- concrete build/runtime validation that was actually performed;
- an updated `docs/FEATURES.md` entry for player-facing behavior;
- confirmation that the branch is current with `main` and contains only intended changes.

The maintainer reviews feature PRs in this order:

~~~text
Does it work?
    ↓
Is the feature documentation accurate/current?
    ↓
Maintainer updates changelog/release metadata
    ↓
Maintainer approves and merges
~~~

Large features should preferably be discussed in an issue before implementation. Submission does not guarantee acceptance, and the maintainer may request revisions before merge.

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
