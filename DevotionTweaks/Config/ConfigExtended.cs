using BepInEx.Configuration;
using LemurFusion.Devotion;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LemurFusion.Config
{
    internal class ConfigExtended
    {
        #region Config Entries
        public static ConfigEntry<bool> Blacklist_Enable;
        public static ConfigEntry<bool> Blacklist_Filter_CannotCopy;
        public static ConfigEntry<bool> Blacklist_Filter_Scrap;

        public static ConfigEntry<bool> DeathDrop_Enable;
        public static ConfigEntry<bool> DeathDrop_DropEgg;
        public static ConfigEntry<DevotionTweaks.DeathItem> DeathDrop_ItemType;
        public static ConfigEntry<bool> DeathDrop_DropAll;

        public static ConfigEntry<string> Blacklisted_ItemTiers_Raw;
        public static ConfigEntry<string> Blacklisted_Items_Raw;
        public static ConfigEntry<string> DeathDrops_TierToItem_Map_Raw;

        public static HashSet<ItemTier> Blacklisted_ItemTiers = [];
        public static HashSet<ItemIndex> Blacklisted_Items = [];
        public static SortedList<ItemTier, ItemIndex> DeathDrops_TierToItem_Map = [];
        public static Func<ItemIndex, bool> Blacklist_Filter;
        #endregion

        #region Loading
        internal static void PostLoad()
        {
            Blacklist_Filter = Inventory.defaultItemCopyFilterDelegate;
            Blacklisted_Items = [];
            Blacklisted_ItemTiers = [];
            DeathDrops_TierToItem_Map = [];
            DeathDrops_TierToItem_Map.Add(ItemTier.Tier1, RoR2Content.Items.ScrapWhite.itemIndex);
            DeathDrops_TierToItem_Map.Add(ItemTier.Tier2, RoR2Content.Items.ScrapGreen.itemIndex);
            DeathDrops_TierToItem_Map.Add(ItemTier.Tier3, RoR2Content.Items.ScrapRed.itemIndex);
            DeathDrops_TierToItem_Map.Add(ItemTier.Boss, RoR2Content.Items.ScrapYellow.itemIndex);


            if (Blacklist_Enable.Value)
            {
                BLItemTiers();
                BLItems();
                CreateBlacklistFunction();
            }

            if (DeathDrop_Enable.Value)
            {
                CreateDeathDropMap();
            }
        }
        #endregion

        #region Death Mechanics
        private static void CreateDeathDropMap()
        {
            if (string.IsNullOrWhiteSpace(DeathDrops_TierToItem_Map_Raw.Value)) return;

            try
            {
                // just for the fun of it
                foreach (var itemPair in DeathDrops_TierToItem_Map_Raw.Value.Replace(" ", "").Split(';'))
                {
                    var split = itemPair.Split(',');
                    if (split.Length != 2)
                    {
                        LemurFusionPlugin.LogWarning($"String parsing error for (TierDef, ItemDef) pair '({itemPair})' for Custom Drop List.");
                        continue;
                    }

                    var tierDef = ItemTierCatalog.FindTierDef(split[0]);
                    var itemIndex = ItemCatalog.FindItemIndex(split[1]);

                    if (PluginConfig.enableDetailedLogs.Value) LemurFusionPlugin.LogInfo($"Attemping to add ({tierDef}, {itemIndex}) pair parsed from string'({itemPair})' for Custom Drop List.");
                    if (tierDef && itemIndex != ItemIndex.None)
                    {
                        if (DeathDrops_TierToItem_Map.ContainsKey(tierDef.tier) && PluginConfig.enableDetailedLogs.Value)
                            LemurFusionPlugin.LogWarning($"Overwriting duplicate {tierDef?.name} with '({tierDef?.name},{itemIndex})' for Custom Drop List.");
                        DeathDrops_TierToItem_Map[tierDef.tier] = itemIndex;
                    }
                    else
                    {
                        LemurFusionPlugin.LogWarning($"Could not find (TierDef, ItemDef) pair '({tierDef?.name},{itemIndex})' for Custom Drop List.");
                    }
                }
            }
            catch (Exception e)
            {
                LemurFusionPlugin.LogWarning(e.Message);
                LemurFusionPlugin.LogWarning(e.StackTrace);
            }
        }
        #endregion

        #region Blacklist
        private static void BLItems()
        {
            LemurFusionPlugin.LogInfo("Blacklisting items...");

            try
            {
                // just to get familiar with this syntax... move along.
                Blacklisted_Items = [.. from item in Blacklisted_Items_Raw.Value?.Split(',') ?? []
                                    let idx = ItemCatalog.FindItemIndex(item?.Trim())
                                    where idx != ItemIndex.None && ItemCatalog.IsIndexValid(idx)
                                    select ItemCatalog.GetItemDef(idx) into def
                                    where def
                                    select def.itemIndex];

                if (!PluginConfig.enableDetailedLogs.Value) return;
                foreach (var item in Blacklisted_Items)
                {
                    LemurFusionPlugin.LogInfo(ItemCatalog.GetItemDef(item)?.nameToken);
                }
            }
            catch (Exception e)
            {
                LemurFusionPlugin.LogWarning(e.Message);
                LemurFusionPlugin.LogWarning(e.StackTrace);
            }
        }

        private static void BLItemTiers()
        {
            LemurFusionPlugin.LogInfo("Blacklisting tiers...");
            try
            {
                Blacklisted_ItemTiers = [.. from tier in Blacklisted_ItemTiers_Raw.Value.Split(',') ?? []
                                        let def = ItemTierCatalog.FindTierDef(tier?.Trim() ?? string.Empty)
                                        where def
                                        select def.tier];

                if (!PluginConfig.enableDetailedLogs.Value) return;
                foreach (var item in Blacklisted_ItemTiers)
                {
                    LemurFusionPlugin.LogInfo(ItemTierCatalog.GetItemTierDef(item)?.name);
                }
            }
            catch (Exception e)
            {
                LemurFusionPlugin.LogWarning(e.Message);
                LemurFusionPlugin.LogWarning(e.StackTrace);
            }
        }

        private static void CreateBlacklistFunction()
        {
            try
            {
                Blacklist_Filter = (idx) =>
                {
                    if (idx == ItemIndex.None || !ItemCatalog.IsIndexValid(idx)) return false;

                    var itemDef = ItemCatalog.GetItemDef(idx);
                    if (!itemDef || itemDef.hidden || !itemDef.canRemove || itemDef.tier == ItemTier.NoTier) return false;

                    var valid = !Blacklisted_Items.Contains(idx) && !Blacklisted_ItemTiers.Contains(itemDef.tier);

                    if (Blacklist_Filter_CannotCopy.Value)
                        valid &= itemDef.DoesNotContainTag(ItemTag.CannotCopy);
                    if (Blacklist_Filter_Scrap.Value)
                        valid &= itemDef.DoesNotContainTag(ItemTag.Scrap) && itemDef.DoesNotContainTag(ItemTag.PriorityScrap);

                    return valid;
                };

                TestFilter();
            }
            catch (Exception e)
            {
                LemurFusionPlugin.LogWarning(e.Message);
                LemurFusionPlugin.LogWarning(e.StackTrace);
            }
        }

        internal static void TestFilter()
        {
            if (!PluginConfig.enableDetailedLogs.Value) return;

            LemurFusionPlugin.LogInfo("Testing blacklist function...");
            try
            {
                foreach (var item in ItemCatalog.allItemDefs.ToList())
                {
                    if (!item) return;
                    var result = Blacklist_Filter(item.itemIndex);
                    bool defaultResult;
                    if (item && (item.tier == ItemTier.Lunar || item.tier >= ItemTier.NoTier)) defaultResult = false;
                    else defaultResult = Inventory.defaultItemCopyFilterDelegate(item.itemIndex);
                }
            }
            catch (Exception e)
            {
                LemurFusionPlugin.LogWarning(e.Message);
                LemurFusionPlugin.LogWarning(e.StackTrace);
            }
        }
        #endregion
        /*
public class BodyEvolutionStage
{
    public string BodyName = "LemurianBody";
    public int EvolutionStage = -1;
}

public enum DeathPenalty
{
    TrueDeath,
    Devolve,
    ResetToBaby
}*/

        //internal static ConfigEntry<bool> Enable;
        //internal static ConfigEntry<int> EvoMax;
        //internal static ConfigEntry<int> OnDeathPenalty;
        //internal static ConfigEntry<bool> CapEvo;

        //internal static ConfigEntry<string> Elite_Blacklist_Raw;
        //internal static ConfigEntry<string> Evo_BodyStages_Raw;

        //internal static List<BodyEvolutionStage> Evo_BodyStages = [];
        //internal static List<EliteDef> Elite_Blacklist = [];

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
                        LemurFusionPlugin.LogWarning(string.Format("'{1}' Is being assigned to an invalid evolution stage, ignoring.", itemString[i]));
                    }
                }

                var Evo_BaseMaster = MasterCatalog.FindMasterPrefab(DevotionTweaks.masterPrefabName);
                if (Evo_BaseMaster)
                {
                    LemurFusionPlugin.LogInfo("Set [" + Evo_BaseMaster.name + "] as base master form.");
                }
                else
                {
                    LemurFusionPlugin.LogWarning(string.Format("Could not find master for [{0}] this will cause errors.", DevotionTweaks.masterPrefabName));
                }

                CharacterMaster master = Evo_BaseMaster.GetComponent<CharacterMaster>();
                if (master)
                {
                    GameObject bodyPrefab = master.bodyPrefab;
                    if (bodyPrefab)
                    {
                        LemurFusionPlugin.LogInfo("Base Master has [" + bodyPrefab.name + "] as base body form.");
                    }
                }
            }
        }*/
    }
}
