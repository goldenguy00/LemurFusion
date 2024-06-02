namespace LemurFusion.Config
{
    // > "99% of this is practically ripped off from RiskyMod, ain't gonna lie."
    //and now it's been ported entirely into LemurFusion. Common Moffein W, once again.
    public static class Configs
	{
		private const string Section_EnableConfig = "!Enable Config";
		private const string Section_BaseStats = "Minion Base Stats";
		private const string Section_Evolution = "Evolution Settings";
		private const string Section_Death = "Death Settings";

		private const string Section_ItemDrop = "Item Drop";
		private const string Section_Blacklist = "Item Blacklist";

		private const string Label_Enable = "!Enable Changes";

		private const string Desc_Enable = "Enables changes for this section.";

		public static void Setup()
        {
            PluginConfig.ReadConfig();
            ReadLightConfig();
			ReadHeavyConfig();

            new LightChanges();
            new HeavyChanges();
        }

        private static void ReadHeavyConfig()
        {

            LemurFusionPlugin._logger.LogInfo("readin heavy");
            //Enable
            HeavyChanges.Enable = PluginConfig.BindAndOptions(Section_EnableConfig,
				"Enable Config",
				true,
				"Enables this entire part of the mod to function, may cause mod incompats.");

			HeavyChanges.ImproveAI = PluginConfig.BindAndOptions(Section_EnableConfig,
				"Improve AI",
				true,
				"Yeah");

			HeavyChanges.CapEvo = PluginConfig.BindAndOptions(Section_Evolution, 
				"Enable Evolution Cap",
				false, 
				"Stops minions from evolving after a set number of times.");

			HeavyChanges.EvoMax = PluginConfig.BindAndOptions(Section_Evolution, 
				"Evolution Level Cap",
				0, 
				"The maximum evolution level devotion minions can reach. Requires \"Enable Evolution Cap\"");

			HeavyChanges.Elite_Blacklist_Raw = PluginConfig.BindAndOptions(Section_Evolution,
				"Blacklisted Elite Types", 
				"", 
				"List of Elite Types that should not be selected.");

			HeavyChanges.Evo_BodyStages_Raw = PluginConfig.BindAndOptions(Section_Evolution,
				"Evolution Body Stages", 
				"LemurianBruiserBody, 3", 
				"Change the Body type at specific evolution levels with a specific body name.");

			//Death
			HeavyChanges.OnDeathPenalty = PluginConfig.BindAndOptionsSlider(Section_Death,
				"Death Penalty",
				(int)HeavyChanges.DeathPenalty.TrueDeath,
                "What to do to a devotion minion when it dies. For Devolve and ResetToBaby, it will kill them if they're at Evolution Level 1\r\n\r\n" +
                $"{nameof(HeavyChanges.DeathPenalty.TrueDeath)}\r\n" +
                $"{nameof(HeavyChanges.DeathPenalty.Devolve)}\r\n" +
                $"{nameof(HeavyChanges.DeathPenalty.ResetToBaby)}\r\n", 
				0, System.Enum.GetValues(typeof(HeavyChanges.DeathPenalty)).Length);

			HeavyChanges.DropEggOnDeath = PluginConfig.BindAndOptions(Section_Death,
				"Egg On Death", 
				true, 
				"Should minions revert to an egg when they are killed off?");
		}
		private static void ReadLightConfig()
        {
            LemurFusionPlugin._logger.LogInfo("Reading light");
            LightChanges.ItemDrop_Enable = PluginConfig.BindAndOptions(Section_ItemDrop, 
				Label_Enable, 
				true, 
				Desc_Enable);

			LightChanges.ItemDrop_Type = PluginConfig.BindAndOptionsSlider(Section_ItemDrop, 
				"Item Drop On Death", 
				(int)LightChanges.DeathItem.Scrap, 
				"What kind of item to drop when minions are removed.\r\n\r\n" +
                $"{nameof(LightChanges.DeathItem.None)}\r\n" +
                $"{nameof(LightChanges.DeathItem.Scrap)}\r\n" +
                $"{nameof(LightChanges.DeathItem.Original)}\r\n" +
                $"{nameof(LightChanges.DeathItem.Custom)}\r\n",
                0, System.Enum.GetValues(typeof(LightChanges.DeathItem)).Length);
            LightChanges.ItemDrop_DropAll = PluginConfig.BindAndOptions(Section_ItemDrop,
				"Drop Duplicate Items",
				false,
				"When items are dropped on death, should it drop additional items equal to the number of stacks? Can result in getting more items back than you gave originally.");

            LightChanges.DeathDrops_TierToItem_Map_Raw = PluginConfig.BindAndOptions(Section_ItemDrop, 
				"Item Drop On Death Map", 
				"Tier1Def, ScrapWhite; Tier2Def, ScrapGreen; Tier3Def, ScrapRed; BossTierDef, ScrapYellow; LunarTierDef, LunarTrinket; VoidTier1Def, TreasureCacheVoid; VoidTier2Def, TreasureCacheVoid; VoidTier3Def, TreasureCacheVoid; VoidBossDef, TreasureCacheVoid", 
				"The item to drop for each Item Tier, in the format \"{TierDef},{ItemDef};\"");

			//Blacklist
			LightChanges.Blacklist_Enable = PluginConfig.BindAndOptions(Section_Blacklist, Label_Enable, true, Desc_Enable);
			LightChanges.Blacklist_Filter_CannotCopy = PluginConfig.BindAndOptions(Section_Blacklist, 
				"Blacklist CannotCopy", 
				true,
				"Automatically blacklist items that are tagged as CannotCopy. (The same filter used for Engineer Turrets)");

			LightChanges.Blacklist_Filter_Scrap = PluginConfig.BindAndOptions(Section_Blacklist, 
				"Blacklist Scrap",
				true,
				"Automatically blacklist items that are tagged as Scrap.");

			LightChanges.Blacklisted_Items_Raw = PluginConfig.BindAndOptions(Section_Blacklist, 
				"Item Blacklist",
				"WardOnLevel, BeetleGland",
				"Items prevented from being given to minions.");

			LightChanges.Blacklisted_ItemTiers_Raw = PluginConfig.BindAndOptions(Section_Blacklist, 
				"Tier Blacklist",
				"LunarTierDef, VoidTier1Def, VoidTier2Def, VoidTier3Def, VoidBossDef",
				"Item tiers prevented from being given or dropped by minions.");
		}
	}
}
