# GK2+ Release Checklist

## Feature PR Approval Gate

Use this before approving every player-facing feature PR.

### 1. Does it work?

- [ ] Branch is current with `main`.
- [ ] PR contains only the intended feature/fix and required support changes.
- [ ] Project builds successfully.
- [ ] Game launches with the branch DLL and no new unexpected errors.
- [ ] Primary feature behavior is tested in-game.
- [ ] Relevant edge cases are tested.
- [ ] Save/reload behavior is tested when persistence is involved.
- [ ] Enable/disable behavior is tested when applicable.
- [ ] Main-menu controls and in-game status are tested when applicable.
- [ ] Compatibility/overlap behavior is tested when applicable.

### 2. Is feature documentation current?

- [ ] `docs/FEATURES.md` documents the player-facing behavior.
- [ ] `docs/ROADMAP.md` is updated if the feature changed roadmap status/scope.
- [ ] Feature-specific docs are current.
- [ ] README navigation/high-level copy is still accurate.
- [ ] Screenshots are captured when useful for visible UI changes.

### 3. Maintainer-owned integration bookkeeping

Before merge, the maintainer:

- [ ] reviews/updates `CHANGELOG.md` under `[Unreleased]`;
- [ ] decides whether any release/public metadata needs a corresponding update;
- [ ] confirms no contributor-owned version/release metadata drift was introduced;
- [ ] performs final PR approval.

Only the maintainer merges official feature PRs.

---

## Every Release — Core Quality Gates

### Branch / Pull Request Hygiene

Before release preparation begins, inventory every open PR and active development branch and classify it as:

- **This release** — must be merged to `main`, explicitly removed from the release, or blocked with a documented reason before publishing.
- **Next release / future work** — may remain open, but its target milestone/intent must be clear and it must not be accidentally included in the current release.
- **Obsolete / superseded** — close the PR and delete the branch once any needed commits have been preserved elsewhere.

Checklist:

- [ ] Review all open pull requests.
- [ ] Confirm every PR intended for this release is already merged to `main` or is explicitly ready for final maintainer approval/merge.
- [ ] Confirm no current-release feature exists only on an unmerged branch.
- [ ] Confirm future-release PRs/branches are intentionally deferred and clearly identifiable as future work.
- [ ] Close superseded/abandoned PRs after preserving any commits that are still needed.
- [ ] Delete merged/superseded remote branches that are no longer needed.
- [ ] Remove temporary integration/test branches after their work has landed in the canonical branch.
- [ ] Confirm no duplicate branches contain divergent copies of the same feature that could cause future confusion.
- [ ] Re-check the branch list after cleanup and confirm only `main` plus intentional active/future work remains.
- [ ] Do not delete a branch whose commits are not safely merged, cherry-picked, tagged, or otherwise preserved.

After the release is published:

- [ ] Delete the completed release-preparation branch after merge.
- [ ] Close any release-specific tracking PRs/issues that are finished.
- [ ] Confirm branches intentionally kept for the next release are still current enough to rebase/sync cleanly later.

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
- [ ] Build the Steam Workshop staging package from that same canonical release ZIP with `tools/release/Build-SteamWorkshopPackage.ps1`.
- [ ] Publish/update the Steam Workshop item locally with `tools/release/Publish-SteamWorkshop.ps1` and record/verify the Workshop item URL.
- [ ] Confirm `docs/FEATURES.md` reflects the final released feature set.
- [ ] Confirm the maintainer has finalized `CHANGELOG.md`, release notes, and public release metadata.
- [ ] Confirm `docs/ROADMAP.md` reflects features that shipped, moved, or were dropped.
- [ ] Confirm `README.md`, `docs/README.md`, `AGENTS.md`, and `CONTRIBUTING.md` still point contributors to the current documentation.
- [ ] Confirm any public page that cannot be fully automated has an explicit manual update step.

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

## v2.0.0 — Native UI / Tracker / Journal Release

### Static release state

- [x] `VERSION` is `2.0.0`.
- [x] `GAME_VERSION` is `1.008`.
- [x] Feature Catalog documents the 2.0 feature set.
- [x] UI framework/menu visual contract is documented.
- [x] Configurable keyboard menu access is implemented.
- [x] Controller / Steam Deck L3 + R3 menu access is implemented.
- [x] RPG Quest Journal uses the shared native-style UI framework.
- [x] Quest Journal includes quest-only **Unpin All** integration with Unified Tracker.

### One-pass runtime validation

Run these in one play session where practical instead of treating every line as a separate test.

#### Menu / input / native UI

