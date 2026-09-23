# Contributing to GK2+

Thanks for your interest in contributing to **GK2+ (Graveyard Keeper Plus)**.

GK2+ is intended to be a modular, configurable, all-in-one quality-of-life and gameplay enhancement suite for **Graveyard Keeper 2**.

Community contributions are welcome through pull requests, but all changes are reviewed before merge to protect compatibility, stability, maintainability, and the modular design of the project.

---

## Core Contribution Principles

Contributions should:

- Keep features modular whenever practical.
- Avoid unnecessary changes to unrelated game systems.
- Preserve compatibility with existing GK2+ modules.
- Prefer configurable behavior over hard-coded behavior.
- Allow major gameplay features to be disabled independently.
- Avoid unnecessary conflicts with other mods.
- Keep player-facing behavior documented.
- Keep code readable and maintainable.
- Be tested before submission.

GK2+ should remain a mod suite where players can choose which features they want rather than being forced into a single playstyle.

---

## Development Environment

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

New gameplay features should normally live under the appropriate `Features` category.

Shared infrastructure belongs under `Core`.

UI code should remain under `UI`.

Harmony patches should be scoped as narrowly as possible.

---

## Feature Requirements

Major gameplay features should normally:

1. Have a clear feature ID.
2. Have a player-readable name and description.
3. Register through the GK2+ feature system.
4. Expose an enable/disable setting when technically practical.
5. Avoid patching unrelated methods.
6. Document known compatibility concerns.
7. Fail safely when possible.

Contributors should never assume that every GK2+ feature is enabled.

Features should be written with coexistence in mind.

---

## Mod Compatibility

GK2+ is designed to coexist with other Graveyard Keeper 2 mods whenever practical.

If your contribution overlaps with another known mod:

- Document the overlap.
- Avoid unnecessary patch conflicts.
- Provide a way for users to disable the GK2+ implementation when practical.
- Note known incompatibilities in the pull request.

Do not intentionally block, disable, or interfere with another mod unless required to prevent a confirmed technical failure.

Compatibility warnings are preferred over silent conflicts.

---

## Harmony Patching Guidelines

When using Harmony:

- Patch the narrowest method necessary.
- Avoid broad patches when a smaller patch will work.
- Avoid modifying unrelated behavior.
- Avoid unnecessary transpilers when prefixes or postfixes are sufficient.
- Document why the patch is required.
- Consider compatibility with other Harmony patches on the same method.

If a patch belongs to a toggleable feature, the patch behavior should respect that setting.

---

## Configuration

Player-facing settings should:

- Use clear names.
- Include useful descriptions.
- Have sensible defaults.
- Be exposed through the GK2+ UI when practical.

Settings that require a restart should be clearly documented.

---

## UI Contributions

GK2+ plans to provide its own in-game configuration interface.

UI contributions should:

- Follow the visual direction of GK2+.
- Remain usable at common resolutions.
- Avoid blocking normal game controls.
- Clearly identify settings that require restart.
- Group settings by feature or category.

Temporary compatibility with BepInEx ConfigurationManager is acceptable during development.

---

## Versioning

GK2+ follows Semantic Versioning:

```text
MAJOR.MINOR.PATCH
```

The repository root `VERSION` file is the single source of truth for the current version.

Do not independently hard-code or change version strings inside source files.

Version bumps are normally handled by project maintainers during release preparation.

---

## Changelog

All notable player-facing changes should be added under:

```md
## [Unreleased]
```

in `CHANGELOG.md`.

Use the appropriate section:

- Added
- Changed
- Fixed
- Compatibility
- Known Issues

Do not create a numbered release section unless requested by a maintainer.

---

## Before Opening a Pull Request

Please make sure:

- The project builds successfully.
- The game launches with GK2+ installed.
- No new errors appear in `BepInEx/LogOutput.log`.
- Existing GK2+ functionality still loads.
- Your feature can be disabled if applicable.
- Your change is documented.
- `CHANGELOG.md` is updated when appropriate.
- No local machine paths or build artifacts are committed.

Do not commit:

```text
local.props
bin/
obj/
BepInEx/
game files
decompiled game source
```

---

## Pull Requests

Pull requests should include:

- A clear title.
- A summary of what changed.
- Why the change is useful.
- Which game systems are affected.
- How the change was tested.
- Whether it may conflict with other mods.
- Screenshots for UI changes when applicable.

Large features should preferably be discussed in an issue before implementation.

Pull requests may be requested to change before merge.

Submission does not guarantee acceptance.

---

## Game Files and Decompiled Code

Do not commit proprietary Graveyard Keeper 2 game files to this repository.

This includes, but is not limited to:

- `Assembly-CSharp.dll`
- Unity game assemblies
- game assets
- extracted textures
- decompiled source files copied directly from the game

Code may reference game types and methods as required for mod development, but proprietary game content should not be redistributed through this repository.

---

## Third-Party Code and Assets

Only contribute code or assets that you have the right to contribute.

If code is based on another open-source project:

- Verify that its license permits reuse.
- Preserve required notices.
- Provide attribution.
- Identify the source in the pull request.

Do not copy another mod's implementation simply because it is publicly downloadable.

---

## Code Review

All pull requests are reviewed before merge.

Review may include:

- Build verification.
- In-game testing.
- Compatibility testing.
- Architecture review.
- Code quality review.
- License and attribution review.

Changes that risk breaking the modular design of GK2+ may be rejected or requested to be redesigned.

---

## Reporting Bugs and Requesting Features

Bug reports and feature requests should be submitted through GitHub Issues.

When reporting a bug, include:

- GK2+ version.
- Graveyard Keeper 2 version.
- BepInEx version.
- Other installed mods.
- Steps to reproduce.
- Relevant log output.

For feature requests, describe the player problem or quality-of-life issue you want solved, not only the proposed implementation.

---

## License

By contributing to GK2+, you agree that your contribution may be distributed under the project's existing license.

See `LICENSE` for the current license terms.

---

Thank you for helping improve GK2+.
