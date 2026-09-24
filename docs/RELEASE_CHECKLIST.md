# GK2+ Release Checklist

## v0.0.1 — First Nexus Release

- [ ] Confirm `VERSION` is `0.0.1`.
- [ ] Confirm experimental/local-only source is not present.
- [ ] Confirm no `*.bak`, `bin/`, `obj/`, `local.props`, private recon output, game DLLs, or extracted game assets are staged.
- [ ] Run `git status` and inspect every changed/untracked file.
- [ ] Run `tools/release/Build-NexusPackage.ps1`.
- [ ] Launch Graveyard Keeper 2 with the newly built DLL.
- [ ] Confirm BepInEx loads `GK2+ 0.0.1` without unexpected errors.
- [ ] Confirm the main-menu GK2+ badge appears.
- [ ] Confirm F2 opens the menu from the main menu.
- [ ] Confirm Esc and Close work.
- [ ] Confirm GitHub and Report Bug buttons work.
- [ ] Confirm the Nexus button is disabled and labelled `Nexus Soon`.
- [ ] Upload the generated `dist/GK2Plus-0.0.1.zip` as the functioning Nexus file.
- [ ] Use the copy in `docs/NEXUS_PAGE.md` for the page and file description.
- [ ] Publish the page only after the functioning file is attached.
- [ ] Record the final Nexus Mods URL.

## v0.0.2 — Official Nexus Link

- [ ] Set `ProjectLinks.NexusUrl` in `src/GK2Plus/Core/ProjectLinks.cs` to the official Nexus page URL.
- [ ] Change `VERSION` to `0.0.2`.
- [ ] Add a `0.0.2` release section to `CHANGELOG.md` noting the Nexus-link change.
- [ ] Build with `tools/release/Build-NexusPackage.ps1`.
- [ ] Launch and confirm the More-tab button says `Nexus Mods` and opens the correct page.
- [ ] Confirm GitHub and Report Bug links still work.
- [ ] Upload `dist/GK2Plus-0.0.2.zip` to Nexus.
- [ ] Update the Nexus file description using the v0.0.2 template.
- [ ] Commit, tag `v0.0.2`, and push.
