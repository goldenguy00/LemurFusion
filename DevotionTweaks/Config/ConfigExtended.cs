using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LemurFusion.Config
{
    internal class ConfigExtended
    {
        //internal static ConfigEntry<bool> Enable;
        //internal static ConfigEntry<int> EvoMax;
        internal static ConfigEntry<bool> DeathDrop_DropEgg;
        //internal static ConfigEntry<int> OnDeathPenalty;
        //internal static ConfigEntry<bool> CapEvo;

        //internal static ConfigEntry<string> Elite_Blacklist_Raw;
        //internal static ConfigEntry<string> Evo_BodyStages_Raw;

        //internal static List<BodyEvolutionStage> Evo_BodyStages = [];
        //internal static List<EliteDef> Elite_Blacklist = [];


        internal static ConfigEntry<bool> Blacklist_Enable;
        internal static ConfigEntry<bool> Blacklist_Filter_CannotCopy;
        internal static ConfigEntry<bool> Blacklist_Filter_Scrap;

        internal static ConfigEntry<bool> DeathDrop_Enable;
        internal static ConfigEntry<int> DeathDrop_ItemType;
        internal static ConfigEntry<bool> DeathDrop_DropAll;

        internal static ConfigEntry<string> Blacklisted_ItemTiers_Raw;
        internal static ConfigEntry<string> Blacklisted_Items_Raw;
        internal static ConfigEntry<string> DeathDrops_TierToItem_Map_Raw;

        internal static HashSet<ItemTier> Blacklisted_ItemTiers = [];
        internal static HashSet<ItemIndex> Blacklisted_Items = [];
        internal static SortedList<ItemTier, ItemIndex> DeathDrops_TierToItem_Map = [];
        internal static Func<ItemIndex, bool> Blacklist_ItemFilter;

        private static void BLItems()
        {
            LemurFusionPlugin._logger.LogInfo("Blacklisting items...");

            // just to get familiar with this syntax... move along.
            Blacklisted_Items = [.. from item in Blacklisted_Items_Raw.Value.Split(',')
                                    let idx = ItemCatalog.FindItemIndex(item.Trim())
                                    where idx != ItemIndex.None && ItemCatalog.IsIndexValid(idx)
                                    select ItemCatalog.GetItemDef(idx) into def
                                    where def != null
                                    select def.itemIndex];

            foreach (var item in Blacklisted_Items)
            {
                LemurFusionPlugin._logger.LogInfo(ItemCatalog.GetItemDef(item).nameToken);
            }
        }

        private static void BLItemTiers()
        {
            LemurFusionPlugin._logger.LogInfo("Blacklisting tiers...");

            Blacklisted_ItemTiers = [.. from tier in Blacklisted_ItemTiers_Raw.Value.Split(',')
                                        let def = ItemTierCatalog.FindTierDef(tier.Trim())
                                        where def
                                        select def.tier];

            foreach (var item in Blacklisted_ItemTiers)
            {
                LemurFusionPlugin._logger.LogInfo(ItemTierCatalog.GetItemTierDef(item)?.name);
            }
        }

        private static void BLFunctionCreate()
        {
            LemurFusionPlugin._logger.LogInfo("Generating blacklist function...");

            Blacklist_ItemFilter = (idx) =>
            {
                if (idx == ItemIndex.None || !ItemCatalog.IsIndexValid(idx)) return false;

                var itemDef = ItemCatalog.GetItemDef(idx);
                if (!itemDef || itemDef.hidden || !itemDef.canRemove || itemDef.tier == ItemTier.NoTier) return false;

                var tierDef = ItemTierCatalog.GetItemTierDef(itemDef.tier);
                bool valid = tierDef && tierDef.canScrap;

                if (Blacklist_Enable.Value)
                {
                    valid &= !Blacklisted_Items.Contains(idx) && !Blacklisted_ItemTiers.Contains(itemDef.tier);

                    if (Blacklist_Filter_CannotCopy.Value)
                        valid &= itemDef.DoesNotContainTag(ItemTag.CannotCopy);
                    if (Blacklist_Filter_Scrap.Value)
                        valid &= itemDef.DoesNotContainTag(ItemTag.Scrap);
                }
                return valid;
            };
        }

        internal static void TestFilter()
        {
            LemurFusionPlugin._logger.LogInfo("Testing blacklist function...");

            foreach (var item in ItemCatalog.allItemDefs.ToList())
            {
                var result = Blacklist_ItemFilter(item.itemIndex);
                var defResult = Inventory.defaultItemCopyFilterDelegate(item.itemIndex);

                if (result && !defResult)
                {
                    LemurFusionPlugin._logger.LogInfo(
                        $"\t{item.nameToken} | {item.tier}\r\n" +
                        $"\tDefault: {defResult}\t New: {result}\r\n" +
                        $"\tHidden? {item.hidden}\t CanRemove? {item.canRemove}\t CanScrap? {ItemTierCatalog.GetItemTierDef(item.tier)?.canScrap}\r\n" +
                        $"\tIn Item Blacklist? {Blacklisted_Items.Contains(item.itemIndex)}\r\n\tIn Tier blacklist? {Blacklisted_ItemTiers.Contains(item.tier)}\r\n" +
                        $"\tTags: {string.Concat(item.tags.Select(t => Enum.GetName(typeof(ItemTag), t) + ", "))}");
                }
            }
        }
        /*
        internal static void PostLoad2()
        {
            if (Enable.Value)
            {
                var itemString = Evo_BodyStages_Raw.Value.Split(',');
                Evo_BodyStages = [];
                for (int i = 0; i + 1 < itemString.Length; i += 2)
                {
                    if (int.TryParse(itemString[i + 1].Trim(), out var intResult) && intResult > 1)
                    {
                        Evo_BodyStages.Add(new()
                        {
                            BodyName = itemString[i].Trim(),
                            EvolutionStage = intResult
                        });
                    }
                    else
                    {
                        LemurFusionPlugin._logger.LogWarning(string.Format("'{1}' Is being assigned to an invalid evolution stage, ignoring.", itemString[i]));
                    }
                }

                var Evo_BaseMaster = MasterCatalog.FindMasterPrefab(DevotionTweaks.masterPrefabName);
                if (Evo_BaseMaster)
                {
                    LemurFusionPlugin._logger.LogInfo("Set [" + Evo_BaseMaster.name + "] as base master form.");
                }
                else
                {
                    LemurFusionPlugin._logger.LogWarning(string.Format("Could not find master for [{0}] this will cause errors.", DevotionTweaks.masterPrefabName));
                }

                CharacterMaster master = Evo_BaseMaster.GetComponent<CharacterMaster>();
                if (master)
                {
                    GameObject bodyPrefab = master.bodyPrefab;
                    if (bodyPrefab)
                    {
                        LemurFusionPlugin._logger.LogInfo("Base Master has [" + bodyPrefab.name + "] as base body form.");
                    }
                }
            }
        }*/
        internal static void PostLoad()
        {
            Blacklist_ItemFilter = Inventory.defaultItemCopyFilterDelegate;
            DeathDrops_TierToItem_Map = [];
            DeathDrops_TierToItem_Map.Add(ItemTier.Tier1, RoR2Content.Items.ScrapWhite.itemIndex);
            DeathDrops_TierToItem_Map.Add(ItemTier.Tier2, RoR2Content.Items.ScrapGreen.itemIndex);
            DeathDrops_TierToItem_Map.Add(ItemTier.Tier3, RoR2Content.Items.ScrapRed.itemIndex);
            DeathDrops_TierToItem_Map.Add(ItemTier.Boss, RoR2Content.Items.ScrapYellow.itemIndex);

            if (Blacklist_Enable.Value)
            {
                try
                {
                    BLItemTiers();
                    BLItems();
                    BLFunctionCreate();
                    TestFilter();
                }
                catch (Exception e)
                {
                    LemurFusionPlugin._logger.LogWarning(e.Message);
                    LemurFusionPlugin._logger.LogWarning(e.StackTrace);
                }
            }

            if (DeathDrop_Enable.Value)
            {
                // just for the fun of it
                foreach (var itemPair in DeathDrops_TierToItem_Map_Raw.Value.Replace(" ", "").Split(';'))
                {
                    var split = itemPair.Split(',');
                    if (split.Length != 2)
                    {
                        LemurFusionPlugin._logger.LogWarning($"String parsing error for (TierDef, ItemDef) pair '({itemPair})' for Custom Drop List.");
                        continue;
                    }

                    var tierDef = ItemTierCatalog.FindTierDef(split[0]);
                    var itemIndex = ItemCatalog.FindItemIndex(split[1]);
                    LemurFusionPlugin._logger.LogInfo($"Attemping to add ({tierDef}, {itemIndex}) pair parsed from string'({itemPair})' for Custom Drop List.");
                    if (tierDef && itemIndex != ItemIndex.None)
                    {
                        if (DeathDrops_TierToItem_Map.ContainsKey(tierDef.tier))
                            LemurFusionPlugin._logger.LogWarning($"Overwriting duplicate {tierDef?.name} with '({tierDef?.name},{itemIndex})' for Custom Drop List.");
                        DeathDrops_TierToItem_Map[tierDef.tier] = itemIndex;
                    }
                    else
                    {
                        LemurFusionPlugin._logger.LogWarning($"Could not find (TierDef, ItemDef) pair '({tierDef?.name},{itemIndex})' for Custom Drop List.");
                    }
                }
            }
        }
    }
}
