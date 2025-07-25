﻿using LemurFusion.Devotion;
using System.Runtime.CompilerServices;

namespace LemurFusion.Config
{
    // > "99% of this is practically ripped off from RiskyMod, ain't gonna lie."
    //and now it's been ported entirely into LemurFusion. Common Moffein W, once again.
    internal static class ConfigReader
	{
        private const string GENERAL = "01 - General";
        private const string EXPERIMENTAL = "02 - Experimental";
        private const string STATS = "03 - Fusion Stats";
        private const string AI_CONFIG = "04 - AI Changes";
        private const string DEATH = "05 - Death Settings";
		private const string BLACKLIST = "06 - Item Blacklist";

        private const string Desc_Enable = "Enables changes for this section.";

        internal static void Init()
        {
            ReadConfig();
            ReadAIConfig();
            ReadConfigExtended();
            RoR2.RoR2Application.onLoad += ConfigExtended.PostLoad;
        }


        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static void ReadConfig()
        {
            if (LemurFusionPlugin.rooInstalled)
            {
                PluginConfig.InitRoO();
            }

            //
            // general
            //
            PluginConfig.maxLemurs = PluginConfig.BindOptionSlider(GENERAL,
                "Max Lemurs",
                3,
                "Max dudes",
                1, 20);

            PluginConfig.teleportDistance = PluginConfig.BindOptionSlider(GENERAL,
                "Teleport Distance",
                100,
                "Sets the max distance a Lemurian can be from their owner before teleporting.",
                50, 400);

            PluginConfig.enableMinionScoreboard = PluginConfig.BindOption(GENERAL,
                "Enable Minion Scoreboard",
                true,
                "Devoted Lemurians will show up on the scoreboard.");

            PluginConfig.showPersonalInventory = PluginConfig.BindOption(GENERAL,
                "Individual Scoreboard Inventories",
                true,
                "Enable to display a scoreboard entry for each lemurian you control. Scoreboard entries will show the items that each minion contributes to the shared inventory.\r\n" +
                "Purely visual, does not change vanilla item sharing mechanics.");

            PluginConfig.highTierElitesOnly = PluginConfig.BindOption(GENERAL,
                "High Tier Elites Only For Final Evolution",
                true,
                "When rerolling the fully evolved elite elder lemurian aspects, should it always be a lategame elite type?");

            PluginConfig.permaDevotion = PluginConfig.BindOption(GENERAL,
                "Permanent Devotion",
                false,
                "When enabled, the Devotion artifact will no longer have any effect. Instead, the egg spawnrate will be decided by \"Egg Spawn Chance\"." +
                " Disable this option if you would like to be able to toggle the artifact's effects like normal.",
                true);

            PluginConfig.eggSpawnChance = PluginConfig.BindOptionSlider(GENERAL,
                "Egg Spawn Chance",
                50,
                "Sets the chance of replacing a drone spawn with an egg.",
                0, 100);

            PluginConfig.disableTeamCollision = PluginConfig.BindOption(GENERAL,
                "Disable Team Attack Collision",
                true,
                "Lightweight filter to allow all teammate bullets and projectiles to pass through allies. Also allows you to walk through lemurians.",
                true);

            //
            // misc
            //
            PluginConfig.enableSharedInventory = PluginConfig.BindOption(EXPERIMENTAL,
                "Enable Shared Inventory",
                true,
                "If set to false, the shared inventory will not be used and instead a unique inventory will be used for every lemurian you control.\r\n" +
                "Disabling this setting will make all lemurians visible on the scoreboard.",
                true);

            PluginConfig.miniElders = PluginConfig.BindOption(EXPERIMENTAL,
                "Mini Elder Lemurians",
                true,
                "Theyre so cute omg");

            PluginConfig.scaleValue = PluginConfig.BindOptionSlider(EXPERIMENTAL,
                "Mini Elder Scale Value",
                5,
                "Size to grow for every fusion level",
                0, 20);
            PluginConfig.initScaleValue = PluginConfig.BindOptionSlider(EXPERIMENTAL,
                "Mini Elder Initial Scale",
                10,
                "Initial size of elder lemurians",
                0, 100);

            PluginConfig.enableDetailedLogs = PluginConfig.BindOption(EXPERIMENTAL,
                "Enable Detailed Logs",
                false,
                "For dev use/debugging. Keep this on if you want to submit a bug report.");

            //
            // stats
            //
            PluginConfig.rebalanceStatScaling = PluginConfig.BindOption(STATS,
                "Rebalance Stat Scaling",
                true,
                "Rebalances base stats and stat scaling so that new summons on later stages have an easier time surviving and elder lemurians dont get buffed as much");

            PluginConfig.statMultHealth = PluginConfig.BindOptionSlider(STATS,
                "Fusion Health Increase",
                20,
                "Health Multiplier for each lemur fusion, in percent.",
                0, 100);

            PluginConfig.statMultDamage = PluginConfig.BindOptionSlider(STATS,
                "Fusion Damage Increase",
                20,
                "Damage Multiplier for each lemur fusion, in percent.",
                0, 100);

            PluginConfig.statMultAttackSpeed = PluginConfig.BindOptionSlider(STATS,
                "Fusion Attack Speed Increase",
                20,
                "Attack Speed Multiplier for each lemur fusion, in percent.",
                0, 100);

            PluginConfig.statMultEvo = PluginConfig.BindOptionSlider(STATS,
                "Evolution Stat Modifier",
                100,
                "Additional Health and Damage Multiplier per Evolution Stage, in percent. Vanilla is 100.",
                0, 200);
        }

