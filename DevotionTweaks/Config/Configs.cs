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
		
		private const string Section_Misc = "Misc Settings";

		private const string Section_ItemDrop = "Item Drop";
		private const string Section_Blacklist = "Item Blacklist";

		private const string Label_Enable = "!Enable Changes";

		private const string Desc_Enable = "Enables changes for this section.";

		public static void Setup()
        {
            PluginConfig.ReadConfig();

            new HeavyChanges();
            new LightChanges();

            ReadLightConfig();
			ReadHeavyConfig();
		}

		private static void ReadHeavyConfig()
        {
			//Enable
			HeavyChanges.Enable = PluginConfig.BindAndOptions(Section_EnableConfig, "Enable Config", false, "Enables this entire part of the mod to function, may cause mod incompats.").Value;
			//HeavyChanges.EvoMax = PluginConfig.BindAndOptions(Section_Evolution, "Evolution Level Cap", 0, "The maximum evolution level devotion minions can reach. (0 = Unlimited/Vanilla)").Value;
			//HeavyChanges.EliteList_Raw = PluginConfig.BindAndOptions(Section_Evolution, "Elite Tiers", "edFire, 1, edIce, 1, edLightning, 1, edEarth, 1, edPoison, 2, edHaunted, 2, edLunar, 2", "List of Elite Types and their Tier.").Value;
			HeavyChanges.Evo_BodyStages_Raw = PluginConfig.BindAndOptions(Section_Evolution, "Evolution Body Stages", "LemurianBruiserBody, 3", "Change the Body type at specific evolution levels with a specific body name.");
			//Death
			HeavyChanges.OnDeathPenalty = PluginConfig.BindAndOptions(Section_Death, "Penalty", HeavyChanges.DeathPenalty.TrueDeath, "What to do to a devotion minion when it dies. (0 = Kill/Vanilla, 1 = Devolve, 2 = Reset Evolution Level to 1) (For 1 and 2 it will kill them if they're at Evolution Level 1.)");
			HeavyChanges.DropEggOnDeath = PluginConfig.BindAndOptions(Section_Death, "Egg On Death", false, "Should minions revert to an egg when they are killed off?");
		}
		private static void ReadLightConfig()
		{
			LightChanges.ItemDrop_Enable = PluginConfig.BindAndOptions(Section_ItemDrop, Label_Enable, false, Desc_Enable).Value;
			LightChanges.ItemDrop_Type = PluginConfig.BindAndOptions(Section_ItemDrop, "Item Drop", LightChanges.DeathItem.Scrap, "What kind of item to drop when minions are removed. (0 = Nothing, 1 = Scrap (vanilla), 2 = Original Item, 3 = Custom");
			LightChanges.ItemDrop_CustomDropList_Raw = PluginConfig.BindAndOptions(Section_ItemDrop, "Custom Drop List", "Tier1Def, ScrapWhite, Tier2Def, ScrapGreen, Tier3Def, ScrapRed, BossTierDef, ScrapYellow, LunarTierDef, LunarTrinket, VoidTier1Def, TreasureCacheVoid, VoidTier2Def, TreasureCacheVoid, VoidTier3Def, TreasureCacheVoid, VoidBossDef, TreasureCacheVoid", "The item to drop for each tier when Item Drop is set to 0.").Value;
			//Blacklist
			LightChanges.Blacklist_Enable = PluginConfig.BindAndOptions(Section_Blacklist, Label_Enable, false, Desc_Enable).Value;
			LightChanges.BlackList_Filter_CannotCopy = PluginConfig.BindAndOptions(Section_Blacklist, "Include CannotCopy", true, "Automatically blacklist items that are tagged as CannotCopy. (The same filter used for Engineer Turrets)").Value;
			LightChanges.BlackList_Filter_Scrap = PluginConfig.BindAndOptions(Section_Blacklist, "Include Scrap", true, "Automatically blacklist items that are tagged as Scrap.").Value;
			LightChanges.BlackList_ItemList_Raw = PluginConfig.BindAndOptions(Section_Blacklist, "Item Blacklist", "WardOnLevel, BeetleGland", "Items to blacklist from being picked.").Value;
			LightChanges.BlackList_TierList_Raw = PluginConfig.BindAndOptions(Section_Blacklist, "Tier Blacklist", "LunarTierDef, VoidTier1Def, VoidTier2Def, VoidTier3Def, VoidBossDef", "Tiers to blacklist from being picked.").Value;
			//Misc
			LightChanges.Misc_FixEvo = PluginConfig.BindAndOptions(Section_Misc, "Fix When Disabled", true, "Fixes the item orb not showing when giving items to eggs and allows devotion minions to evolve even when the Artifact is disabled.").Value;
		}
	}
}
