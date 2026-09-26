# GK2+ Release Checklist

## Every Release — Core Quality Gates

### Repository / Packaging

- [ ] Confirm `VERSION` matches the intended release.
- [ ] Confirm `GAME_VERSION` matches the Graveyard Keeper 2 version used for final runtime validation.
- [ ] Confirm experimental/local-only source is not present.
- [ ] Confirm no `*.bak`, `bin/`, `obj/`, `local.props`, private recon output, game DLLs, extracted game assets, or local save data are staged.
- [ ] Run `git status` and inspect every changed/untracked file.
- [ ] Build the release package from a clean tree with `tools/release/Build-ReleasePackage.ps1`.
- [ ] Confirm only intended GK2+ files are included.
- [ ] Record the release ZIP SHA256 printed by the packaging script.
- [ ] Use the same version/build artifact for GitHub Releases and Nexus Mods.

### Launch / UI Lifecycle

- [ ] Launch Graveyard Keeper 2 with the newly built DLL.
- [ ] Confirm BepInEx loads GK2+ without unexpected errors.
- [ ] Confirm the main-menu GK2+ badge shows the intended version.
- [ ] Confirm the F2 menu reports the running GK2 version as `[tested]`.
- [ ] Temporarily test a mismatched `GAME_VERSION` value and confirm GK2+ warns but still loads.
- [ ] Confirm F2 opens/closes GK2+ from the main menu.
- [ ] Load a save and confirm F2 opens/closes GK2+ during active gameplay.
- [ ] Confirm Esc and the Close button work.
- [ ] Open representative vanilla windows and confirm GK2+ renders above them while open.
- [ ] Return to the main menu and confirm F2 still works.
- [ ] Confirm there is only one persistent GK2+ menu/controller instance.

### Manual Save

- [ ] Confirm the pause menu contains exactly one **Save Game** button.
- [ ] Confirm keyboard/mouse can activate Save Game.
- [ ] Confirm controller/D-pad navigation includes Save Game.
- [ ] Confirm the native saving indicator appears.
- [ ] Confirm the day/time does not advance merely because Manual Save was used.
- [ ] Save in a recognizable location, quit without sleeping, reload, and confirm the location persists.
- [ ] Confirm representative inventory/money/world-object state persists.
- [ ] Confirm Exit to Main Menu shows a correct last-save age/time.
- [ ] Confirm the last-save status updates after Manual Save.

### Save Safety / Disk I/O

- [ ] Confirm a normal launch creates no GK2+ save backup.
- [ ] Confirm loading a save creates no GK2+ save backup.
- [ ] Confirm a normal GK2 save creates no extra GK2+ save backup.
- [ ] Confirm the first Moderate/High-risk test mutation creates one checkpoint.
- [ ] Confirm repeated risky actions before another load/save reuse that checkpoint.
- [ ] Confirm the next risky action after a native save/load creates a new checkpoint.
- [ ] Confirm backup retention never exceeds five directories per save slot.
- [ ] Confirm a failed required backup blocks the protected mutation.
- [ ] Confirm backup creation does not buffer the entire save into managed memory.
- [ ] Confirm no automated restore overwrites the live save.

### Cheats / Achievement Integrity

- [ ] Confirm Cheats actions are unavailable without a loaded gameplay save.
- [ ] Confirm the first cheat opens the native permanent-achievement warning.
- [ ] Confirm **Cancel** performs no cheat mutation and does not taint the save.
- [ ] Confirm **Enable Cheats** writes the active `<slot>.gk2plus-cheat-taint` marker before the cheat executes.
- [ ] Confirm the Cheats tab changes to the tainted-save warning.
- [ ] Confirm closing/reopening the game preserves tainted status and does not show the first-cheat dialog again.
- [ ] Confirm all retained GK2+ backups for the slot contain `GK2Plus-CheatTaint.txt`.
- [ ] Confirm a new backup created after tainting also contains the marker.
- [ ] Confirm cheat actions fail closed if the achievement guard cannot initialize.
- [ ] When practical, confirm a real achievement platform call is blocked while a tainted save is active.

### Performance / Resource Behavior

