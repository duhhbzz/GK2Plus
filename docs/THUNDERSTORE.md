# GK2+ Thunderstore / R2ModMan Plan

GK2+ should publish to the Graveyard Keeper II Thunderstore community once that community is publicly available and its package/dependency identifiers are confirmed.

## Release Principle

Thunderstore must not use a separate gameplay build.

For a given GK2+ version:

~~~text
VERSION
  |
  +-- GitHub Release
  +-- Nexus Mods
  +-- Thunderstore / R2ModMan
~~~

All channels should ship the same compiled `GK2Plus.dll`.

Only the distribution wrapper/metadata should differ.

## Expected Thunderstore Package Pieces

Once the community metadata is known, add a Thunderstore package containing the required items such as:

- `manifest.json`
- package icon
- Thunderstore README/description
- the same release `GK2Plus.dll`
- appropriate BepInEx dependency declaration
- package layout expected by the Graveyard Keeper II community

Do not guess the final community/dependency IDs before the community is live.

## Validation

Before publishing:

1. Install through R2ModMan/Thunderstore Mod Manager into a clean profile.
2. Confirm BepInEx loads GK2+.
3. Confirm the version badge matches the GitHub/Nexus release.
4. Confirm F2 works in main menu and gameplay.
5. Confirm Manual Save works.
6. Confirm Cheats actions load correctly.
7. Confirm updating the package does not create duplicate GK2+ DLLs.
8. Confirm uninstall removes the managed package files without deleting player save metadata/backups.

## Version Synchronization

Do not create Thunderstore-only version numbers.

If a packaging-only problem needs a fix and Thunderstore requires a new package version, prefer making that a real GK2+ patch release so all public channels remain understandable and traceable.

## Pending

The exact `manifest.json` dependency/community fields will be added after the Graveyard Keeper II Thunderstore community is available and verified.
