# 1.6.3

- I promise i fixed collision for real this time
- Default is back to true and cfg option moved to General
- Fixed error with propersave when loading malformed data on stage start

# 1.6.1

- Fixed ally attack and body collision cfg options
- Changed default to false

# 1.6.0

- Removed prediction. Download https://thunderstore.io/package/score/AdvancedPrediction/ instead
- Added config option to disable Team attack and body collision (player team only) since they love getting in the way.
- Modified inventory management system to use the native RoR2 inventory more. This should fix scoreboard issues.
- Bug fixes and refinement of the personal/shared inventory systems.
- Rewrote some of the hitbox math for better dodging and readded visualizer
- various bits of cleanup, i know the readme needs updating but ill get around to it later

# 1.5.2

- Fixed some nullref issues when spawning clones

# 1.5.1

- Fixed possible dependency issues

# 1.5.0

- SotS Update

- Permanant devotion config option. Allows eggs to spawn without the artifact, along with a percent chance to replace a normal drone spawn.

- Variant API support (unsure if this works with SotS though...)

- Full backend rework. This is an absolutely monstrous update but i foolishly did not document things.

- Some features may not be working correctly, please report bugs to the github issues page.

# 1.3.1

- Dodge AI fleshed out for use against the majority of projectiles, integrated seamlessly into the combat loop.

- Refined the Mithrix and Umbral Mithrix fights. End goal is for them to reliably dodge everything. Yes, everything. No, not exaggerating.

- Additional tweaks to the base AI so that they don't fist fight beetle guards and inevitably die...

- Adjustable detection range and update rate. Removed in/out of combat distinction since performance isn't a concern.

- More tweaks to the stat scaling. I'm bad at math apparently.

- Further separation of the lemurian/adult lemurian prefabs from the Devoted variants. This disables compatibility with Variance API but that'll be reimplemented.

- Probably some stuff I'm forgetting but the AI has nearly peaked. They're cracked.

# 1.2.2

- Implemented changes from https://thunderstore.io/package/Goorakh/LemurianFix/ cuz i love stealing

- More refinement to AI and some small optimizations to the dodging routine.

- Adjusted strafe to create more distance, added jump logic for some projectiles.

- Maintains the "in combat" update rate and widens scan radius when fighting mithrix (fast projectiles could sneak hits in)

- Added survivor projectile filter. Rarely useful but not worth getting rid of entirely.

# 1.2.1

- Made projectile filtering too aggressive, they will escape from dotzones and void death like intended.

# 1.2.0

- Refined AI changes. Features predictive aiming (thanks moff love u), projectile dodging and sprinting!

- Projectile dodging visualization is included but disabled by default.

- Included config options for AI tweaks, debug logs and reorganized config (final time I promise)

- !!! Delete your configs if you aren't seeing all 6 categories !!!

- Re-added RiskyMod ally tracker

- Works with Variants for the most part, currently Variants will be rerolled often. Will be permanent in future updates.

- Refined the Rebalanced Stat Scaling option to account for the new dodging mechanics.

- Some bug fixes

- Removed ChangeLog from ReadMe

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