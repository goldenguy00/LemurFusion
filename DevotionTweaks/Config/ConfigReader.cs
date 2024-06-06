namespace LemurFusion.Config
{
    // > "99% of this is practically ripped off from RiskyMod, ain't gonna lie."
    //and now it's been ported entirely into LemurFusion. Common Moffein W, once again.
    internal static class ConfigReader
	{
		//private const string Section_BaseStats = "Minion Base Stats";
		//private const string Section_Evolution = "Evolution Settings";
		private const string DEATH = "Death Settings";
		private const string BLACKLIST = "Item Blacklist";
		private const string Desc_Enable = "Enables changes for this section.";

        internal static void Setup()
        {
            PluginConfig.ReadConfig();

			ReadConfigExtended();
            new ConfigExtended();
        }

        private static void ReadConfigExtended()
        {
            LemurFusionPlugin._logger.LogInfo("Reading config extended");
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
            ConfigExtended.DeathDrop_Enable = PluginConfig.BindAndOptions(DEATH, "Enable Death Changes", true, Desc_Enable, true);

            ConfigExtended.DeathDrop_DropEgg = PluginConfig.BindAndOptions(DEATH,
				"Egg On Death", 
				false, 
				"Should minions revert to an egg when they are killed off?");

            ConfigExtended.DeathDrop_DropAll = PluginConfig.BindAndOptions(DEATH,
                "Drop Duplicate Items",
                false,
                "When items are dropped on death, should it drop additional items equal to the number of stacks? Can result in getting more items back than you gave originally.");

            ConfigExtended.DeathDrop_ItemType = PluginConfig.BindAndOptions(DEATH, 
				"Item Dropped On Death",
				DevotionTweaks.DeathItem.Scrap, 
				"What kind of item to drop when minions are killed.",
				true);

            ConfigExtended.DeathDrops_TierToItem_Map_Raw = PluginConfig.BindAndOptions(DEATH, 
				"Custom Item Map", 
				"Tier1Def, ScrapWhite; Tier2Def, ScrapGreen; Tier3Def, ScrapRed; BossTierDef, ScrapYellow; LunarTierDef, LunarTrinket; VoidTier1Def, TreasureCacheVoid; VoidTier2Def, TreasureCacheVoid; VoidTier3Def, TreasureCacheVoid; VoidBossDef, TreasureCacheVoid", 
				"Requires \"Item Dropped On Death\" set to Custom. Maps out the Item to drop for each held Item Tier, in the format \"TierDef,ItemDef;\" (whitespace ignored)",
				true);

			//Blacklist
			ConfigExtended.Blacklist_Enable = PluginConfig.BindAndOptions(BLACKLIST, "Enable Blacklist Changes", true, Desc_Enable, true);

            ConfigExtended.Blacklist_Filter_SprintRelated = PluginConfig.BindAndOptions(BLACKLIST,
                "Blacklist Sprint Related Items",
                true,
                "Automatically blacklist items that are tagged as Sprint Related. Lemurs can't sprint. Sorry.");

            ConfigExtended.Blacklist_Filter_CannotCopy = PluginConfig.BindAndOptions(BLACKLIST, 
				"Blacklist CannotCopy", 
				true,
				"Automatically blacklist items that are tagged as CannotCopy. (The same filter used for Engineer Turrets)");

			ConfigExtended.Blacklist_Filter_Scrap = PluginConfig.BindAndOptions(BLACKLIST, 
				"Blacklist Scrap",
				true,
				"Automatically blacklist items that are tagged as Scrap.");

			ConfigExtended.Blacklisted_Items_Raw = PluginConfig.BindAndOptions(BLACKLIST, 
				"Item Blacklist",
				"WardOnLevel, BeetleGland",
				"Items prevented from being given to minions. Items not on this list may still get filtered by other settings.",
				true);

			ConfigExtended.Blacklisted_ItemTiers_Raw = PluginConfig.BindAndOptions(BLACKLIST, 
				"Tier Blacklist",
				"LunarTierDef, VoidTier1Def, VoidTier2Def, VoidTier3Def, VoidBossDef",
				"Item tiers prevented from being given to minions.",
				true);
		}
	}
}
