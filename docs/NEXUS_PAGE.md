# Nexus Mods Page Copy — GK2+

This file is the maintainer-owned source copy for the public Nexus Mods page.

Keep the main listing concise. Detailed feature behavior belongs in the canonical feature catalog instead of being duplicated here.

## Mod Name

**Graveyard Keeper Plus (GK2+)**

## Short Summary

**A modular, configurable quality-of-life and gameplay enhancement suite for Graveyard Keeper 2. Use the features you want and disable the ones you do not.**

## Main Description

**GK2+ — one mod, your way.**

Graveyard Keeper Plus is a modular all-in-one enhancement suite for **Graveyard Keeper 2**.

GK2+ combines quality-of-life tools, gameplay improvements, optional cheats, and shared safety/compatibility infrastructure behind one mod. Features are designed to be independently disableable where practical so players can keep another mod's implementation of an overlapping feature.

### Detailed features

The complete maintained feature list, behavior, settings, compatibility notes, and validation details live here:

**Feature Catalog**  
https://github.com/duhhbzz/GK2Plus/blob/main/docs/FEATURES.md

Release-specific changes:

**Changelog**  
https://github.com/duhhbzz/GK2Plus/blob/main/CHANGELOG.md

### Controls

Press **F2** to open/close GK2+.

The menu works from both the main menu and active gameplay.

### Compatibility philosophy

GK2+ prefers narrow Harmony patches, native game systems, independently disableable modules, and player choice over silently blocking third-party mods.

### Save safety / cheats

Protected persistent mutations use GK2+'s shared checkpoint system. Optional cheat actions also use per-save cheat tainting and achievement protection. Normal QoL features do not taint a save merely because GK2+ provides them.

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
7. Press **F2** to open the mod menu.

## Source / Bugs / Features

Feature Catalog:  
https://github.com/duhhbzz/GK2Plus/blob/main/docs/FEATURES.md

GitHub / Source:  
https://github.com/duhhbzz/GK2Plus

Issues:  
https://github.com/duhhbzz/GK2Plus/issues

Support development:  
https://buymeacoffee.com/duhhbzz

GK2+ is free and open source. Donations are optional and do not gate features or support.

## Maintainer Note

The public Nexus main-page description is maintainer-owned.

The release workflow may update release-file metadata, but the maintainer must verify the live main page against this file before considering a release complete.
