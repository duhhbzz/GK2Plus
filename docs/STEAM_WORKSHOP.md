# GK2+ Steam Workshop

GK2+ targets the Graveyard Keeper 2 Steam Workshop in addition to GitHub, Nexus Mods, and Thunderstore.

Graveyard Keeper 2 uses Steam App ID 4358690.

**Live GK2+ Workshop item:**  
https://steamcommunity.com/sharedfiles/filedetails/?id=3808769342

**PublishedFileId:** `3808769342`

Steam publishing stays local because SteamCMD can require the maintainer's password and Steam Guard challenge. Credentials must not be stored in the repository or CI.

## Build

Use the canonical release ZIP, then prepare the Workshop content and VDF:

~~~powershell
.\tools\release\Build-SteamWorkshopPackage.ps1
~~~

The local output is under dist\steam-workshop and is ignored by Git.

The Workshop content contains the same GK2Plus.dll from the canonical release ZIP at:

~~~text
BepInEx\plugins\GK2Plus\GK2Plus.dll
~~~

It also contains README.md, CHANGELOG.md, LICENSE, and WORKSHOP_INSTALL.txt.

The Workshop package does not bundle BepInEx itself.

## Publish

Run:

~~~powershell
.\tools\release\Publish-SteamWorkshop.ps1
~~~

The script locates SteamCMD, or downloads Valve's official SteamCMD package if it is not installed. It prompts for the Steam login name when needed and leaves password/Steam Guard authentication to SteamCMD.

The first upload uses publishedfileid 0. SteamCMD writes the new Workshop item ID back into the generated VDF. GK2+ then stores that ID locally in dist\steam-workshop\item-id.txt so later uploads update the same item.

An explicit login can be supplied without storing a password:

~~~powershell
.\tools\release\Publish-SteamWorkshop.ps1 -SteamUser YOUR_STEAM_LOGIN
~~~

## Subscriber installation

Steam Workshop downloads subscribed content under a path like:

~~~text
Steam\steamapps\workshop\content\4358690\<WorkshopItemId>
~~~

GK2+ requires BepInEx 5.4.23.5. Existing GK2 BepInEx Workshop mods currently instruct users to copy/merge the included BepInEx folder into the Graveyard Keeper 2 game directory. GK2+ follows that same distribution layout until an automatic Workshop-loader path is separately validated.

## Verification

After the first upload:

- open the item URL printed by the script;
- accept any Steam Workshop legal agreement;
- verify the title, description, preview, visibility, and change note;
- subscribe and confirm Steam downloads the item;
- confirm the content contains BepInEx\plugins\GK2Plus\GK2Plus.dll;
- install/merge it into a clean BepInEx setup;
- launch GK2 and verify the GK2+ version and F2 menu;
- confirm the published item remains available at https://steamcommunity.com/sharedfiles/filedetails/?id=3808769342.
