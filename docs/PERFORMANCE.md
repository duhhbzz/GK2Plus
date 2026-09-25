# GK2+ Performance and Resource Standards

GK2+ is intended to stay lightweight even as the feature set grows. Performance, memory behavior, storage I/O, and cleanup are architecture requirements rather than post-release polish.

## Core Rules

1. **No heavy work every frame.** Update loops should be limited to tiny input/state checks.
2. **Prefer native game events over polling.**
3. **Cache expensive lookups** instead of repeatedly scanning Unity resources or large game collections.
4. **Every event subscription must have a matching unsubscribe.**
5. **Persistent objects must have one clear owner and teardown path.**
6. **Hidden UI should not perform background work.**
7. **Avoid reflection, LINQ, allocations, and large collection walks in hot paths.**
8. **Harmony patches must be narrow and cheap.**
9. **Production builds must not continuously write diagnostic logs or recon data.**
10. **Features that allocate resources must release them when disabled or shut down.**
11. **Save-safety I/O must be deduplicated and retention-limited.**
12. **Measure suspicious behavior instead of assuming it is harmless.**

## CPU

Good patterns:

- react to SaveSystem, quest, inventory, resource, interaction, or other native events;
- calculate UI data when the page opens or when relevant data changes;
- resolve/capture references once during initialization;
- keep hot Harmony prefixes/postfixes extremely small.

Avoid:

- Resources.FindObjectsOfTypeAll(...) every frame;
- repeated reflection/property discovery every frame;
- full inventory/world scans on a timer when an event exists;
- rebuilding UI every time F2 is pressed;
- continuous background polling without a demonstrated need.

The current GK2+ menu is built once, retained, and shown/hidden. The persistent controller performs only lightweight hotkey checks during normal play.

## RAM and Leak Prevention

A feature is not complete if it leaves references behind after shutdown.

Required practices:

- unsubscribe from every event subscribed to;
- avoid static references to scene objects unless lifecycle is explicitly managed;
- do not create duplicate DontDestroyOnLoad controllers;
- destroy owned Unity objects during teardown;
- clear caches that retain game/scene objects after they become invalid;
- do not attach GK2+ delegates to serializable game objects unless that behavior is explicitly understood and safe.

Long-session testing should include repeated menu/game transitions and repeated open/close cycles. Memory may fluctuate because of Unity and managed GC, but sustained monotonic growth after comparable cycles should be investigated.

## GPU and UI

GK2+ should remain a low-cost UI mod.

Guidelines:

- use ordinary Unity UI/native game assets rather than expensive custom rendering;
- avoid unnecessary full-screen canvases;
- disable the GK2+ overlay when the menu is closed;
- avoid constant layout rebuilds and animations on hidden UI;
- do not create new UI trees on every open;
- keep visual effects simple and consistent with the game's native interface.

The current mod menu uses one retained overlay Canvas with explicit sorting so it can render above native GK2 windows when open.

## Disk I/O

Normal gameplay should not cause continuous GK2+ disk writes.

Release behavior should follow these rules:

- no runtime recon logging unless a developer explicitly enables a dev tool;
- no backup on normal launch;
- no backup merely because a save is loaded;
- no extra backup for a normal GK2 save;
- the first Moderate/High-risk GK2+ mutation after a load/save may create one checkpoint;
- later risky actions reuse that checkpoint until the game writes or loads a save;
- retain at most **5 backup directories per save slot**;
- backup copies are streamed from source to destination rather than loading the whole save into a managed byte array.

See [SAVE_SAFETY.md](SAVE_SAFETY.md).

## Garbage Collection

Avoid unnecessary transient allocations in frequently executed code.

Particular care should be taken with:

- LINQ in hot paths;
- per-frame interpolated/log strings;
- temporary lists/dictionaries;
- reflection result arrays;
- rebuilding UI labels/collections unnecessarily.

Allocations during initialization, page opening, or rare player actions are generally preferable to equivalent per-frame allocations.

## Logging

Use logs to diagnose lifecycle and failures, not to create a constant data stream.

- Info: meaningful lifecycle/state transitions.
- Warning: degraded behavior, blocked safety action, recoverable problems.
- Error: failed operation or unexpected exception.
- Debug: detailed developer diagnostics that are not needed during ordinary play.

Never log every frame in a production path.

## Performance Validation

Before a feature is considered release-ready, test at least:

1. Launch to main menu.
2. Load a save.
3. Travel through several zones.
4. Open/close GK2+ repeatedly.
5. Open/close relevant vanilla windows repeatedly.
6. Save normally.
7. Return to the main menu.
8. Load again.
9. Repeat the workflow several times.

Watch for:

- increasing RAM that does not settle across comparable cycles;
- accumulating duplicate GK2+ GameObjects/components;
- increasing event callback counts;
- unexpected disk writes;
- log spam;
- noticeable frame-time spikes when a feature is idle;
- UI work continuing while the menu is hidden.

Deeper profiling/diagnostic tooling may be added later, but feature code should follow these standards from the start.