- [ ] Build Debug and Release with **0 errors / 0 warnings**.
- [ ] Main menu: open/close GK2+ with the configured keyboard hotkey.
- [ ] Change the hotkey in **General**, confirm the header hint updates immediately, old key stops toggling, and new key works.
- [ ] Restart GK2 and confirm the selected hotkey persists.
- [ ] With controller shortcut enabled, hold **L3 + R3** for about half a second and confirm the menu toggles only once per hold.
- [ ] Confirm **B** closes the top-most GK2+ picker/menu surface and **Esc** still behaves identically.
- [ ] Confirm controller opening gives navigation focus to the active top tab; exercise D-pad/stick navigation through representative controls.
- [ ] Disable the controller shortcut and confirm L3 + R3 / B no longer control GK2+.
- [ ] Sweep every F2 tab for clipping, overlap, stale old-font text, or non-native controls.
- [ ] Expand/collapse parent settings with child options and confirm one shared dark panel contains the whole group.
- [ ] Confirm collapsing a group does not change its saved feature/child values.
- [ ] Load a save and confirm main-menu-only settings are read-only while live-safe controls remain editable.

#### RPG Quest Journal / Unified Tracker

- [ ] Open Character -> Quests and confirm the RPG Journal replaces only the native quest-tree presentation.
- [ ] Switch repeatedly Character -> Quests -> Map -> Quests and confirm no duplicate journal trees or stale native quest tree appears.
- [ ] Test **Active** and **Completed** filters and their counts.
- [ ] Expand/collapse several NPC groups; verify all child quests remain inside one shared group panel.
- [ ] Select several quests and verify title, portrait, status, description, and item objectives.
- [ ] Test a quest with no item objective and a quest with multiple item objectives.
- [ ] Track/untrack individual active quests from both the quest row pin and the detail action.
- [ ] Pin several quests, use **Unpin All**, and confirm only quest pins are removed; craft/item/plan pins remain.
- [ ] Confirm the compact HUD remains bounded with several tracked entries and does not grow off-screen.
- [ ] Disable RPG Quest Journal and confirm the vanilla quest tree returns cleanly.
- [ ] Re-enable it and confirm the journal remounts without duplicate UI.

#### Sprinting

- [ ] Enable Sprinting and verify configured key + multiplier are shown in the Movement tab.
- [ ] Hold the sprint key during normal free movement and verify the speed increase.
- [ ] Release the key and verify native speed returns immediately.
- [ ] Change sprint key and multiplier, reload, and verify values persist.
- [ ] Sanity-check a non-free movement state so sprint does not obviously leak into scripted movement.

#### Continuous Planting

- [ ] Enable Continuous Planting.
- [ ] Plant a seed and confirm the same seed remains selected while more remain in inventory.
- [ ] Use the normal cancel/menu/tab action and confirm GK2+ does not restore the canceled seed selection.
- [ ] Use the last seed and confirm planting selection ends normally.
- [ ] Disable the feature and confirm vanilla one-shot planting behavior returns.

#### Backwards Compatible Extensions

- [ ] Enable the feature with a compatible workstation + **Fine Tool Rack** attached.
- [ ] Confirm a recipe requiring basic **Tool Rack** becomes craftable through the Fine Tool Rack.
- [ ] Confirm a recipe that genuinely requires Fine Tool Rack still requires it normally.
- [ ] Disable the feature and confirm the basic Tool Rack requirement returns to vanilla behavior.
- [ ] Open an unrelated workstation/extension combination and confirm it is unaffected.

#### Existing 0.1.x regression smoke

- [ ] Manual Save still saves/reloads the same location and representative world/inventory state.
- [ ] Shared Storage Current Zone / Global access still works.
- [ ] Bigger Item Stacks still applies the configured multiplier.
- [ ] Spawn Item still inserts a normal inventory item through the native path.
- [ ] Damage the player, use **Heal Player**, and confirm HP actually restores.
- [ ] Use **Refill Energy** and confirm the visible work-energy resource restores.
- [ ] Trigger one cheat flow and confirm warning/taint/checkpoint behavior is unchanged.
- [ ] When practical, observe a naturally triggered platform achievement attempt being blocked on a tainted save.

### Final packaging / publish

- [ ] Run `tools/release/Build-ReleasePackage.ps1` from a clean tree.
- [ ] Install and test the **Release** DLL from the generated archive, not only the Debug build.
- [ ] Record the canonical ZIP SHA256.
- [ ] Build the Thunderstore package from the same source/version and validate its manifest/icon/layout.
- [ ] Build the Steam Workshop staging package from the canonical release ZIP.
- [ ] Verify Workshop/Thunderstore/Nexus copy mentions the current 2.0 feature set and configurable/controller menu access.
- [ ] Capture final screenshots of the F2 menu, Quest Journal, Unified Tracker HUD, and one representative settings group.
- [ ] Merge the 2.0 release branch to `main`, tag `v2.0.0`, and publish the same canonical artifact to the intended channels.

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
