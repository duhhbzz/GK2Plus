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
| Inventory | [Shared Chests](#shared-chests) |
| Cheats | [Functional Cheats](#functional-cheats) |
| Cheats / Safety | [Cheat & Achievement Integrity](#cheat--achievement-integrity) |
| Safety | [Save Safety Checkpoints](#save-safety-checkpoints) |

---

## GK2+ Mod Menu

Press **F2** to open or close GK2+. **Esc** closes it.

The menu persists between the main menu and gameplay and organizes features into categories such as Inventory, Farming, Cheats, and More.

Where practical, features are independently configurable so players can disable an overlapping GK2+ feature without uninstalling the entire suite.

Settings are ordered intentionally within each tab. Child options stay visually attached beneath their parent feature, and parent-disabled options remain visible but greyed/non-interactive so their saved values are still understandable.

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

## Shared Chests

**Category:** Inventory  
**Setting mode:** Main-menu enable/disable; read-only status during gameplay.

Shared Chests makes the eligible storage inventories that Graveyard Keeper 2 already exposes for the **current world zone** usable from both normal chest windows and the character inventory.

### How it works

Vanilla GK2 already supplies those other current-zone storage inventories to the chest-window data, but their remote slots are rendered unavailable. GK2+ enables those existing inventory widgets and keeps the game's own inventory objects, transfer callbacks, stack rules, filters, capacity behavior, and notifications in control.

GK2+ does **not** create a separate shared-storage database or custom shared-chest save format.

### Player behavior

With **Shared Chests ON**:

- open a normal chest and access other eligible storage in the current zone;
- open the character inventory and browse/transfer items from eligible current-zone storage;
- move full stacks, partial stacks, or single items through GK2's native inventory paths.

Player-only context actions such as **Use** or **Equip** still require the item to be in the player's carried inventory. Shared Chests does not currently bypass those native restrictions.

With **Shared Chests OFF**, the remote inventories return to their vanilla greyed/read-only behavior.

### Configuration

From the **main menu → Inventory** tab:

~~~text
Shared Chests    [ ON / OFF ]
~~~

During active gameplay, the same row is shown as read-only status.

The underlying enable/disable value is a normal BepInEx configuration entry, so advanced users may also manage it through the generated GK2+ `.cfg` file or a compatible mod-manager config editor. External config edits should be treated as next-launch changes.

### Validation completed

Runtime validation covered:

- current-zone remote storage discovery;
- character-inventory remote storage activation;
- remote chest selection;
- remote → opened chest transfers;
- opened chest → remote transfers;
- full-stack, partial-stack, and single-item movement;
- full destination behavior;
- repeated close/reopen cycles;
- save/reload persistence;
- main-menu ON/OFF behavior;
- read-only in-game status;
- disabling the feature and returning to vanilla remote-slot behavior;
- preserving native player-only restrictions for context actions such as Use/Equip.

### Compatibility

Shared Chests patches the chest-window data construction path narrowly and reuses GK2's native transfer behavior.

If another mod should own overlapping chest behavior, disable Shared Chests from the main menu.

### Planned Shared Storage direction

Shared Chests is intentionally narrow for the current release, but the longer-term feature family is expected to evolve toward **Shared Storage** with independent child settings instead of one all-or-nothing "god mode" switch.

Planned shape:

~~~text
Shared Storage                              [ ON ]
    Storage Scope                   [ Current Zone ▼ ]
    Character Inventory Access              [ ON ]
    Use Items From Storage                  [ OFF ]
    Craft From Storage                      [ OFF ]
~~~

The key design rule is that **scope** and **capability** remain separate:

- **Storage Scope** decides whether eligible storage is limited to the current zone or can span the world.
- **Character Inventory Access** controls whether remote storage appears in the character inventory.
- **Use Items From Storage** would allow selected player-only item actions directly from eligible storage.
- **Craft From Storage** would allow workstations to source ingredients from eligible storage while preserving the workstation's own crafting rules.

These are planned capabilities, not part of the current release, and each should be investigated/tested independently before implementation.


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
