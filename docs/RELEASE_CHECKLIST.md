# GK2+ Release Checklist

## Every Release — Core Quality Gates

### Repository / Packaging

- [ ] Confirm VERSION matches the intended release.
- [ ] Confirm experimental/local-only source is not present.
- [ ] Confirm no *.bak, bin/, obj/, local.props, private recon output, game DLLs, extracted game assets, or local save data are staged.
- [ ] Run git status and inspect every changed/untracked file.
- [ ] Build the release package from a clean tree.
- [ ] Confirm only intended GK2+ files are included.

### Launch / UI Lifecycle

- [ ] Launch Graveyard Keeper 2 with the newly built DLL.
- [ ] Confirm BepInEx loads GK2+ without unexpected errors.
- [ ] Confirm the main-menu GK2+ badge appears.
- [ ] Confirm F2 opens/closes GK2+ from the main menu.
- [ ] Load a save and confirm F2 opens/closes GK2+ during active gameplay.
- [ ] Confirm Esc and the Close button work.
- [ ] Open representative vanilla windows and confirm GK2+ renders above them while open.
- [ ] Return to the main menu and confirm F2 still works.
- [ ] Confirm there is only one persistent GK2+ menu/controller instance.

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

## v0.0.1 — First Nexus Release

Historical checklist for the initial foundation package:

- [ ] Confirm VERSION is 0.0.1.
- [ ] Run tools/release/Build-NexusPackage.ps1.
- [ ] Confirm BepInEx loads GK2+ 0.0.1 without unexpected errors.
- [ ] Confirm the main-menu GK2+ badge appears.
- [ ] Confirm F2 opens the menu from the main menu.
- [ ] Confirm Esc and Close work.
- [ ] Confirm GitHub and Report Bug buttons work.
- [ ] Confirm the Nexus button is disabled and labelled Nexus Soon.
- [ ] Upload dist/GK2Plus-0.0.1.zip as the functioning Nexus file.
- [ ] Use the copy in docs/NEXUS_PAGE.md for the page and file description.
- [ ] Publish the page only after the functioning file is attached.
- [ ] Record the final Nexus Mods URL.

---

## v0.0.2 — Official Nexus Link

- [ ] Set ProjectLinks.NexusUrl in src/GK2Plus/Core/ProjectLinks.cs to the official Nexus page URL.
- [ ] Change VERSION to 0.0.2.
- [ ] Add a 0.0.2 release section to CHANGELOG.md noting the Nexus-link change and any other included work.
- [ ] Build with tools/release/Build-NexusPackage.ps1.
- [ ] Run the Every Release quality gates above.
- [ ] Launch and confirm the More-tab button says Nexus Mods and opens the correct page.
- [ ] Confirm GitHub and Report Bug links still work.
- [ ] Upload dist/GK2Plus-0.0.2.zip to Nexus.
- [ ] Update the Nexus file description using the v0.0.2 template.
- [ ] Commit, tag v0.0.2, and push.
