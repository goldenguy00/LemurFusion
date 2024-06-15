# Lemur Fusion

Makes your lemurians fuse together like dragonball!

![dbz](https://static1.srcdn.com/wordpress/wp-content/uploads/2017/10/DBZ-Fusion-Goku-and-Piccolo-Featured.jpg?q=50&fit=contain&w=1140&h=&dpr=1.5)


---
# Found Bug or Have Suggestions?

- Contact me at

    >https://ko-fi.com/score_

    >https://discord.com/channels/1214643706490257549/1219963478765207622

    >https://github.com/goldenguy00/LemurFusion/issues

---
## Features

- Set the number of lemur buddies you want and trying to summon any more than that will juice up the existing fellows.

- Supports any modded elites!

- Fully supports RiskyMod Ally changes!

- Designed to be a full scale rework but still compatible with all other mods

- Lemurians will switch between the various high tier elite types once they are fully evolved.

- Fully functional and configurable scoreboard entry for all the lemurs you control. 

- Lemurians will drop their tributed items on death and will remove their contribution to the shared inventory on death.

- Lemurians can use the vanilla shared inventory or have an inventory entirely to themselves.

- Stat multipliers per fusion, similar to dronemeld, while still retaining the vanilla HP and damage buffs that they would get on evolution.

- ... And much, much more! Nearly all features are configurable.


### *(WORK IN PROGRESS, SOME FUTURE PLANS COMING SOON)*

- Multiple elite types on a single guy.

- Resize Lemur buddies based on fusion count

---
## Config Options

- !!! Subject to Change, this list may not be entirely accurate.

- Total number of lemur buddies you can have

- Disable fall damage

- Teleport to owner distance

- Minion scoreboard

- Improve AI

- Stat Increase Per Fusion

- Revert to Egg On Death

- Items to Drop on Death

- Tributed Item Blacklist

---
# Special Thanks to:

- ThinkInvis for the Dronemeld foundation that this was based on.

    >https://github.com/ThinkInvis/RoR2-Dronemeld

- Kking117 for the awesome devotion config ideas and framework

    >https://thunderstore.io/package/kking117/DevotionConfig/

- Moffein for the good reference materials for working with EliteDefs and Ally support features from RiskyMod

    >https://github.com/Moffein/RiskyMod
    
    >https://github.com/Moffein/Risky_Artifacts

- Nuxlar for the scoreboard
    >https://github.com/FocusedFault

---
## Changelog

# 1.1.1

- Big AI changes, allies will avoid aoe damage zones and damage immunities work properly now. Disable "Improve AI" if this breaks...

- Health scaling changes, makes summoning new lemurians on later stages more viable. Configurable.

- Config restructuring, deleting config shouldn't be necessary but I'm not entirely certain.

- Experimental revive changes, summon clone when giving revival items. Disabled by default.

- Further attempts at making item transformations work have been made but it's still not perfect.

# 1.1.0

- Propersave support! Return to your lemur buddies at any time :)

- Config option to disable shared inventory, and instead use a unique inventory for every lemur friend

- Scoreboard features, can group all minions into one entry or split them up, displaying their contribution to the shared inventory

- Displays the correct lemur name on the scoreboard.

- Risky Compat temporarily removed, will be readded in a future patch :(

# 1.0.9

- ACTUALLY FIXED IT THIS TIME >:(

# 1.0.8

- Minor Config tweaks

- Additional safeguards against soft dependency errors

# 1.0.7

- Made debug logs optional

- Fixed non-functional on death mechanics

- Blacklist setting for sprint related items (lemurs can't sprint sorry)

- Added Mini Elder Lemurian config option back

- Included regen changes (not configurable yet, scales with max hp)

- Added config option for rerolling max evolution elite types

- Improved blacklist enable/disable functionality

- Fixed issue with item propagation during evolution

# 1.0.6

- Update manifest.json

# 1.0.5

- MAJOR SYSTEM REWORK

- Things shouldn't be broken, but hang in there if they are! This is under active development.

- Created better config options

- Item blacklist

- On Death mechanics

- Integrated many changes from https://thunderstore.io/package/kking117/DevotionConfig/

- Fixed compat with https://thunderstore.io/package/Risky_Lives/RiskyMod/

- Fixed compat with https://thunderstore.io/package/bouncyshield/LemurianNames/

# 1.0.2

- Fixed LemurianNames incompat for real this time

- Removed some debug logs

- Disabled size scaling until I make it work properly

# 1.0.1

- Fixed LemurianNames incompat

# 1.0.0

- Ported DronemeldDevotionFix code, stripped the dronemeld dependency and made the mod good