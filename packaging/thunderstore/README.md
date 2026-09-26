# GK2+ — Graveyard Keeper Plus

**One mod, your way.**

GK2+ is a modular quality-of-life and gameplay enhancement suite for **Graveyard Keeper 2**.

## v0.1.0 — First Gameplay Release

**Tested with Graveyard Keeper 2 v1.006.** Newer game versions are allowed to load but are treated as unvalidated until tested.

### Manual Save

Adds a native-style **Save Game** button to the in-game pause menu.

- Uses Graveyard Keeper 2's native save system.
- Saves mid-day without forcing sleep or advancing the day.
- Preserves player position and tested world state across reload.
- Adds real last-save age/time to the Exit to Main Menu confirmation.
- Keyboard/mouse and controller navigation tested.

### Cheats

Press **F2** during gameplay and open the **Cheats** tab.

Current actions:

- +1 / +5 / +10 / +100 Silver
- +1 / +5 / +10 / +100 Gold
- Heal Player
- Refill Energy

Money actions use GK2's native resource-change path and normal money feedback.

### Cheat Saves and Achievements

The first cheat used on a save displays a permanent warning.

If confirmed, GK2+:

- permanently marks that save as cheat-tainted;
- propagates the taint to its GK2+ safety backups;
- remembers the taint across restarts;
- blocks platform achievement progress/unlock calls while that tainted save is active.

Normal QoL features such as Manual Save do **not** taint a save.

### Save Safety

Persistent Moderate/High-risk actions use pre-mutation safety checkpoints.

Repeated risky actions in the same save generation reuse one checkpoint instead of repeatedly copying the save.

GK2+ retains up to **5 safety backups per save slot**.

## Controls

- **F2** — open/close GK2+
- **Esc** — close GK2+

## Requirements

This Thunderstore package declares the Graveyard Keeper 2 community's BepInEx pack as a dependency, so compatible mod managers can install the required BepInEx runtime automatically.

## Known v0.1.0 Limitations

- Heal Player uses GK2's native full-heal path but has not yet been hands-on tested while the player is actually damaged.
- Cheat-taint persistence is runtime validated; a naturally triggered platform achievement attempt has not yet been observed during testing.
- Automated backup restore is intentionally not included yet.

## Links

- [GitHub / Source](https://github.com/duhhbzz/GK2Plus)
- [Report an Issue](https://github.com/duhhbzz/GK2Plus/issues)
- [☕ Support on Buy Me a Coffee](https://buymeacoffee.com/duhhbzz)

Support is completely optional. GK2+ remains free and open source, with no features or support gated behind donations.

**GK2+ — one mod, your way.**
