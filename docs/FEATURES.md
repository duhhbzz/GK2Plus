# GK2+ Feature Catalog

This is the canonical player-facing feature catalog for **GK2+ (Graveyard Keeper Plus)**.

The README and mod-platform landing pages intentionally stay concise. Detailed feature behavior belongs here so public descriptions do not become an ever-growing laundry list.

> Availability is release-specific. Use the [Changelog](../CHANGELOG.md) and GitHub Releases to determine which cataloged features exist in a published version.
>
> Looking for planned work rather than implemented behavior? See the **[GK2+ Roadmap](ROADMAP.md)**.

## Feature Index

| Category | Feature |
| --- | --- |
| General / UI | [GK2+ Mod Menu](#gk2-mod-menu) |
| General | [Manual Save](#manual-save) |
| General | [Last Save Status](#last-save-status) |
| Crafting | [Backwards Compatible Extensions](#backwards-compatible-extensions) |
| Tracker | [Unified Tracker](#unified-tracker) |
| Inventory | [Bigger Item Stacks](#bigger-item-stacks) |
| Inventory | [Shared Storage](#shared-storage) |
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

## Backwards Compatible Extensions

**Category:** Crafting  
**Setting mode:** Main-menu enable/disable; read-only status during gameplay.

Backwards Compatible Extensions lets a supported upgraded workstation extension satisfy recipes that require its lower-tier predecessor.

The initial compatibility rule is:

~~~text
Fine Tool Rack
  counts as Fine Tool Rack
  + counts as Tool Rack for recipe requirements
~~~

This means a correctly attached **Fine Tool Rack** can satisfy recipes that still list the basic **Tool Rack** requirement, so players do not need to keep both rack tiers beside the same compatible workbench solely for older recipes.

The feature is **OFF by default** to preserve vanilla behavior unless the player opts in from the GK2+ **Crafting** tab.

### Native behavior and compatibility

GK2+ does not create fake workstation links and does not change which extensions a workbench can physically accept. The game must already recognize the Fine Tool Rack as a valid attached extension for that workbench.

GK2+ only relaxes GK2's final recipe-extension eligibility check when a recipe requires `tool_rack` and a valid attached `tool_rack_fine` is present. All other recipe requirements, progression, mastery, tools, ingredients, energy, queues, and crafting behavior remain native.

### Validation status

The implementation is based on GK2's native `CraftComponent.IsCraftAllowedByAttachedExtensions` path and the game's own attached-workbench-extension links. Runtime validation is still required before release, specifically:

- Fine Tool Rack satisfies a Tool Rack recipe on a compatible workbench when enabled;
- Fine Tool Rack recipes continue to require the Fine Tool Rack normally;
- disabling the option restores vanilla Tool Rack requirements;
- unrelated extensions and workbenches are unaffected.

---

## Unified Tracker

**Category:** Tracker  
**Setting mode:** Pin management while a save is loaded; overall feature enable/disable from the main menu.

The Unified Tracker provides one compact HUD for **active quests**, **known crafts/recipes**, and **custom item quantity targets**.

### Tracker tab

While a save is loaded, the Tracker tab exposes searchable selectors for:

- active quests;
- crafts/recipes currently available from GK2's game-balance/knowledge data;
- normal item definitions;
- common target quantities for custom item pins.

Selected entries can be pinned independently. The first implementation supports up to **12 pins** and stores only GK2+ tracker metadata; it does not alter quest, crafting, or inventory state.

### HUD behavior

The compact HUD refreshes its displayed values four times per second rather than doing expensive game-data discovery every frame.

- **Quest pins** show the localized quest name, current description/objective, and item requirements when the quest exposes them.
- **Craft pins** show the localized/result name and the player's current count versus each recipe requirement.
- **Item pins** show the player's current inventory count against the configured target and mark the row complete when the target is reached.

Pins remain until the player removes or clears them; completing a quest or reaching an item target does not silently delete the pin.

### Current scope

Initial material/item counts use the **player inventory**. Shared Storage-aware tracker scopes can be added after the core tracker UX is validated.

The initial HUD is intentionally a lightweight vertical slice so quest/craft/item data behavior can be runtime-tested before final native-style presentation polish.

### Validation required

- pin/unpin active quests;
- search and pin a known craft;
- search and pin several normal items with different target quantities;
- verify counts update as items enter/leave player inventory;
- verify quest completion/status changes update without mutating the quest;
- save/reload and confirm tracker pins persist through GK2+ configuration;
- confirm Clear Pins and HUD toggle behavior;
- check common resolutions for clipping/overlap before final UI polish.

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

## Shared Storage

**Category:** Inventory  
**Setting mode:** Main-menu configuration; read-only status during gameplay.

Shared Storage exposes eligible Graveyard Keeper 2 storage through one configurable feature family while continuing to use GK2's native inventories, transfer logic, stack limits, filters, capacity rules, notifications, crafting checks, and resource consumption.

GK2+ does **not** create a separate shared-storage database or custom shared-storage save format.

### Configuration

From the **main menu → Inventory** tab:

~~~text
Shared Storage                              [ ON ]
    Storage Scope                   [ Current Zone ▼ ]
    Character Inventory Access              [ ON ]
    Use Items From Storage                  [ OFF ]
    Craft From Selected Scope               [ OFF ]
~~~

**Storage Scope** controls which eligible storage participates:

- **Current Zone** — uses GK2's current world-zone storage scope.
- **Global** — also exposes eligible persisted storage from other zones.

Scope and capability are intentionally separate. Turning one child capability off does not disable the others.

Binary child settings toggle directly when clicked. Multi-choice settings such as **Storage Scope** open the normal selector.

### Character Inventory Access

When ON, eligible Shared Storage appears in the character inventory and can use GK2's native transfer behavior.

When OFF, character-inventory remote storage returns to the vanilla disabled/read-only behavior while normal chest-window Shared Storage can remain enabled.

### Use Items From Storage

When ON, supported consumable **Use** actions can be executed directly from eligible Shared Storage shown in the character inventory.

GK2+ redirects the native item-removal step to the selected source inventory while leaving the normal player item-use effects in control.

This setting intentionally does **not** make all player-only context actions remote-capable. **Equip, Plant, Fertilize, Destroy, and hotbar actions remain player-inventory only** unless separately supported later.

### Craft From Selected Scope

GK2 already supports crafting from eligible storage in the current world zone.

When **Storage Scope = Global** and **Craft From Selected Scope = ON**, GK2+ extends the native crafting and blueprint/building inventory sources with eligible cross-zone Shared Storage. GK2's own recipe checks, counts, material consumption, workstation rules, and building rules remain authoritative.

Turning this option OFF restores crafting/building material access to GK2's normal zone-bound behavior without disabling Shared Storage browsing or transfers.

### Chest behavior

With Shared Storage enabled:

- normal chest windows can browse eligible storage in the selected scope;
- full, partial, and single-stack transfers continue through GK2's native inventory paths;
- storage-to-storage, player-to-storage, and storage-to-opened-chest transfers retain native capacity/filter/stack behavior.

### Validation completed

Runtime validation covered:

- current-zone and Global storage discovery;
- character-inventory and normal chest-window access;
- Global cross-zone chest visibility;
- remote chest selection;
- remote → opened chest, opened chest → remote, and chest → chest transfers;
- full-stack, partial-stack, and single-item movement;
- full destination behavior;
- Character Inventory Access ON/OFF behavior;
- direct consumable Use from remote eligible storage;
- Global crafting from materials stored only in another zone;
- Global blueprint/building material checks and actual remote resource consumption;
- Craft From Selected Scope OFF restoring zone-bound crafting/building behavior;
- switching Global back to Current Zone;
- save/reload persistence for transferred items;
- main-menu configuration and read-only in-game status;
- inline binary child-setting toggles and selector behavior for non-binary settings.

### Compatibility

Shared Storage uses GK2's native inventory objects and deliberately keeps stack, filter, capacity, transfer, crafting, and building behavior in the game's own systems wherever possible.

The stable internal feature/config ID remains **`shared-chests`** for configuration compatibility even though the player-facing feature is now named **Shared Storage**.

If another mod should own overlapping storage behavior, disable Shared Storage or the specific overlapping child capability rather than uninstalling GK2+.


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

### Current limitation

Spawn Item is currently intended primarily for normal inventory items. Big carryables and world-linked definitions are still inserted through the inventory path rather than being routed through GK2's native physical world-drop/carry behavior. Native drop routing is being tested separately and is not part of v0.1.5.

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

If the work also changes the status or scope of a planned roadmap item, update [ROADMAP.md](ROADMAP.md) as well. Keep planned behavior in the roadmap and implemented/released behavior in this catalog.

The entry should explain:

- what the feature does;
- how the player uses it;
- where settings live;
- whether settings are main-menu-only, live-safe, or restart-required;
- save/persistence behavior when relevant;
- compatibility/overlap concerns;
- validation actually completed.

Contributors do **not** need to update the changelog/version/release metadata unless the maintainer explicitly asks them to. See [GOVERNANCE.md](GOVERNANCE.md) and [CONTRIBUTING.md](../CONTRIBUTING.md).
