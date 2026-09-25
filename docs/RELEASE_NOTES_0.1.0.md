# GK2+ v0.1.0 — First Gameplay Release

GK2+ v0.1.0 is the first release that moves beyond the original framework/menu preview and adds functional gameplay features.

## Highlights

### Manual Save

- Adds a native-style **Save Game** button to the pause menu.
- Uses Graveyard Keeper 2's own save system.
- Saves mid-day without forcing sleep or advancing the day.
- Reload testing preserved player position, inventory/economy state, and moved world-object state.
- Exit confirmation now shows when the active save was last written.
- Keyboard/mouse and controller navigation tested.

### Functional Cheats

The F2 **Cheats** tab now includes:

- +1 / +5 / +10 / +100 Silver
- +1 / +5 / +10 / +100 Gold
- Heal Player
- Refill Energy

Money actions use the game's native resource-change path and display the normal money feedback.

### Save Safety

- Moderate/High-risk mutations create a pre-mutation checkpoint.
- Repeated risky actions reuse that checkpoint until the game loads/writes a save.
- Five backups are retained per slot.
- Backups are streamed rather than buffered as whole-save byte arrays.

### Cheat / Achievement Integrity

The first cheat used on a save shows a permanent warning.

After confirmation:

- the active save is marked cheat-tainted by GK2+;
- retained/future GK2+ backups for that slot carry the taint;
- the taint persists after restart;
- platform achievement progress/unlock calls are blocked while that tainted save is active.

## Known Limitations

- **Heal Player** uses the native full-heal path but has not yet been hands-on tested while HP is below maximum.
- Cheat-taint persistence is runtime validated; a naturally triggered platform achievement attempt has not yet been observed in runtime testing.
- Automated backup restore is intentionally not included.
- More QoL/gameplay modules are still in development.

## Installation

Requires **BepInEx 5.4.23.5**.

Extract `GK2Plus-0.1.0.zip` into the Graveyard Keeper 2 installation directory and confirm:

~~~text
BepInEx/plugins/GK2Plus/GK2Plus.dll
~~~

Press **F2** to open GK2+.

## Recommended Release Screenshots

1. Cheats tab with the functional action grid.
2. Pause menu with **Save Game**.
3. Exit confirmation showing **Last saved: ...**.
4. Optional first-cheat achievement warning.

---

**GK2+ — one mod, your way.**
