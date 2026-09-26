# GK2+ Feature Catalog

This is the canonical player-facing feature catalog for **GK2+ (Graveyard Keeper Plus)**.

The README and mod-platform landing pages intentionally stay concise. Detailed feature behavior belongs here so public descriptions do not become an ever-growing laundry list.

> Availability is release-specific. Use the [Changelog](../CHANGELOG.md) and GitHub Releases to determine which cataloged features exist in a published version.

## Feature Index

| Category | Feature |
| --- | --- |
| General / UI | [GK2+ Mod Menu](#gk2-mod-menu) |
| General | [Manual Save](#manual-save) |
| General | [Last Save Status](#last-save-status) |
| Cheats | [Functional Cheats](#functional-cheats) |
| Cheats / Safety | [Cheat & Achievement Integrity](#cheat--achievement-integrity) |
| Safety | [Save Safety Checkpoints](#save-safety-checkpoints) |

---

## GK2+ Mod Menu

Press **F2** to open or close GK2+. **Esc** closes it.

The menu persists between the main menu and gameplay and organizes features into categories such as Inventory, Farming, Cheats, and More.

Where practical, features are independently configurable so players can disable an overlapping GK2+ feature without uninstalling the entire suite.

---

## Manual Save

Adds a native-style **Save Game** button to the in-game pause menu.

GK2+ delegates the actual save operation to Graveyard Keeper 2's own save system rather than implementing custom save serialization.

Validated behavior includes mid-day saving without forcing sleep, preserving player location and tested world state, native saving feedback, and keyboard/mouse plus controller navigation.

Manual Save is a normal QoL feature and does **not** mark a save as cheat-tainted.

---

## Last Save Status

Extends the native **Exit to Main Menu** confirmation with the active slot's real last-save age/time.

Example:

~~~text
Last saved: 10 minutes ago (4:26 AM).
~~~

The value comes from native save metadata and updates after a successful save.

---

## Functional Cheats

The Cheats tab currently provides money and player-recovery actions.

Released actions include Silver/Gold increments, **Heal Player**, and **Refill Energy**.

Money changes use GK2's native resource path and normal feedback. Refill Energy targets the normal work/action energy resource.

Cheat actions use the integrity and save-safety systems described below.

---

## Cheat & Achievement Integrity

The first cheat used on a save displays a confirmation explaining that platform achievements will be disabled for that save and its GK2+ backup lineage.

If confirmed:

- the active save receives a persistent GK2+ cheat-taint sidecar;
- retained GK2+ backups for that slot are marked tainted;
- future backups inherit the marker;
- future cheat actions on that save do not ask again;
- GK2+ blocks the game's platform achievement progress/unlock boundary while that tainted save is active.

The marker is stored outside Graveyard Keeper 2's serialized save schema.

Ordinary QoL features do not taint a save merely because GK2+ provides them.

---

## Save Safety Checkpoints

Protected Moderate/High-risk persistent mutations use an on-demand checkpoint model.

~~~text
Load/save generation begins
→ no automatic GK2+ backup

First protected mutation
→ create one pre-mutation checkpoint

More protected mutations
→ reuse that checkpoint

Native save/load
→ invalidate checkpoint

Next protected mutation
→ create a new checkpoint
~~~

GK2+ retains up to **5 backup directories per save slot**.

See [SAVE_SAFETY.md](SAVE_SAFETY.md) for the detailed safety architecture.

---

## Adding or Changing a Feature

A feature PR should update this catalog when player-facing behavior is added, removed, renamed, or materially changed.

The entry should explain:

- what the feature does;
- how the player uses it;
- where settings live;
- whether settings are main-menu-only, live-safe, or restart-required;
- save/persistence behavior when relevant;
- compatibility/overlap concerns;
- validation actually completed.

Contributors do **not** need to update the changelog/version/release metadata unless the maintainer explicitly asks them to. See [GOVERNANCE.md](GOVERNANCE.md) and [CONTRIBUTING.md](../CONTRIBUTING.md).
