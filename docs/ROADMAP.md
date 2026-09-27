# GK2+ Roadmap

> **GK2+ — one mod, your way.**

This roadmap tracks features that are considered **high-confidence, technically feasible targets** for GK2+.

These are not speculative wishlist items. They are features with strong implementation precedent in the Graveyard Keeper modding ecosystem and/or systems that GK2+ has already mapped closely enough to treat as realistic development targets.

Exact implementation details, UI, defaults, compatibility behavior, and release order may still change as Graveyard Keeper 2 evolves.

## Roadmap Status

- ✅ **Shipped** — available in a public GK2+ release.
- 🧪 **In Development** — actively being implemented/tested.
- 🎯 **Planned / High Confidence** — technically feasible and intended for GK2+.
- 🔬 **Recon First** — feasible, but the safest GK2-specific implementation path still needs targeted recon before coding.

---

## Current Foundation

GK2+ already provides the framework needed to build the roadmap below:

- ✅ Persistent native-style GK2+ in-game menu
- ✅ Modular feature registry
- ✅ BepInEx configuration support
- ✅ Save-safety checkpoints for persistent mutations
- ✅ Manual Save
- ✅ Cheat-save tainting and achievement protection
- ✅ Functional money / heal / energy cheat actions
- ✅ Shared Storage with Current Zone / Global scope and independent access/use/crafting capabilities
- ✅ Bigger Item Stacks with configurable multiplier
- ✅ Spawn Item cheat
- 🧪 Configurable GK2+ menu hotkey
- 🧪 Continuous Planting

---

# World / Building Quality of Life

## 🎯 Move Buildings and Crafting Stations

Allow placed buildings, workstations, and other supported world objects to be repositioned without forcing the player to demolish and rebuild them.

### Intended behavior

- Enter a move/reposition mode for a supported placed object.
- Preserve the existing object's state wherever technically safe.
- Validate the destination using GK2's placement rules.
- Avoid duplicating or deleting the object if placement fails.
- Use save-safety protection for higher-risk world mutations.

### Important compatibility concerns

Moving persistent world objects can affect:

- WGO/world-object state;
- attached inventories;
- queued crafts;
- worker/zombie assignments;
- object-specific progression state.

GK2+ should fail closed for unsupported objects rather than attempting a destructive generic move.

---

## 🎯 Upgrade Workstations In Place

Upgrade a workstation to the next supported tier without requiring demolition followed by construction of the replacement.

### Intended behavior

- Detect valid upgrade paths.
- Consume the normal upgrade/building requirements.
- Preserve location and orientation.
- Preserve supported workstation state and attached storage when safe.
- Prevent invalid downgrades or unsupported cross-type replacements.

This should feel like a native **Upgrade** action rather than a delete/rebuild shortcut.

---

## 🎯 Return Materials After Demolition

Allow configurable material recovery when a supported building or workstation is demolished.

### Planned configuration

Possible modes:

- Vanilla behavior
- Partial material refund
- Full material refund
- Custom percentage

Refund logic should use actual build requirements whenever possible rather than maintaining a separate hard-coded recipe table.

---

# Inventory / Storage

## 🎯 Big-Item Stacking

Allow normally non-stackable or low-stack-capacity large items to stack.

### Goals

- Reduce unnecessary inventory friction.
- Keep stack limits configurable.
- Avoid altering unrelated normal-item stack behavior.
- Preserve serialization compatibility.

This feature should be independently toggleable because stack-size changes can overlap with other inventory mods.

---

## 🎯 Big-Item Container Capacity

Increase how many large/big items supported containers can hold.

### Planned configuration

- Vanilla capacity
- Preset larger capacities
- Custom capacity where technically safe

Container changes must be tested carefully against loading, saving, UI slot presentation, and existing over-capacity saves.

---

# Crafting / Automation

## 🎯 Improved Auto Crafting

Expand and improve automated crafting behavior.

Potential capabilities include:

- smarter queue continuation;
- improved resource pulling;
- better handling of interrupted jobs;
- clearer queue/status information;
- more predictable automation around available inputs/outputs;
- optional integration with nearby/shared storage.

Because GK2 1.006 changed parts of craft completion and worker/zombie pickup handling, implementation should build on the refreshed crafting recon rather than older assumptions.

---

# Movement

## 🎯 Sprinting

Add a configurable sprint action that increases normal walking speed while a button is held.

### Planned behavior

- Hold-to-sprint by default.
- Configurable sprint key.
- Configurable speed multiplier.
- Normal movement returns immediately when the key is released.
- Avoid affecting cutscenes, scripted movement, knockback, or other non-player-driven movement states.

The binding should use the same conflict-aware keybinding system being developed for the GK2+ menu hotkey.

---

# Planner / Tracking / Notes

## 🎯 Pinning System

Provide one unified player-facing pinboard for several types of information.

Planned pin types:

- Quests
- Crafts / recipes
- Shopping lists
- Custom notes
- Custom reminders

### Desired UX

Pinned items should be visible without forcing the player to repeatedly reopen several native windows.

