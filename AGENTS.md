# AGENTS.md — GK2+ Contributor / AI Assistant Guide

This file lives at the repository root so coding assistants and contributors can quickly discover the project's source-of-truth documentation and engineering rules.

## Project

**GK2+ / Graveyard Keeper Plus** is a modular, configurable quality-of-life and gameplay enhancement suite for Graveyard Keeper 2.

Core product principle:

> **GK2+ — one mod, your way.**

Major features should be independently configurable where practical, compatibility-friendly, and narrowly integrated with the game.

## Read Before Making Changes

Start with:

1. [README.md](README.md) — public project overview.
2. [docs/FEATURES.md](docs/FEATURES.md) — canonical player-facing behavior for implemented features.
3. [docs/ROADMAP.md](docs/ROADMAP.md) — high-confidence planned features and intended outcomes.
4. [CONTRIBUTING.md](CONTRIBUTING.md) — architecture and contribution rules.
5. [docs/GOVERNANCE.md](docs/GOVERNANCE.md) — maintainer/contributor ownership and merge/release authority.
6. [docs/CONFIGURATION.md](docs/CONFIGURATION.md) — configuration source-of-truth and UI/config rules.
7. [docs/PERFORMANCE.md](docs/PERFORMANCE.md) — performance/resource requirements.
8. [docs/SAVE_SAFETY.md](docs/SAVE_SAFETY.md) — persistent mutation and backup rules.
9. [docs/RELEASE_CHECKLIST.md](docs/RELEASE_CHECKLIST.md) — feature approval and release gates.
10. [docs/UI-FRAMEWORK.md](docs/UI-FRAMEWORK.md) — native UI primitives and the required F2 feature-page visual contract.
11. [docs/README.md](docs/README.md) — documentation index and source-of-truth guidance.

Do not treat the roadmap as an implementation specification. It records intended outcomes. The exact game integration must still be confirmed against the current Graveyard Keeper 2 build.

## Architecture

Prefer this separation:

~~~text
Framework = how GK2+ talks to the game
Feature   = behavior GK2+ provides
Patch     = smallest unavoidable interception
~~~

Player-facing gameplay/QoL code normally belongs under:

~~~text
src/GK2Plus/Features/
~~~

Shared game-internal access should live behind:

~~~text
src/GK2Plus/Framework/
~~~

Avoid placing feature logic directly in the persistent menu controller.

## Game Recon / API Rules

Before changing gameplay behavior:

- identify the exact game type/method involved;
- verify signatures against the current game build;
- reuse native game APIs and data paths where practical;
- prefer the narrowest Harmony patch that solves the problem;
- avoid guessing private field/method names;
- revalidate affected systems after significant game updates.

Public recon tooling is allowed under `tools/recon/`.

Do **not** commit or publish:

- game DLLs;
- decompiled proprietary game source;
- extracted proprietary assets;
- raw private architecture dumps;
- private deep-dive/runtime recon reports.

## Feature Rules

Major features should normally:

- have a stable feature ID;
- have a readable name/description;
- register through the GK2+ feature system;
- expose an enable/disable setting where practical;
- fail safely when expected game targets are unavailable;
- document compatibility concerns;
- avoid assuming unrelated GK2+ features are enabled;
- update `docs/FEATURES.md` when player-facing behavior changes.

If a feature overlaps another mod, prefer warning + player choice over silently disabling the other mod.

## Configuration

BepInEx `ConfigEntry` values are the normal settings source of truth.

Do not create an independent settings database just for the in-game UI unless there is a documented technical reason.

Follow [docs/CONFIGURATION.md](docs/CONFIGURATION.md), including deterministic menu order, parent/child grouping, and main-menu-only/live-safe/restart-required behavior.

## F2 Menu UI Rules

When a feature appears in the GK2+ F2 menu, follow
[docs/UI-FRAMEWORK.md](docs/UI-FRAMEWORK.md#feature-page-visual-contract).

In particular:

- use the existing native-style page shell and section headers;
- do not create a section for every button/control;
- a parent ON/OFF feature owns its child options;
- parent + children share one expandable dark native panel;
- clicking the parent label expands/collapses; clicking ON/OFF changes state;
- collapse state is presentation-only;
- preserve child configuration when a parent feature is disabled;
- keep descriptions short and inside the parent row;
- use native red controls for concise actions/values;
- use `GK2UiTheme`, `GK2UiMetrics`, factories/builders, and
  `GK2UIService` instead of one-off visual code.

## Persistent State / Cheats

Persistent player, economy, progression, quest, or world mutations must use the shared save-safety architecture.

Cheat-classified actions must preserve the existing cheat-taint and achievement-integrity flow.

Do not create a second backup/taint system inside an individual feature.

## Performance

Avoid:

- heavy per-frame work;
- repeated `Resources.FindObjectsOfTypeAll` in hot paths;
- repeated reflection/LINQ allocations in hot paths;
- continuous disk writes;
- unbounded logs/backups/collections;
- leaked subscriptions or persistent Unity objects.

Prefer events, cached lookups, bounded retention, and explicit lifecycle cleanup.

## Documentation Expectations

When a change affects player-facing behavior:

- update `docs/FEATURES.md`;
- update `docs/ROADMAP.md` when roadmap status/scope changes;
- update README/docs when onboarding or configuration changes;
- keep feature-specific documentation accurate.

Do **not** edit maintainer-owned `CHANGELOG.md`, `VERSION`, `GAME_VERSION`, release notes, or official release metadata unless the maintainer explicitly asks. See [docs/GOVERNANCE.md](docs/GOVERNANCE.md).

## Validation

A successful compile is not sufficient for gameplay features.

Before considering work complete, validate as applicable:

- game launch and BepInEx load;
- relevant UI behavior;
- enable/disable behavior;
- save/load persistence;
- no unexpected log errors;
- no duplicated persistent UI/objects;
- compatibility with the current GK2 build;
- performance/resource behavior;
- safe failure when expected game APIs are unavailable.

Never claim runtime validation that was not actually performed.

## Local / Ignored Files

Never commit:

~~~text
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
~~~
