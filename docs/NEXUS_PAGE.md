# Nexus Mods Page Copy — GK2+ v0.1.0

## Mod Name

**Graveyard Keeper Plus (GK2+)**

## Short Summary

**A modular QoL/gameplay suite for Graveyard Keeper 2 with Manual Save, functional money/energy cheats, native-style UI, save-safety checkpoints, and per-save achievement protection for cheat use.**

## Suggested Category

Gameplay / Utilities / Miscellaneous — use whichever Graveyard Keeper 2 category best matches the available Nexus categories when the page is restored/published.

---

## Main Description

**GK2+ — one mod, your way.**

Graveyard Keeper Plus is a modular all-in-one enhancement suite for **Graveyard Keeper 2**.

The project is built around small, independently testable modules so players can use GK2+ for quality-of-life improvements, management tools, gameplay tweaks, or optional cheats without every feature needing to touch the same systems.

### v0.1.0 — First Gameplay Release

v0.1.0 moves GK2+ beyond the original framework/menu preview and adds real gameplay functionality.

**Tested with Graveyard Keeper 2 v1.006.** Newer game versions are allowed to load but are treated as unvalidated until tested.

### Manual Save

GK2+ adds a native-style **Save Game** button to the pause menu.

The mod delegates the actual save to Graveyard Keeper 2's own save system rather than implementing a custom save format.

Runtime-tested behavior includes:

- save at the current time of day;
- no forced sleep/day transition;
- reload at the same player location;
- inventory/money persistence;
- moved world-object persistence;
- native saving indicator;
- keyboard/mouse and controller support.

The **Exit to Main Menu** confirmation also shows how long ago the active slot was last saved.

### Functional Cheats

Open GK2+ with **F2** during gameplay and select the **Cheats** tab.

Current cheat actions:

**Silver**
- +1
- +5
- +10
- +100

**Gold**
- +1
- +5
- +10
- +100

**Player**
- Heal Player
- Refill Energy

Money cheats use the game's native resource-change behavior, including the normal money feedback/animation.

Refill Energy restores the normal work/action energy bar.

### Cheat Saves and Achievements

GK2+ deliberately treats cheat use as a permanent choice for that save.

Before the first cheat is used, a native confirmation warns that platform achievements will be disabled for the save and its GK2+ backup lineage.

If confirmed:

- the active save is marked cheat-tainted by GK2+;
- existing GK2+ backups for that slot are marked tainted;
- future GK2+ backups inherit the marker;
- future cheat actions on that save do not ask again;
- GK2+ blocks the game's platform achievement progress/unlock boundary while that tainted save is loaded.

The marker is stored as GK2+ metadata rather than by changing Graveyard Keeper 2's serialized save schema.

This is an integrity feature, not anti-tamper DRM. Deliberately removing/modifying GK2+ metadata can bypass a mod-level restriction.

### Save Safety

Persistent Moderate/High-risk GK2+ mutations use a checkpoint system.

Example:

~~~text
Load save
→ no backup write

First money cheat
→ create one pre-mutation checkpoint

More money cheats
→ reuse the same checkpoint

Native save/load
→ invalidate checkpoint

Next persistent cheat
→ create a new checkpoint
~~~

GK2+ retains up to **5 backup directories per save slot**.

Backups are stored under:

~~~text
BepInEx/config/GK2Plus/SaveBackups/<slot>/
~~~

This avoids repeatedly copying the same save just because several cheat buttons are clicked in one session.

### GK2+ Menu

Press **F2** to open/close GK2+.

The menu works from:

- the main menu;
- active gameplay.

Current tabs:

- General
- Inventory
- Crafting
- Farming
- Zombies
- Cheats
- More

The custom overlay is persistent and renders above normal game windows while open.

---

## Installation

### Requirements

- Graveyard Keeper 2 on Windows
- BepInEx 5.4.23.5

### Install

1. Install BepInEx for Graveyard Keeper 2.
2. Download **GK2Plus-0.1.0.zip**.
3. Extract it into the Graveyard Keeper 2 installation directory.
4. Confirm this file exists:

~~~text
BepInEx/plugins/GK2Plus/GK2Plus.dll
~~~

5. Launch the game normally.
6. Confirm the GK2+ status badge appears on the main menu.
7. Press **F2** to open the mod menu.

### Uninstall

Delete:

~~~text
BepInEx/plugins/GK2Plus/
~~~

GK2+ configuration and safety backups are stored separately under:

~~~text
BepInEx/config/GK2Plus/
~~~

A cheat-tainted save also has a small GK2+ sidecar beside the native save file. Uninstalling the DLL does not automatically remove user metadata or backups.

---

## Compatibility Philosophy

GK2+ is built around modular features rather than one giant all-or-nothing patch.

Where technically practical, GK2+ aims to:

- keep patches narrow;
- avoid unrelated game systems;
- allow overlapping modules to be disabled;
- detect/warn about known conflicts;
- use native game APIs/events where possible;
- keep persistent mutations behind save-safety gates.

---

## Known v0.1.0 Limitations

- **Heal Player** uses the native full-heal path, but it has not yet been hands-on tested while the player is actually damaged.
- Cheat taint/backup persistence is runtime validated. The achievement platform guard is installed at the game's final platform-award boundary, but a naturally triggered achievement attempt has not yet been observed during testing.
- Automated backup restore is intentionally not included yet.
- More QoL/gameplay modules are still under development.

---

## Planned Direction

Planned areas include:

- inventory/storage QoL;
- crafting improvements;
- continuous planting and farming QoL;
- zombie management;
- quest/map tools;
- automation/economy helpers;
- additional optional cheats;
- richer per-feature settings.

Planned features may change as game systems are researched and tested.

---

## Source / Bugs

GitHub:
https://github.com/duhhbzz/GK2Plus

Issues:
https://github.com/duhhbzz/GK2Plus/issues

Support development:
https://buymeacoffee.com/duhhbzz

GK2+ is free and open source. Donations are completely optional and do not gate features or support.

---

## Suggested Nexus Screenshot Order

1. **Cheats tab** — show the Silver/Gold increments, Heal Player, and Refill Energy.
2. **Pause menu** — show the native-style Save Game button.
3. **Exit confirmation** — show the `Last saved: ...` status.
4. **First-cheat warning** — optional, demonstrates the achievement-integrity behavior.

Suggested captions:

- **Functional Cheats** — Native money actions, player recovery tools, and save-safety integration.
- **Manual Save** — Save mid-day directly from the pause menu without sleeping.
- **Last Save Status** — Exit confirmation shows when the active slot was last saved.
- **Achievement Protection** — First cheat use permanently taints that save and its GK2+ backup lineage.

---

## v0.1.0 File Description

**GK2+ v0.1.0 — First Gameplay Release**

Adds the first functional GK2+ gameplay features:

- Manual Save from the pause menu
- last-save status on exit confirmation
- +1/+5/+10/+100 Silver
- +1/+5/+10/+100 Gold
- Heal Player
- Refill Energy
- save-safety checkpoints with five-backup retention
- persistent per-save cheat taint and achievement protection
- persistent F2 menu in both main menu and gameplay

Install by extracting the archive into the Graveyard Keeper 2 game directory.

Requires BepInEx 5.4.23.5.
