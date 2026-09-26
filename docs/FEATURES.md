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
| Inventory | [Bigger Item Stacks](#bigger-item-stacks) |
| Inventory | [Shared Chests](#shared-chests) |
| Cheats | [Functional Cheats](#functional-cheats) |
| Cheats | [Spawn Item](#spawn-item) |
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

## Bigger Item Stacks

**Category:** Inventory  
**Setting mode:** Main-menu enable/disable + multiplier selection; read-only status during gameplay.

Bigger Item Stacks scales GK2's live native stack limits for stackable items while leaving items with a native stack limit of 1 unchanged.

### Configuration

From the **main menu → Inventory** tab:

~~~text
Bigger Item Stacks                 [ ON ]
    Stack Size Multiplier           [ 3x ]
~~~

The multiplier picker exposes **2x through 20x** for normal menu use. The underlying BepInEx value remains the single source of truth.

When the parent feature is OFF, the multiplier remains visible but greyed/non-interactive and keeps its saved value.

### Native behavior and compatibility

GK2+ modifies the live `ItemDef.stackCount` value after game balance loads and leaves inventory transfer/merge behavior to GK2.

The feature records the live value it scaled. When disabling or changing the multiplier, GK2+ only restores values that still match the value GK2+ applied. If another mod changed a stack limit afterward, GK2+ preserves that newer live value instead of overwriting it.

### Save behavior

No custom stack data is written to the save.

Testing confirmed that an already-saved over-cap stack remains present after Bigger Item Stacks is disabled. GK2 then normalizes/splits that stack through its normal inventory behavior when the stack is moved or otherwise adjusted.

### Validation completed

Runtime validation covered:

- 2x → 3x multiplier changes;
- a native 50-stack item reaching 150 at 3x;
- main-menu enable/disable behavior;
- parent/child menu grouping and disabled-child presentation;
- saving with an over-cap stack;
- disabling the feature before reload;
- loading the existing over-cap stack without loss;
- GK2 lazily splitting/normalizing that stack when it is moved.

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
Shared Chests                              [ ON / OFF ]
    Character Inventory Access              [ ON / OFF ]
~~~

**Character Inventory Access** is a child setting and defaults to ON. Turning it OFF keeps normal chest-window Shared Chests behavior while restoring the character inventory's remote storage to vanilla read-only behavior.

During active gameplay, feature settings are shown as read-only status.

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

Current/future shape:

~~~text
Shared Storage                              [ ON ]
    Storage Scope                   [ Current Zone ▼ ]  (future)
    Character Inventory Access              [ ON ]      (implemented)
    Use Items From Storage                  [ OFF ]     (future)
    Craft From Storage                      [ OFF ]     (future)
~~~

The key design rule is that **scope** and **capability** remain separate:

- **Character Inventory Access** is already implemented and independently configurable.
- **Storage Scope** would decide whether eligible storage is limited to the current zone or can span the world.
- **Use Items From Storage** would allow selected player-only item actions directly from eligible storage.
- **Craft From Storage** would allow workstations to source ingredients from eligible storage while preserving the workstation's own crafting rules.

Future-only controls are intentionally not rendered in the live menu until their behavior exists and has been tested.


---

## Functional Cheats

The Cheats tab currently provides money and player-recovery actions.

Released actions include Silver/Gold increments, **Heal Player**, **Refill Energy**, and **Spawn Item**.

Money changes use GK2's native resource path and normal feedback. Refill Energy targets the normal work/action energy resource.

Cheat actions use the integrity and save-safety systems described below.

---

## Spawn Item

**Category:** Cheats  
**Setting mode:** Item/quantity selection in the GK2+ Cheats menu; cheat execution requires a loaded save.

Spawn Item creates a selected native GK2 item and adds it to the player's inventory through GK2's normal item materialization and inventory-add paths.

### Player behavior

The Cheats tab provides:

- a searchable/paged native item picker;
- a quantity field;
- a Spawn action.

The selected item id and quantity are normal BepInEx configuration values. Quantity is limited to **1–10,000**.

### Safety

Spawn Item is a cheat action. It uses the same first-cheat confirmation, save checkpoint/taint flow, and achievement-integrity protection as the other GK2+ cheats.

GK2+ asks GK2 to materialize the requested item through `ItemCount.CreateItems()` and add it through the player's native inventory API rather than constructing a separate inventory representation.

### Validation completed

Runtime validation confirmed item selection, quantity control, native item creation, and insertion into the player's inventory.

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
