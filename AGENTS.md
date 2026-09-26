# AGENTS.md — GK2+ Contributor / AI Assistant Guide

This file is intentionally placed at the repository root so coding assistants and contributors can quickly discover the project's rules and source-of-truth documents.

## Project

**GK2+ / Graveyard Keeper Plus** is a modular, configurable quality-of-life and gameplay enhancement suite for Graveyard Keeper 2.

Core product principle:

> **GK2+ — one mod, your way.**

Major features should be independently configurable where practical, compatibility-friendly, and narrowly integrated with the game.

## Read Before Making Changes

Use these documents as the starting context:

1. [README.md](README.md) — current public project overview.
2. [docs/ROADMAP.md](docs/ROADMAP.md) — high-confidence planned features and development direction.
3. [CONTRIBUTING.md](CONTRIBUTING.md) — architecture and contribution rules.
4. [docs/PERFORMANCE.md](docs/PERFORMANCE.md) — performance/resource requirements.
5. [docs/SAVE_SAFETY.md](docs/SAVE_SAFETY.md) — persistent mutation and backup rules.
6. [docs/RELEASE_CHECKLIST.md](docs/RELEASE_CHECKLIST.md) — validation/release gates.
7. [docs/README.md](docs/README.md) — documentation index and source-of-truth guidance.

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
- avoid assuming unrelated GK2+ features are enabled.

If a feature overlaps another mod, prefer warning + player choice over silently disabling the other mod.

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

- update `CHANGELOG.md` under **Unreleased**;
- update `docs/ROADMAP.md` when feature status/scope changes;
- update README/docs when onboarding or configuration changes;
- keep release/checklist documentation aligned with newly shipped features.

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
