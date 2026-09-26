# GK2+ Documentation

This directory contains the public development and release documentation for **GK2+ / Graveyard Keeper Plus**.

If you are contributing manually or with an AI coding assistant, start with the documents below rather than guessing project intent from individual source files.

## Start Here

- [Roadmap](ROADMAP.md) — high-confidence planned features and current development direction.
- [Contributing](../CONTRIBUTING.md) — architecture, coding, compatibility, testing, and contribution rules.
- [Performance](PERFORMANCE.md) — CPU, GPU, RAM, GC, disk I/O, polling, lifecycle, and cleanup requirements.
- [Save Safety](SAVE_SAFETY.md) — rules for persistent mutations, checkpoints, backups, and cheat-taint behavior.
- [Release Checklist](RELEASE_CHECKLIST.md) — validation gates before shipping a build.
- [Thunderstore](THUNDERSTORE.md) — packaging/distribution notes for Thunderstore and R2ModMan.
- [Nexus Page](NEXUS_PAGE.md) — maintained player-facing Nexus copy/reference.

## Source-of-Truth Order

When documents overlap, use this order:

1. Current source code and tests/runtime validation for what GK2+ actually does today.
2. [ROADMAP.md](ROADMAP.md) for planned/high-confidence feature direction.
3. [CONTRIBUTING.md](../CONTRIBUTING.md) for implementation and contribution rules.
4. [PERFORMANCE.md](PERFORMANCE.md) and [SAVE_SAFETY.md](SAVE_SAFETY.md) for non-negotiable engineering constraints.
5. [CHANGELOG.md](../CHANGELOG.md) for player-facing release history.
6. README files for summaries and onboarding.

The roadmap describes intended outcomes, not permission to skip reconnaissance. New gameplay work should still verify the exact Graveyard Keeper 2 systems and methods before implementation.

## For AI-Assisted Contributions

AI tools should be given, at minimum:

- `AGENTS.md`
- `CONTRIBUTING.md`
- `docs/ROADMAP.md`
- the relevant architecture/safety document for the feature being changed
- the current source files being modified

AI-generated code must follow the same requirements as human-written code. In particular:

- do not invent game APIs or assume method signatures;
- do not publish decompiled proprietary game source or private recon output;
- prefer native GK2 APIs and narrow Harmony patches;
- preserve modular feature toggles;
- route persistent mutations through the shared save-safety layer;
- update player-facing documentation/changelog when behavior changes;
- validate in-game before calling a feature complete.

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