- [ ] Test repeated main-menu -> gameplay -> main-menu cycles.
- [ ] Open/close GK2+ repeatedly and verify no duplicate UI trees/controllers accumulate.
- [ ] Travel through multiple zones and open representative vanilla windows.
- [ ] Watch for RAM that continually grows across comparable cycles and does not settle after GC.
- [ ] Confirm hidden GK2+ UI is idle aside from lightweight hotkey/state handling.
- [ ] Confirm no heavy Unity resource scans/reflection/collection walks occur every frame.
- [ ] Confirm no production log spam or continuous recon writes.
- [ ] Confirm new Harmony patches are narrow and cheap in their execution path.
- [ ] Note any measurable CPU/GPU/disk impact introduced by the release.

See [PERFORMANCE.md](PERFORMANCE.md) and [SAVE_SAFETY.md](SAVE_SAFETY.md).

---

## v0.1.0 — First Gameplay Release

### Functional validation

- [x] Persistent F2 menu works in main menu and gameplay.
- [x] Manual Save works from the pause menu.
- [x] Manual Save reloads at the same player location without sleeping.
- [x] Moved world-object state persisted across manual save/reload.
- [x] Last-save status updates from native save metadata.
- [x] Pause-menu Save Game controller navigation works.
- [x] Silver/Gold cheat increments work and trigger native money feedback.
- [x] Refill Energy restores the visible work/action energy bar.
- [x] First money mutation creates one checkpoint.
- [x] Repeated money actions reuse that checkpoint.
- [x] Active cheat-taint sidecar persists after restart.
- [x] All five retained test backups received cheat-taint markers.
- [ ] Heal Player tested while actual HP is below maximum.
- [ ] Naturally triggered platform achievement attempt observed being blocked on a tainted save.

### Release preparation

- [ ] Confirm `VERSION` is `0.1.0`.
- [ ] Confirm `GAME_VERSION` is `1.006`.
- [ ] Confirm `CHANGELOG.md` contains the dated `0.1.0` section.
- [ ] Confirm README describes current functional features instead of the foundation preview.
- [ ] Confirm `docs/NEXUS_PAGE.md` reflects v0.1.0.
- [ ] Run:

~~~powershell
.\tools\release\Build-ReleasePackage.ps1
~~~

- [ ] Install/test the DLL produced by the release build.
- [ ] Confirm the final archive is named `GK2Plus-0.1.0.zip`.
- [ ] Capture final screenshots:
  - [ ] Cheats tab
  - [ ] pause-menu Save Game button
  - [ ] Exit confirmation / Last saved status
  - [ ] optional first-cheat warning
- [ ] Merge the release-preparation PR.
- [ ] From updated `main`, publish the GitHub tag/release/archive with:

~~~powershell
.\tools\release\Publish-GitHubRelease.ps1
~~~

- [ ] Confirm GitHub Release **GK2+ v0.1.0 — First Gameplay Release** exists and contains `GK2Plus-0.1.0.zip`.
- [ ] Upload the exact same ZIP to Nexus Mods.
- [ ] Use `docs/NEXUS_PAGE.md` for the restored Nexus description/file copy.
- [ ] Add the final Nexus URL to `ProjectLinks.NexusUrl` in a follow-up release if the URL is not known before v0.1.0 ships.

### Thunderstore / R2ModMan

- [x] Confirm the **Graveyard Keeper 2** Thunderstore community is publicly available.
- [x] Confirm the BepInEx dependency string: `BepInEx-BepInExPack-5.4.2305`.
- [x] Add Thunderstore README, generated manifest, generated 256x256 icon, and BepInEx plugin layout.
- [ ] Create/select the permanent Thunderstore Team that will own GK2+.
- [ ] Build the Thunderstore package:

~~~powershell
.\tools\release\Build-ThunderstorePackage.ps1
~~~

- [ ] Confirm output is `dist/GK2Plus-0.1.0-Thunderstore.zip`.
- [ ] Validate the package with Thunderstore's manifest/package validator.
- [ ] Upload under community **Graveyard Keeper 2**, category **Mods**, NSFW **No**.
- [ ] Confirm the listing declares `BepInEx-BepInExPack-5.4.2305`.
- [ ] Install through Thunderstore Mod Manager or R2ModMan into a clean profile.
- [ ] Confirm the installed DLL/version matches the GitHub/Nexus v0.1.0 build.
- [ ] Confirm F2, Manual Save, and Cheats load from the mod-manager profile.

---

## v0.0.1 — Historical Foundation Release

- Initial framework/menu preview.
- Retained here only as release history; use the current checklist for new releases.