        internal static void ReadAIConfig()
        {
            AITweaks.disableFallDamage = PluginConfig.BindOption(AI_CONFIG,
                "Disable Fall Damage",
                true,
                "If true, prevents Lemurians from taking fall damage.");

            AITweaks.immuneToVoidDeath = PluginConfig.BindOption(AI_CONFIG,
                "Immune To Void Death",
                false,
                "If true, prevents Lemurians from dying to void insta-kill explosions.");

            AITweaks.improveAI = PluginConfig.BindOption(AI_CONFIG,
                "Improve AI",
                true,
                "Makes minions less likely to stand around and makes them better at not dying.",
                true);

            AITweaks.enableProjectileTracking = PluginConfig.BindOption(AI_CONFIG,
                "Enable Projectile Tracking",
                true,
                "Requires \"Improve AI\". Tracks and attempts to dodge most projectiles that come close.");

            AITweaks.visualizeProjectileTracking = PluginConfig.BindOption(AI_CONFIG,
                "Visualize Projectile Tracking",
                false,
                "Requires \"Enable Projectile Tracking\". Creates a line connecting the foot position to the tracked projectile and makes the hitbox visible.");

            AITweaks.updateFrequency = PluginConfig.BindOptionSlider(AI_CONFIG,
                "In Combat Update Frequency",
                0.1f,
                "Requires \"Enable Projectile Tracking\". Controls how often to refresh the projectile targeting. " +
                "Increasing this value may change how the AI pathing behaves but will result in getting hit more often.",
                0.1f, 0.5f);

            AITweaks.detectionRadius = PluginConfig.BindOptionSlider(AI_CONFIG,
                "Detection Radius",
                5,
                "Requires \"Enable Projectile Tracking\". Controls the size of the detection area to use when deciding whether running away is necessary. " +
                "Distance from large AOE zones is estimated using the hitbox instead of center point to account for size and rotation.",
                5, 20);
        }

