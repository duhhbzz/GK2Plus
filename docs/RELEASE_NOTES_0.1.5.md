# GK2+ v0.1.5 — Shared Storage & Inventory Update

GK2+ v0.1.5 is the first major feature expansion after the initial gameplay release.

**Tested with Graveyard Keeper 2 v1.006.**

## Highlights

### Shared Storage

Shared Chests has grown into **Shared Storage**, with independent controls for scope and capability:

~~~text
Shared Storage                              [ ON ]
    Storage Scope                   [ Current Zone ▼ ]
    Character Inventory Access              [ ON ]
    Use Items From Storage                  [ OFF ]
    Craft From Selected Scope               [ OFF ]
~~~

- **Current Zone** keeps storage access limited to GK2's current world zone.
- **Global** exposes eligible persisted storage from other zones.
- Character Inventory Access can be enabled/disabled separately.
- Supported consumables can be used directly from eligible remote storage.
- Global crafting can pull materials from eligible storage in other zones.
- Blueprint/building material checks and actual consumption can use the selected Global scope.
- Normal chest windows and the character inventory both support Global storage browsing and transfers.
- GK2+ continues to use the game's native inventory objects, stack rules, filters, capacity behavior, transfer paths, crafting checks, and resource consumption.

### Bigger Item Stacks

- Adds a configurable **Stack Size Multiplier**.
- Main-menu picker supports **2x through 20x**.
- Items with a native stack limit of 1 remain unchanged.
- Live values are restored/reapplied conservatively so later third-party stack changes are not blindly overwritten.

### Spawn Item

The Cheats tab now includes a searchable, paged item picker with configurable quantity.

- Uses GK2's native item materialization and player-inventory insertion paths.
- Uses GK2+'s existing cheat confirmation, save checkpoint, cheat-taint, and achievement-protection flow.

### Menu / UX Improvements

- Every tab body is now scrollable when its content exceeds the available space.
- Feature child rows stay visible in gameplay as read-only status.
- Binary child options such as ON/OFF toggle immediately when clicked.
- Multi-choice settings such as Storage Scope still use the normal selector.
- Feature families use deterministic parent/child ordering and spacing.
- The Nexus Mods button is enabled and points to the live GK2+ page.

## Validation

Runtime validation for this release included:

- Shared Storage Current Zone and Global discovery;
- Global storage in character inventory and normal chest windows;
- cross-zone chest-to-chest and player/storage transfers;
- full, partial, and single-stack movement;
- full-destination handling;
- Character Inventory Access ON/OFF;
- direct remote consumable Use;
- Global crafting using materials stored only in another zone;
- Global blueprint/building resource checks and actual remote material consumption;
- disabling Craft From Selected Scope and confirming crafting/building returns to zone-bound behavior;
- switching Global back to Current Zone;
- save/reload persistence for transferred storage items;
- Bigger Item Stacks multiplier behavior and over-cap save/reload handling;
- Spawn Item quantity/item selection and native inventory insertion;
- main-menu configuration and read-only in-game status;
- inline binary toggles and multi-choice picker behavior.

## Known Limitations

- **Use Items From Storage** currently supports consumable Use behavior only. Equip, Plant, Fertilize, Destroy, and hotbar actions remain player-inventory only.
- **Spawn Item** is best used with normal inventory items in v0.1.5. Big carryables and world-linked definitions are not yet routed through GK2's native physical drop/carry path.
- Configurable GK2+ menu hotkey and Continuous Planting remain in development and are **not** included in v0.1.5.
- Automated save-backup restore remains intentionally out of scope.

## Installation

Requires **BepInEx 5.4.23.5**.

Extract `GK2Plus-0.1.5.zip` into the Graveyard Keeper 2 installation directory and confirm:

~~~text
BepInEx/plugins/GK2Plus/GK2Plus.dll
~~~

Press **F2** to open GK2+.

---

**GK2+ — one mod, your way.**
