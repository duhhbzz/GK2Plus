# GK2+ v2.0.0 — Native UI, Quest Journal & Unified Tracker

GK2+ v2.0.0 is a major feature and presentation release focused on native-style UI, quest/tracking quality of life, movement/farming improvements, and stronger controller / Steam Deck access.

**Tested with Graveyard Keeper 2 v1.008.**

## Highlights

### Native-Style GK2+ Menu

The persistent GK2+ menu has been rebuilt around Graveyard Keeper 2's native UI language:

- native window/header/tab/button styling;
- grouped, expandable parent/child settings;
- one shared panel for a feature and its sub-options;
- reusable framework components for future GK2+ screens;
- documented UI standards so new features follow the same visual structure.

The default keyboard shortcut remains **F2**, but it is now configurable from the **General** tab.

Controller / Steam Deck users can also:

- hold **L3 + R3** briefly to open/close GK2+;
- press **B** to close the top-most GK2+ menu/picker.

### RPG Quest Journal

The native Quests page can now be replaced by GK2+'s RPG-style Quest Journal.

Features include:

- Active / Completed filters;
- NPC-grouped quest navigation;
- expandable quest groups;
- quest portraits, status, descriptions, and item objectives;
- direct Unified Tracker integration;
- per-quest pinning;
- quest-only **Unpin All** without clearing craft/item/plan pins.

The journal reuses the same native-style UI framework as the F2 menu and cleanly returns to the vanilla quest tree when disabled.

### Unified Tracker

Unified Tracker combines multiple goals into one compact gameplay HUD:

- quests;
- crafts / recipes;
- construction targets;
- custom item quantity targets.

It supports native gameplay tracking interactions where available, automatic new-quest tracking options, completed-quest cleanup behavior, and management from the Tracker tab.

The HUD intentionally stays compact and bounded instead of expanding indefinitely when many goals are tracked.

### Sprinting

Adds configurable sprinting during normal free movement:

- configurable sprint key;
- configurable movement-speed multiplier;
- returns immediately to native speed when the key is released.

### Continuous Planting

After a successful planting action, the same seed remains selected while more of that exact seed is available.

Normal cancel/menu behavior and the final seed still end planting normally.

### Backwards Compatible Extensions

Adds narrowly scoped workstation-extension compatibility.

For v2.0.0, **Fine Tool Rack** can satisfy compatible recipes that still require the basic **Tool Rack**, while recipes that genuinely require Fine Tool Rack continue to behave normally.

### Existing GK2+ Features Retained

v2.0.0 also retains and regression-tests the existing feature set, including:

- Manual Save;
- Shared Storage;
- Bigger Item Stacks;
- Spawn Item;
- money / heal / energy cheats;
- save-safety checkpoints;
- per-save cheat taint;
- achievement-integrity protection.

## Validation

The final v2.0.0 validation pass completed successfully against GK2 v1.008.

Validated areas include:

- configurable keyboard menu access;
- controller / Steam Deck menu access;
- native F2-menu grouping and collapse behavior;
- RPG Quest Journal lifecycle and navigation;
- Unified Tracker pin/unpin behavior;
- Sprinting;
- Continuous Planting;
- Backwards Compatible Extensions;
- Manual Save;
- Shared Storage;
- Bigger Item Stacks;
- Spawn Item;
- Heal Player;
- Refill Energy;
- cheat warning / taint / checkpoint behavior.

Debug and Release builds passed with compiler warnings treated as errors.

The canonical release, Thunderstore package, and Steam Workshop staging package also passed the automated v2.0 release-readiness preflight.

## Installation

Requires **BepInEx 5.4.23.5**.

Extract `GK2Plus-2.0.0.zip` into the Graveyard Keeper 2 installation directory and confirm:

~~~text
BepInEx/plugins/GK2Plus/GK2Plus.dll
~~~

Default controls:

~~~text
F2            Open / close GK2+ (configurable)
ESC           Close GK2+
L3 + R3 hold  Open / close GK2+ on controller / Steam Deck
B             Close GK2+ on controller
~~~

## Upgrade Notes

Existing GK2+ configuration remains in the normal BepInEx config file.

New v2.0.0 menu-access settings are live-safe and can be changed from the General tab.

The new UI framework, Quest Journal, Tracker, Sprinting, Continuous Planting, and Backwards Compatible Extensions do not require UnityExplorer or any development/recon tooling at runtime.

---

**GK2+ — one mod, your way.**
