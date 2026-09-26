# GK2+ Project Governance

GK2+ is open to community ideas, features, fixes, documentation improvements, and pull requests.

The project uses a **maintainer-led integration and release model** so one person remains responsible for deciding what enters the official build and for keeping release metadata consistent.

## Maintainer

The project maintainer is **@duhhbzz**.

The maintainer is responsible for:

- approving or declining pull requests;
- deciding when a feature is ready to merge;
- resolving scope and compatibility questions;
- updating `CHANGELOG.md`;
- choosing version numbers and updating `VERSION`;
- updating the tested game version in `GAME_VERSION`;
- preparing release notes;
- preparing official GitHub/Nexus/Thunderstore release metadata;
- producing and publishing official builds/releases;
- deciding when experimental work becomes generally supported.

Contributors should not interpret an accepted idea, open PR, or successful local test as approval to merge or release it.

## Contributions are welcome

Contributors may propose essentially any feature or improvement that fits the project and license.

A normal feature contribution should include:

- implementation;
- relevant tests/runtime validation;
- compatibility notes;
- an update to the canonical [Feature Catalog](FEATURES.md) describing the player-facing behavior;
- screenshots when the change affects visible UI.

The maintainer may request changes, additional testing, scope reductions, compatibility controls, or documentation before approval.

## Maintainer-owned release files

Unless the maintainer explicitly asks otherwise, contributors should **not** modify:

- `CHANGELOG.md`;
- `VERSION`;
- `GAME_VERSION`;
- `docs/RELEASE_NOTES_*.md`;
- release publication/version metadata;
- marketplace release copy used for an official release;
- release automation solely to publish a contributor's feature.

This keeps version history and public release messaging under one owner.

A contributor should instead explain the player-facing change clearly in the PR. The maintainer will make the final changelog/release-metadata update before merge or release.

## Merge approval gates

Every player-facing feature PR is reviewed in this order:

~~~text
1. Does it work?
      ↓
2. Is the feature documentation accurate?
      ↓
3. Has the maintainer updated required changelog/release metadata?
      ↓
4. Maintainer approves and merges
~~~

A feature can be functionally correct and still remain unmerged until documentation, compatibility, scope, or release bookkeeping is ready.

See [CONTRIBUTING.md](../CONTRIBUTING.md) for implementation expectations and [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md) for release gates.

## AI-assisted work

AI-assisted contributions are welcome.

They are held to exactly the same requirements as human-written work. The contributor remains responsible for verifying the code, testing the actual game behavior, describing what was really tested, and ensuring the PR does not contain invented APIs, fabricated validation, copied incompatible code, or unrelated changes.

## Official builds

Only releases published through the maintainer-controlled GK2+ release process are official GK2+ builds.

Forks and local/community builds are welcome under the project license, but they should not be represented as an official GK2+ release.