        private static void ReadConfigExtended()
        {
            /*
            ConfigExtended.CapEvo = PluginConfig.BindAndOptions(Section_Evolution, 
				"Enable Evolution Cap",
				false, 
				"Stops minions from evolving after a set number of times.");

            ConfigExtended.EvoMax = PluginConfig.BindAndOptions(Section_Evolution, 
				"Evolution Level Cap",
				0, 
				"The maximum evolution level devotion minions can reach. Requires \"Enable Evolution Cap\"");

            ConfigExtended.Elite_Blacklist_Raw = PluginConfig.BindAndOptions(Section_Evolution,
				"Blacklisted Elite Types", 
				"", 
				"List of Elite Types that should not be selected.", 
				true);

            ConfigExtended.Evo_BodyStages_Raw = PluginConfig.BindAndOptions(Section_Evolution,
				"Evolution Body Stages", 
				"LemurianBruiserBody, 3", 
				"Change the Body type at specific evolution levels with a specific body name.", 
				true);
			*/
            //Death
            /*ConfigExtended.OnDeathPenalty = PluginConfig.BindAndOptionsSlider(Section_Death,
				"Death Penalty",
				(int)DevotionTweaks.DeathPenalty.TrueDeath,
                "What to do to a devotion minion when it dies. For Devolve and ResetToBaby, it will kill them if they're at Evolution Level 1\r\n\r\n" +
                $"{nameof(DevotionTweaks.DeathPenalty.TrueDeath)}\t" +
                $"{nameof(DevotionTweaks.DeathPenalty.Devolve)}\t" +
                $"{nameof(DevotionTweaks.DeathPenalty.ResetToBaby)}\t", 
				0, System.Enum.GetValues(typeof(DevotionTweaks.DeathPenalty)).Length);
			*/
            ConfigExtended.DeathDrop_Enable = PluginConfig.BindOption(DEATH,
                "Enable Death Changes",
                true,
                Desc_Enable,
                true);

            ConfigExtended.DeathDrop_DropEgg = PluginConfig.BindOption(DEATH,
				"Egg On Death", 
				false, 
				"Should lemurs revert to an egg when they die?");

            ConfigExtended.DeathDrop_DropAll = PluginConfig.BindOption(DEATH,
                "Drop Duplicate Items",
                false,
                "When items are dropped on death, should it drop additional items equal to the number of stacks? Can result in getting more items back than you originally gave up.");

            ConfigExtended.DeathDrop_ItemType = PluginConfig.BindOption(DEATH, 
				"Item Dropped On Death",
				DevotionTweaks.DeathItem.Scrap, 
				"What kind of item to drop when lemurs are killed.",
				true);

            ConfigExtended.DeathDrops_TierToItem_Map_Raw = PluginConfig.BindOption(DEATH, 
				"Custom Item Map", 
				"Tier1Def, ScrapWhite; Tier2Def, ScrapGreen; Tier3Def, ScrapRed; BossTierDef, ScrapYellow; LunarTierDef, LunarTrinket; VoidTier1Def, TreasureCacheVoid; VoidTier2Def, TreasureCacheVoid; VoidTier3Def, TreasureCacheVoid; VoidBossDef, TreasureCacheVoid", 
				"Requires \"Item Dropped On Death\" set to Custom. Maps out the Item to drop for each held Item Tier, in the format \"TierDef,ItemDef;\" (whitespace ignored)",
				true);

			//Blacklist
			ConfigExtended.Blacklist_Enable = PluginConfig.BindOption(BLACKLIST,
                "Enable Blacklist Changes",
                true, 
                Desc_Enable, 
                true);

            ConfigExtended.Blacklist_Filter_CannotCopy = PluginConfig.BindOption(BLACKLIST, 
				"Blacklist CannotCopy", 
				true,
				"Automatically blacklist items that are tagged as CannotCopy. (The same filter used for Engineer Turrets)");

			ConfigExtended.Blacklist_Filter_Scrap = PluginConfig.BindOption(BLACKLIST, 
				"Blacklist Scrap",
				true,
				"Automatically blacklist items that are tagged as Scrap.");

			ConfigExtended.Blacklisted_Items_Raw = PluginConfig.BindOption(BLACKLIST, 
				"Item Blacklist",
				"WardOnLevel, BeetleGland",
				"Items prevented from being given to minions. Items not on this list may still get filtered by other settings.",
				true);

			ConfigExtended.Blacklisted_ItemTiers_Raw = PluginConfig.BindOption(BLACKLIST, 
				"Tier Blacklist",
				"LunarTierDef, VoidTier1Def, VoidTier2Def, VoidTier3Def, VoidBossDef",
				"Item tiers prevented from being given to lemurs.",
				true);
		}
	}
}
