# GK2+ Save Safety

GK2+ includes a save-safety layer for features that intentionally modify persistent player, progression, economy, or world state.

The safety layer is designed to protect the player without turning ordinary gameplay into a continuous backup workload.

## High-Level Flow

~~~text
Player uses a GK2+ action
        |
        v
Classify mutation risk
        |
        +-- Low ----------> run without an automatic backup
        |
        +-- Moderate/High
                |
                v
        Verify an active save is loaded
                |
                v
        Verify GK2 is not loading/writing
                |
                v
        Reuse current safety checkpoint?
             /        \
           yes         no
            |           |
            |      create one backup
            |           |
            +-----+-----+
                  |
                  v
           run the mutation
~~~

## Risk Levels

| Risk | Intended examples | Automatic checkpoint |
| --- | --- | --- |
| Low | heal, refill energy, temporary/session-only effects | No |
| Moderate | money, items, tech points, persistent resources | Yes |
| High | quest/progression/world-state changes | Yes |

The exact classification belongs to each feature implementation and may change as GK2 internals are validated.

## Checkpoint Model

GK2+ does **not** create a new backup every time a player clicks a cheat/action.

Instead:

1. A save is loaded or written by GK2.
2. No GK2+ safety checkpoint exists for that new save generation.
3. The first Moderate/High-risk GK2+ action creates one checkpoint.
4. Additional risky actions reuse that same checkpoint.
5. When GK2 loads or writes a save again, the checkpoint is invalidated.
6. The next risky action creates a new checkpoint.

Example:

~~~text
Load save                 -> 0 GK2+ backup writes
Give Money                -> create checkpoint
Give Money again          -> reuse checkpoint
Normal GK2 save           -> invalidate checkpoint
Give Money                -> create new checkpoint
~~~

This reduces unnecessary writes on SSDs and HDDs while preserving a known pre-mutation recovery point.

## Backup Location

By default:

~~~text
<Graveyard Keeper 2>/
└── BepInEx/
    └── config/
        └── GK2Plus/
            └── SaveBackups/
                └── <slot>/
                    └── <timestamp>_<reason>/
                        ├── <slot>.dat
                        ├── <slot>.info
                        ├── GK2Plus-Backup.txt
                        └── GK2Plus-CheatTaint.txt   (tainted slots only)
~~~

Example slot:

~~~text
BepInEx/config/GK2Plus/SaveBackups/Steam_1/
~~~

A configurable backup path may be added later.

## Retention

GK2+ currently retains at most **5 backup directories per save slot**.

After a successful new backup, older entries beyond the retention limit are removed on a best-effort basis.

The newly created backup is never intentionally deleted by that prune operation.

## Cheat Taint and Backup Lineage

Cheat use has a separate integrity rule from ordinary save safety.

Before the first cheat executes, GK2+ asks the player to confirm that the save will become permanently cheat-tainted.

After confirmation, GK2+ writes a sidecar next to the active native save:

~~~text
<slot>.gk2plus-cheat-taint
~~~

The sidecar records GK2+ metadata such as the first cheat identifier and the time the save became tainted.

GK2+ does **not** add fields to Graveyard Keeper 2's serialized save schema.

When a slot becomes tainted:

1. the active sidecar is written before the first cheat executes;
2. existing retained GK2+ backups for that slot receive `GK2Plus-CheatTaint.txt`;
3. future backups copy the taint marker automatically;
4. loading that save later still reports the slot as tainted;
5. GK2+ blocks platform achievement progress/unlock calls while that tainted slot is active.

This policy intentionally applies to the GK2+ backup lineage as well as the active save.

The system is an integrity feature, not anti-tamper DRM. A user who deliberately removes the mod or manipulates metadata can bypass a mod-level restriction.

## Disk and Memory Behavior

Save files are copied with file streams. GK2+ does not intentionally load the entire save into a managed byte array just to back it up.

Normal activity produces no GK2+ safety backup writes:

- launch: none;
- normal gameplay: none;
- load: none;
- normal GK2 save: none.

Only a requested checkpoint/backup writes copies of the save files.

The backup verifier compares copied file length with source length. It intentionally does not perform a second full-file checksum pass, which would add another complete read solely for verification.

Cheat-taint metadata is a small text sidecar and does not create repeated save-file copies by itself.

## Failure Behavior

For a Moderate/High-risk protected action:

- if there is no active loaded save, the action is blocked;
- if GK2 is currently loading/writing a save, the action is blocked;
- if the required backup cannot be created, the action is blocked;
- if the mutation throws or validation fails, GK2+ logs the failure and checkpoint path.

For cheat actions:

- if the platform achievement guard cannot initialize, cheat actions fail closed;
- if the active save cannot be marked tainted, the first cheat is not executed;
- cancelling the first-cheat confirmation does not taint or mutate the save.

GK2+ does **not** automatically overwrite the live save with a backup. Automated rollback can make a partially understood failure worse, so recovery remains explicit/user-controlled until a tested restore workflow is implemented.

## Current Runtime Validation

v0.1.0 validation includes:

- passive launch/load/save creates no GK2+ backup;
- first Moderate-risk money action creates one checkpoint;
- repeated money actions in the same save generation reuse that checkpoint;
- new native save/load invalidates the checkpoint;
- retention remains capped at five directories;
- active cheat-taint sidecar persists after close/reopen;
- all five retained test backups were successfully marked tainted;
- future backups inherit the taint marker.

## Scope

Features that make persistent mutations should call the shared service rather than independently editing/copying save files.

Save safety does not itself grant money, items, perks, quest state, or other gameplay changes.
