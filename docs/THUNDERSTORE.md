# GK2+ Thunderstore / R2ModMan

The **Graveyard Keeper 2** Thunderstore community is live and accepts package uploads.

GK2+ should publish the same gameplay build across GitHub, Nexus Mods, and Thunderstore/R2ModMan.

## Release Principle

For a given GK2+ version:

~~~text
VERSION
  |
  +-- GitHub Release ZIP
  +-- Nexus Mods ZIP
  +-- Thunderstore package ZIP
~~~

The wrapper/layout differs by channel, but all channels must ship the same compiled `GK2Plus.dll`.

The repository-root `GAME_VERSION` file records the Graveyard Keeper 2 version validated for the release. For v0.1.0, that is **1.006**.

## Community

- Community: **Graveyard Keeper 2**
- Section/category: **Mods**
- NSFW: **No**
- Package name: **GK2Plus**
- Website: https://github.com/duhhbzz/GK2Plus

The Thunderstore upload form requires a Team. Create/select the permanent Team you want to own the package before the first upload.

A later upload must use the same Team and package name to update the existing listing rather than create a separate package.

## BepInEx Dependency

The Graveyard Keeper 2 community exposes the pinned Thunderstore BepInExPack.

GK2+ v0.1.0 declares:

~~~text
BepInEx-BepInExPack-5.4.2305
~~~

That package contains BepInEx 5.4.23.5, matching the GK2+ development/runtime baseline.

## Package Layout

Build with:

~~~powershell
.\tools\release\Build-ThunderstorePackage.ps1
~~~

Output:

~~~text
dist/GK2Plus-<version>-Thunderstore.zip
~~~

ZIP layout:

~~~text
manifest.json
README.md
CHANGELOG.md
icon.png
BepInEx/
└── plugins/
    └── GK2Plus/
        └── GK2Plus.dll
~~~

Thunderstore requires `manifest.json`, `README.md`, and a 256x256 `icon.png` at the root of the archive.

The build script generates the icon from `assets/branding/gk2plus-logo.png` so the package branding stays synchronized with the repository.

## Manifest

The build script generates `manifest.json` from the repository `VERSION` file.

Current v0.1.0 values:

~~~json
{
  "name": "GK2Plus",
  "version_number": "0.1.0",
  "website_url": "https://github.com/duhhbzz/GK2Plus",
  "description": "Modular Graveyard Keeper 2 QoL suite with Manual Save, native-style cheats, save-safety checkpoints, and per-save achievement protection.",
  "dependencies": [
    "BepInEx-BepInExPack-5.4.2305"
  ]
}
~~~

Do not manually drift the Thunderstore version away from the root `VERSION` file.

## First Upload

1. Create/select the Thunderstore Team that should permanently own GK2+.
2. Run the Release build/package scripts.
3. Validate `dist/GK2Plus-0.1.0-Thunderstore.zip` with Thunderstore's manifest validator.
4. On **Upload package**, choose that ZIP.
5. Select the Team.
6. Select **Graveyard Keeper 2** under Communities.
7. Select **Mods** as the category.
8. Leave NSFW set to **No**.
9. Submit.
10. Open the package listing and verify the README/icon/dependency.
11. Test installation through Thunderstore Mod Manager or R2ModMan using a clean profile.

A new package may take some time to appear in search/mod-manager caches even after the direct listing is available.

## Validation

Before treating a Thunderstore version as released:

1. Install into a clean mod-manager profile.
2. Confirm the declared BepInEx dependency installs.
3. Confirm `GK2Plus.dll` lands under the profile's BepInEx plugins path.
4. Confirm the main-menu badge reports the intended GK2+ version.
5. Confirm F2 works in main menu and gameplay.
6. Confirm Manual Save works.
7. Confirm Cheats actions load correctly.
8. Confirm updating the package does not create duplicate GK2+ DLLs.
9. Confirm uninstall removes managed package files without deleting player save metadata/backups.

## Version Synchronization

Do not create Thunderstore-only gameplay versions.

If packaging itself requires a public fix after upload, prefer a normal GK2+ patch release so GitHub, Nexus, and Thunderstore remain traceable to the same source/version.
