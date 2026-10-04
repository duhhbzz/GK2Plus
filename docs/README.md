# GK2+ Documentation

This directory contains the public development, feature, configuration, governance, safety, and release documentation for **GK2+ / Graveyard Keeper Plus**.

If you are contributing manually or with an AI coding assistant, start here instead of inferring project intent from individual source files.

## Start Here

- [Feature Catalog](FEATURES.md) — canonical player-facing documentation for features that exist today.
- [Roadmap](ROADMAP.md) — high-confidence planned features and intended player-facing outcomes.
- [Contributing](../CONTRIBUTING.md) — architecture, coding, compatibility, testing, and contribution rules.
- [Governance](GOVERNANCE.md) — maintainer/contributor boundaries, merge authority, and release ownership.
- [Configuration](CONFIGURATION.md) — configuration source-of-truth and UI/config expectations.
- [UI Framework](UI-FRAMEWORK.md) — native GK2 visual primitives, menu layout contract, and rules for new feature UI.
- [Performance](PERFORMANCE.md) — CPU, GPU, RAM, GC, disk I/O, polling, lifecycle, and cleanup requirements.
- [Save Safety](SAVE_SAFETY.md) — persistent mutation, checkpoint, backup, and cheat-integrity rules.
- [Release Checklist](RELEASE_CHECKLIST.md) — feature approval and release validation gates.
- [Thunderstore](THUNDERSTORE.md) — Thunderstore / R2ModMan packaging and distribution notes.
- [Steam Workshop](STEAM_WORKSHOP.md) — SteamCMD packaging/publishing and Workshop validation.
- [Nexus Page](NEXUS_PAGE.md) — maintained player-facing Nexus copy/reference.

## Source-of-Truth Order

When documents overlap, use this order:

1. Current source code plus actual runtime validation for what GK2+ does today.
2. [FEATURES.md](FEATURES.md) for canonical player-facing behavior of implemented features.
3. [ROADMAP.md](ROADMAP.md) for planned/high-confidence feature direction.
4. [CONTRIBUTING.md](../CONTRIBUTING.md) and [GOVERNANCE.md](GOVERNANCE.md) for implementation, review, ownership, and merge rules.
5. [CONFIGURATION.md](CONFIGURATION.md), [PERFORMANCE.md](PERFORMANCE.md), and [SAVE_SAFETY.md](SAVE_SAFETY.md) for engineering constraints.
6. [CHANGELOG.md](../CHANGELOG.md) and release notes for maintainer-owned release history.
7. README files for summaries and onboarding.

The roadmap describes intended outcomes, not permission to skip reconnaissance. New gameplay work still needs current-version game-system verification before implementation.

## For AI-Assisted Contributions

AI tools should be given, at minimum:

- `AGENTS.md`
- `CONTRIBUTING.md`
- `docs/FEATURES.md`
- `docs/ROADMAP.md`
- `docs/GOVERNANCE.md`
- `docs/UI-FRAMEWORK.md` when adding or changing player-facing UI
- the relevant configuration/safety/performance document for the feature being changed
- the current source files being modified

AI-generated code is held to the same standards as human-written code. In particular:

- do not invent game APIs, method signatures, save formats, or runtime validation;
- do not publish decompiled proprietary game source or private recon output;
- prefer native GK2 APIs and narrow Harmony patches;
- preserve modular feature toggles and configuration behavior;
- route persistent mutations through the shared save-safety layer;
- update `docs/FEATURES.md` when player-facing behavior changes;
- do not edit maintainer-owned changelog/version/release metadata unless explicitly asked;
- validate in-game before calling a gameplay feature complete.

## Private Recon

Public recon tooling lives under:

~~~text
tools/recon/
~~~

Private reverse-engineering output belongs outside the repository, such as:

~~~text
D:\GK2-Recon\
~~~

Do not commit game DLLs, decompiled source, extracted assets, raw architecture dumps, private deep-dive reports, or local runtime reports containing proprietary data.
