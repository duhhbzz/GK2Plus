# Nexus Mods Page Copy — GK2+ v0.0.1

## Mod Name

**Graveyard Keeper Plus (GK2+)**

## Short Summary

**A modular, compatibility-friendly quality-of-life and gameplay enhancement suite for Graveyard Keeper 2. v0.0.1 establishes the framework and native-style mod menu that future GK2+ features will use.**

## Suggested Category

Utilities / Gameplay / Miscellaneous — use whichever Graveyard Keeper 2 category best matches the available Nexus categories at upload time.

## Main Description

**GK2+ — one mod, your way.**

Graveyard Keeper Plus is a modular all-in-one enhancement suite for **Graveyard Keeper 2**. The project is being built to combine quality-of-life improvements, gameplay tweaks, management tools, and optional cheats into one configurable mod while remaining friendly to players who prefer specialized mods for individual mechanics.

### v0.0.1 — Foundation Preview

This is the first public foundation build of GK2+.

It currently includes:

- BepInEx plugin foundation
- modular feature architecture
- compatibility scanning foundation
- shared framework services for major game systems
- a native-style GK2+ status badge on the main menu
- an F2 GK2+ mod-menu shell
- General, Inventory, Crafting, Farming, Zombies, Cheats, and More tabs
- GitHub and bug-report links
- versioning and release infrastructure for future modules

**No gameplay-changing modules are enabled in v0.0.1 yet.** This release establishes the framework and UI baseline first so future features can be added cleanly and independently.

### Current limitation

The custom **F2 menu currently works from the Graveyard Keeper 2 main menu only**. It does not yet persist after entering active gameplay. Fixing the in-game UI lifecycle is one of the next framework milestones.

### Planned direction

Planned feature areas include:

- Inventory and storage QoL
- Crafting improvements
- Farming improvements such as continuous planting
- Movement and convenience options
- Zombie management tools
- Automation/economy helpers
- Optional cheat utilities
- Compatibility warnings and per-feature toggles

Features will be added only after the relevant game systems have been researched and tested. Planned items may change during development.

### Installation

**Requirements:**

- Graveyard Keeper 2 on Windows
- BepInEx 5.4.23.5

**Install:**

1. Install BepInEx for Graveyard Keeper 2.
2. Download the GK2+ archive.
3. Extract it into your Graveyard Keeper 2 installation directory.
4. Confirm the file exists at:

`BepInEx/plugins/GK2Plus/GK2Plus.dll`

5. Start the game normally.
6. The GK2+ status badge should appear on the main menu.
7. Press **F2** on the main menu to open the current mod-menu shell.

### Uninstall

Delete:

`BepInEx/plugins/GK2Plus/`

### Compatibility philosophy

GK2+ is being designed around modular features rather than an all-or-nothing patch set. Where practical, overlapping GK2+ features will be independently disableable so players can continue using another mod's implementation of a mechanic they prefer.

### Source / Bugs

GitHub: https://github.com/duhhbzz/GK2Plus

Issues: https://github.com/duhhbzz/GK2Plus/issues

### Early-development notice

GK2+ is still in early development. Expect features and internals to change as Graveyard Keeper 2 is researched, updated, and tested.

---

## v0.0.1 File Description

**GK2+ v0.0.1 — Foundation Preview**

First public build. Includes the BepInEx/framework foundation, compatibility scanner, main-menu GK2+ badge, and native-style F2 menu shell. No gameplay-changing modules are enabled yet. F2 currently works from the main menu only.

Install by extracting the archive into the Graveyard Keeper 2 game directory.

---

## v0.0.2 File Description Template

**GK2+ v0.0.2 — Nexus Link Update**

Small follow-up to the initial foundation release. Enables the Nexus Mods button inside the GK2+ More tab and points it to the official GK2+ mod page.

No gameplay modules are introduced by this update.
