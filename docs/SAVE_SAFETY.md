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

Typical examples:

| Risk | Intended examples | Automatic checkpoint |
| --- | --- | --- |
| Low | heal, refill energy/stamina, temporary/session-only effects | No |
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
Give Item                 -> reuse checkpoint
Add Tech Points           -> reuse checkpoint
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
                        └── GK2Plus-Backup.txt
~~~

Example slot:

~~~text
BepInEx/config/GK2Plus/SaveBackups/Steam_1/
~~~

A configurable backup path may be added later.

## Retention

GK2+ currently retains at most **5 backup directories per save slot**.

After a successful new backup, older entries beyond the retention limit are removed on a best-effort basis.

The newly-created backup is never intentionally deleted by that prune operation.

## Disk and Memory Behavior

The save files are copied with file streams. GK2+ does not intentionally load the entire save file into a managed byte array just to back it up.

Normal activity produces no GK2+ safety backup writes:

- launch: none;
- normal gameplay: none;
- load: none;
- normal GK2 save: none.

Only a requested backup/checkpoint writes copies of the save files.

The backup verifier compares the copied file length with the source length. It intentionally does not perform a second full-file checksum pass, which would add another complete read of the backup solely for verification.

## Failure Behavior

For a Moderate/High-risk protected action:

- if there is no active loaded save, the action is blocked;
- if GK2 is currently loading/writing a save, the action is blocked;
- if the backup cannot be created, the action is blocked;
- if the mutation throws or validation fails, GK2+ logs the failure and the checkpoint path.

GK2+ does **not** automatically overwrite the live save with a backup. Automated rollback can make a partially-understood failure worse, so recovery remains explicit/user-controlled until a tested restore workflow is implemented.

## Scope

The save-safety service is infrastructure. By itself it does not grant money, items, perks, quest state, or any other gameplay change.

Features that make persistent mutations should call the service rather than independently editing/copying save files.