Possible presentation options:

- compact HUD tracker;
- expandable pinned-items panel;
- per-pin visibility toggle;
- ordering / grouping;
- quick complete/remove actions for custom notes and shopping-list entries.

Quest and craft pins should reference game data rather than copying static text when possible.

---

## 🎯 In-Game Wiki

Add a searchable reference browser inside GK2+.

Potential categories:

- Items
- Recipes
- Workstations
- Crops / farming
- NPCs
- Quests
- Technologies
- Zombies / workers
- Locations
- Game systems

The wiki should prefer data discovered from the running game so it stays closer to the installed game version and localization.

This should remain primarily read-only and should not require save mutation.

---

## 🎯 Daily Reminder Popups

Show a lightweight daily reminder when a new in-game day begins.

### Example content

- Current weekday/day
- Important recurring weekly events
- Player-created reminders
- Pinned time-sensitive tasks

The preferred presentation is a small native-style thought-bubble/toast rather than an intrusive full-screen dialog.

Players should be able to disable reminder categories individually.

---

# Time / World Schedule

## 🎯 Configurable Day / Night Cycle

Allow players to change the relative speed/length of the game's time cycle.

Possible controls:

- Overall day length multiplier
- Daytime length
- Nighttime length
- Longer days / shorter nights
- Shorter days / longer nights
- Restore vanilla timing

This must preserve scheduled events and time-dependent systems as much as possible rather than simply freezing or skipping game time.

---

# Events / Convenience

## 🎯 Trigger Donkey Delivery On Demand

Provide an optional action that requests/triggers the donkey delivery flow manually rather than waiting for its normal schedule.

### Safety goals

- Reuse the game's own delivery/event logic where possible.
- Prevent duplicate active deliveries.
- Respect required progression/unlocks.
- Avoid repeatedly granting the same delivery state.

This is intended as an opt-in convenience/cheat-adjacent feature and may be placed under either **Automation**, **More**, or **Cheats** depending on final behavior.

---

# Cheats / Gameplay Modifiers

All features in this section are considered cheat-mode behavior.

Using them on a save should follow GK2+'s existing cheat-integrity rules:

- the save is marked cheat-tainted;
- platform achievements are disabled for that save lineage;
- GK2+ safety backups inherit the taint marker.

## 🎯 More Prayer Rewards

Add configurable prayer reward multipliers.

Potential settings:

- 1x / Vanilla
- 2x
- 3x
- 5x
- Custom multiplier

Where practical, the multiplier should apply through the normal reward path so native feedback remains intact.

---

## 🎯 More Crafting Yield

Increase output quantities from supported crafting recipes.

Potential settings:

- 1x / Vanilla
- 2x
- 3x
- 5x
- Custom multiplier

The implementation must avoid multiplying special progression outputs, quest-only rewards, or other one-off results unless explicitly supported.

---

## 🎯 More Tech Points

Increase technology-point rewards or provide configurable technology-point gain multipliers.

Potential scope:

- Red points
- Green points
- Blue points
- Any additional GK2-specific tech-point resources discovered during implementation

Possible configuration:

- Vanilla
- Per-color multiplier
- Global multiplier
- Direct grant controls in the Cheats tab

---

# Suggested Development Waves

The roadmap is not locked to release numbers, but a sensible implementation order is:

### Wave 1 — Low-Risk Player QoL

- Continuous Planting
- Sprinting
- Pinning / custom notes
- Daily reminders
- In-game wiki

### Wave 2 — Inventory / Crafting

- Big-item stacking
- Big-item container capacity
- Improved Auto Crafting
- Return materials after demolition

### Wave 3 — Persistent World Mutations

- Move buildings / crafting stations
- Upgrade workstations in place
- Trigger donkey delivery on demand

### Wave 4 — Time / Gameplay Configuration

- Configurable day/night cycle
- Additional automation controls

### Cheat Expansion

Can be developed alongside other waves as the relevant game systems are mapped:

- More Prayer Rewards
- More Crafting Yield
- More Tech Points

---

# Roadmap Rules

A feature being listed here means GK2+ intends to pursue it, but implementation should still follow the project's normal engineering rules:

1. Recon the exact GK2 system first.
2. Prefer native game APIs and data paths.
3. Keep Harmony patches narrow.
4. Add a feature-level toggle where practical.
5. Protect risky persistent mutations with save safety.
6. Detect known overlapping mods where possible.
7. Prefer warnings and player choice over silently disabling another mod.
8. Test save/load behavior before calling a persistent feature complete.
9. Revalidate affected systems after significant GK2 game updates.
10. Keep proprietary/decompiled game source out of the public repository.

---

## Community Suggestions

This roadmap is intentionally expandable.

Good feature candidates are those that:

- remove repetitive friction;
- improve management without replacing the core game;
- have strong community demand;
- have existing modding precedent;
- can be implemented modularly;
- can coexist with other mods through feature toggles.

Ideas can be proposed through the project's GitHub Issues or community discussion channels.
