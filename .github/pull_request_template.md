## Summary

<!-- What changed and why? Keep this scoped to the feature/fix in this PR. -->

## Game Systems Affected

<!-- Example: inventory/chest UI, farming placement, save system, GK2+ UI. -->

## Runtime / Build Validation

Check only what was actually tested.

- [ ] Project builds successfully.
- [ ] Graveyard Keeper 2 launches with this branch DLL.
- [ ] No new unexpected BepInEx errors were observed.
- [ ] Primary feature behavior was tested in-game.
- [ ] Relevant edge cases were tested.
- [ ] Save/reload behavior was tested where persistence is involved.
- [ ] Enable/disable behavior was tested where applicable.
- [ ] Main-menu controls and in-game status were tested where applicable.
- [ ] Parent/child setting order and disabled-state behavior were tested where applicable.
- [ ] Direct BepInEx/r2modman config values map to the same settings used by the in-game UI.
- [ ] Compatibility/overlap behavior was tested where applicable.

### What I actually tested

<!-- Describe exact runtime steps/results. Do not write "should work." -->

## Feature Documentation

- [ ] `docs/FEATURES.md` was added/updated for player-facing behavior.
- [ ] Feature-specific docs were updated where applicable.
- [ ] README navigation/high-level copy was checked if the project structure changed.
- [ ] Screenshots are included for visible UI changes when useful.

### Documentation notes

<!-- Mention the feature-doc files changed. -->

## Compatibility / Mod Overlap

<!-- What happens if another mod provides the same behavior? Can this GK2+ feature be disabled independently? -->

## Scope / Mainline Readiness

- [ ] This branch is current with `main`.
- [ ] The PR contains only the intended feature/fix and required supporting changes.
- [ ] No private recon output, game DLLs/assets, local paths, saves, `bin/`, `obj/`, `dist/`, or `local.props` are included.
- [ ] Performance/resource impact was considered.
- [ ] New Harmony patches are as narrow as practical.
- [ ] Experimental behavior is clearly labeled in both UI and documentation.
- [ ] Feature/settings ordering is deterministic and does not depend on registration order.

## Maintainer-owned release metadata

Unless specifically requested by the maintainer, this PR should **not** edit:

- `CHANGELOG.md`
- `VERSION`
- `GAME_VERSION`
- `docs/RELEASE_NOTES_*.md`
- official Nexus/Thunderstore/GitHub release metadata

The maintainer will update those surfaces during final PR approval/release preparation.

## Maintainer Merge Gate

1. **Does it work?**
2. **Is feature documentation accurate/current?**
3. **Maintainer updates required changelog/release metadata.**
4. **Maintainer approves and merges.**

AI-assisted and human-written contributions use the same validation requirements.
