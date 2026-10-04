# Nexus Mods Page Copy — GK2+

This file is the maintainer-owned source copy for the public Nexus Mods page.

## Mod Name

**Graveyard Keeper Plus (GK2+)**

## Short Summary

**A modular, compatibility-friendly quality-of-life and gameplay enhancement suite for Graveyard Keeper 2.**

## Main Description

# GK2+ — Graveyard Keeper Plus

**One mod, your way.**

GK2+ is a modular, configurable quality-of-life and gameplay enhancement suite for **Graveyard Keeper 2**.

Instead of installing a pile of small overlapping mods, GK2+ brings multiple improvements together behind one native-style in-game menu while still letting you enable or disable individual features.

**Current Release:** v2.0.0  
**Tested With:** Graveyard Keeper 2 v1.008  
**Requires:** BepInEx 5.4.23.5

## What's New in 2.0

### Native-Style GK2+ Menu

The GK2+ menu has been rebuilt around Graveyard Keeper 2's own visual language.

- native-style window, header, tab, button, and panel styling;
- grouped expandable feature settings;
- child options live inside the same panel as their parent feature;
- configurable keyboard hotkey;
- controller / Steam Deck menu shortcut.

Default controls:

- **F2** — open / close GK2+;
- **Esc** — close GK2+;
- **L3 + R3 (hold)** — open / close GK2+ on controller / Steam Deck;
- **B** — close GK2+ on controller.

The keyboard hotkey and controller shortcut can be changed from the **General** tab.

### RPG Quest Journal

GK2+ can replace the native Quests page with an RPG-style Quest Journal.

Features include:

- Active / Completed filters;
- quests grouped by NPC;
- expandable NPC quest groups;
- quest portraits, status, descriptions, and item objectives;
- direct Unified Tracker integration;
- per-quest pinning;
- **Unpin All** for quest pins without clearing craft, item, or plan tracking.

Disable the feature at any time to return to the vanilla quest tree.

### Unified Tracker

Track multiple goals in one compact gameplay HUD:

- quests;
- crafts / recipes;
- construction targets;
- custom item quantity targets.

The tracker is intentionally compact and bounded so it does not grow endlessly when several goals are active.

### Sprinting

Adds configurable sprinting during normal free movement.

Available settings include:

- sprint key;
- movement-speed multiplier.

Release the key and movement immediately returns to native speed.

### Continuous Planting

After successfully planting a seed, the same seed remains selected while more of that exact seed remains available.

Normal cancel behavior still works, and using the final seed ends planting normally.

### Backwards Compatible Extensions

Adds narrowly scoped workstation-extension compatibility.

For v2.0.0, **Fine Tool Rack** can satisfy compatible recipes that still require the basic **Tool Rack**, while recipes that genuinely require Fine Tool Rack continue to behave normally.

## Existing Features

### Shared Storage

Shared Storage extends Graveyard Keeper 2's native inventory/storage systems while continuing to use the game's normal inventories, transfers, stack rules, capacity limits, filters, crafting checks, and resource consumption.

Available settings include:

- **Storage Scope** — Current Zone or Global;
- **Character Inventory Access** — ON/OFF;
- **Use Items From Storage** — ON/OFF;
- **Craft From Selected Scope** — ON/OFF.

With Global scope enabled, eligible storage from other areas can participate in supported inventory, consumable, crafting, and building flows.

Each capability is independently configurable.

### Bigger Item Stacks

Increase normal stack limits for stackable items.

The Stack Size Multiplier can be configured from **2x through 20x**.

Items that normally stack to 1 remain unchanged.

### Spawn Item

The Cheats tab includes a searchable, paged item browser.

Choose an item, select a quantity, and spawn it through Graveyard Keeper 2's native item pipeline.

Spawn Item uses GK2+'s normal cheat confirmation, save-safety, cheat-taint, and achievement-protection systems.

### Manual Save

Adds a native-style **Save Game** button to the in-game pause menu.

Manual Save uses Graveyard Keeper 2's own save system, allowing mid-day saves without forcing sleep or advancing time.

### Cheats

The Cheats tab includes:

- Silver grants;
- Gold grants;
- Heal Player;
- Refill Energy;
- Spawn Item.

Cheat actions are protected by GK2+'s save-safety and achievement-integrity systems.

## Save Safety

GK2+ includes a shared safety system for features that perform persistent mutations.

When a protected action needs to modify persistent game state, GK2+ creates a pre-mutation checkpoint. Additional protected actions reuse that checkpoint until Graveyard Keeper 2 loads or writes another save.

GK2+ retains up to five backup directories per save slot.

Normal quality-of-life features do **not** mark a save as cheated simply because GK2+ is installed.

When a cheat-classified action is confirmed:

- the active save lineage is permanently marked as cheat-enabled by GK2+;
- retained/future GK2+ backups preserve that taint;
- platform achievement progress/unlock calls are blocked while that tainted save is active.

## Compatibility

GK2+ is built around:

- narrow Harmony patches;
- native Graveyard Keeper 2 systems;
- independently configurable features;
- minimal interference with unrelated game behavior.

Where practical, overlapping features can be disabled individually instead of forcing players to choose between entire mods.

If another mod provides an implementation you prefer, disable the corresponding GK2+ feature.

## Installation

### Requirements

- Graveyard Keeper 2 on Windows
- BepInEx 5.4.23.5

### Install

1. Install BepInEx for Graveyard Keeper 2.
2. Download the GK2+ release archive.
3. Extract it into the Graveyard Keeper 2 installation directory.
4. Confirm this file exists:

~~~text
BepInEx/plugins/GK2Plus/GK2Plus.dll
~~~

5. Launch the game normally.
6. Confirm the GK2+ status badge appears on the main menu.
7. Press **F2** by default, or use your configured keyboard/controller shortcut.

## Links

**Feature Catalog**  
https://github.com/duhhbzz/GK2Plus/blob/main/docs/FEATURES.md

**Changelog**  
https://github.com/duhhbzz/GK2Plus/blob/main/CHANGELOG.md

**GitHub / Source Code**  
https://github.com/duhhbzz/GK2Plus

**Bug Reports & Feature Requests**  
https://github.com/duhhbzz/GK2Plus/issues

**Support Development**  
https://buymeacoffee.com/duhhbzz

GK2+ is free and open source. Donations are optional and do not gate features or support.

---

**GK2+ — one mod, your way.**

## Maintainer Note

The public Nexus main-page description is maintainer-owned.

The release workflow may update release-file metadata, but the maintainer must verify the live main page against this file before considering a release complete.
