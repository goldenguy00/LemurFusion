using BepInEx.Configuration;
using RoR2;
using RoR2.CharacterAI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AddressableAssets;

namespace LemurFusion.Config
{
    public class LightChanges
    {
        public enum DeathItem
        {
            None,
            Scrap,
            Original,
            Custom
        }

        internal static ConfigEntry<bool> Blacklist_Enable;
        internal static ConfigEntry<bool> Blacklist_Filter_CannotCopy;
        internal static ConfigEntry<bool> Blacklist_Filter_Scrap;

        internal static ConfigEntry<bool> ItemDrop_Enable;
        internal static ConfigEntry<int> ItemDrop_Type;
        internal static ConfigEntry<bool> ItemDrop_DropAll;

        internal static ConfigEntry<string> Blacklisted_ItemTiers_Raw;
        internal static ConfigEntry<string> Blacklisted_Items_Raw;
        internal static ConfigEntry<string> DeathDrops_TierToItem_Map_Raw;

        internal static HashSet<ItemTier> Blacklisted_ItemTiers = [];
        internal static HashSet<ItemIndex> Blacklisted_Items = [];
        internal static SortedList<ItemTier, ItemIndex> DeathDrops_TierToItem_Map = [];

        public LightChanges()
        {
            UpdateItemDef();
            Hooks();
        }

        private void UpdateItemDef()
        {
            //Fix up the tags on the Harness
            LemurFusionPlugin._logger.LogInfo("Changing Lemurian Harness");
            ItemDef itemDef = Addressables.LoadAssetAsync<ItemDef>("RoR2/CU8/Harness/items.LemurianHarness.asset").WaitForCompletion();
            if (itemDef)
            {
                itemDef.tags = [.. itemDef.tags, ItemTag.BrotherBlacklist, ItemTag.CannotSteal, ItemTag.CannotCopy];
            }
        }

        private static void BLItems()
        {
            try
            {
            // just to get familiar with this syntax... move along.
            Blacklisted_Items = [.. from item in Blacklisted_Items_Raw.Value.Split(',')
                                        let idx = ItemCatalog.FindItemIndex(item.Trim())
                                        where idx != ItemIndex.None && ItemCatalog.IsIndexValid(idx)
                                        select ItemCatalog.GetItemDef(idx) into def
                                        where def && def.DoesNotContainTag(ItemTag.AIBlacklist)
                                                  && (!Blacklist_Filter_Scrap.Value || (def.DoesNotContainTag(ItemTag.Scrap) && def.DoesNotContainTag(ItemTag.PriorityScrap)))
                                                  && (!Blacklist_Filter_CannotCopy.Value || def.DoesNotContainTag(ItemTag.CannotCopy))
                                        select def.itemIndex];
            }
            catch (Exception e)
            {
                LemurFusionPlugin._logger.LogWarning(e.Message);
                LemurFusionPlugin._logger.LogWarning(e.StackTrace);
            }
        }

        private static void BLItemTiers()
        {
            try
            {
                Blacklisted_ItemTiers = [.. from tier in Blacklisted_ItemTiers_Raw.Value.Split(',')
                                            let def = ItemTierCatalog.FindTierDef(tier.Trim())
                                            where def
                                            select def.tier];
            }
            catch (Exception e)
            {
                LemurFusionPlugin._logger.LogWarning(e.Message);
                LemurFusionPlugin._logger.LogWarning(e.StackTrace);
            }
        }

        internal static void PostLoad()
        {
            LemurFusionPlugin._logger.LogInfo("Light PostLoad");
            DeathDrops_TierToItem_Map = [];
            DeathDrops_TierToItem_Map.Add(ItemTier.Tier1, RoR2Content.Items.ScrapWhite.itemIndex);
            DeathDrops_TierToItem_Map.Add(ItemTier.Tier2, RoR2Content.Items.ScrapGreen.itemIndex);
            DeathDrops_TierToItem_Map.Add(ItemTier.Tier3, RoR2Content.Items.ScrapRed.itemIndex);
            DeathDrops_TierToItem_Map.Add(ItemTier.Boss, RoR2Content.Items.ScrapYellow.itemIndex);
            try
            {
                if (Blacklist_Enable.Value)
                {
                    BLItems();
                    BLItemTiers();

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
                            if (!Blacklisted_ItemTiers.Contains(tierDef.tier))
                            {
                                if (DeathDrops_TierToItem_Map.ContainsKey(tierDef.tier))
                                    LemurFusionPlugin._logger.LogWarning($"Overwriting duplicate {tierDef?.name} with (TierDef, ItemDef) pair '({tierDef?.name},{itemIndex})' for Custom Drop List.");
                                DeathDrops_TierToItem_Map[tierDef.tier] = itemIndex;
                            }
                            else
                                LemurFusionPlugin._logger.LogWarning($"Skipping Blacklisted (TierDef, ItemDef) pair '({tierDef?.name},{itemIndex})' for Custom Drop List.");
                        }
                        else
                        {
                            LemurFusionPlugin._logger.LogWarning($"Could not find (TierDef, ItemDef) pair '({tierDef?.name},{itemIndex})' for Custom Drop List.");
                        }
                    }
                }
            }catch (Exception e)
            {
                LemurFusionPlugin._logger.LogWarning(e.Message);
                LemurFusionPlugin._logger.LogWarning(e.StackTrace);
            }
        }

        private void Hooks()
        {
            if (Blacklist_Enable.Value)
            {
                On.RoR2.PickupPickerController.SetOptionsFromInteractor += SetPickupPicker;
            }
        }

        private void SetPickupPicker(On.RoR2.PickupPickerController.orig_SetOptionsFromInteractor orig, PickupPickerController self, Interactor activator)
        {
            if (!self.TryGetComponent<LemurianEggController>(out _) || !activator || !activator.TryGetComponent<CharacterBody>(out var body) || !body.inventory)
            {
                orig(self, activator);
                return;
            }

            List<PickupPickerController.Option> list = [];
            foreach (var itemIndex in body.inventory.itemAcquisitionOrder)
            {
                var itemDef = ItemCatalog.GetItemDef(itemIndex);
                var itemTier = ItemTierCatalog.GetItemTierDef(itemDef.tier);
                var pickupIndex = PickupCatalog.FindPickupIndex(itemIndex);

                if (pickupIndex != PickupIndex.none && itemTier && !itemDef.hidden && itemDef.canRemove)
                {
                    if (!Blacklisted_ItemTiers.Contains(itemTier.tier) && !Blacklisted_Items.Contains(itemIndex))
                    {
                        list.Add(new PickupPickerController.Option
                        {
                            available = true,
                            pickupIndex = pickupIndex
                        });
                    }
                }
            }

            self.SetOptionsServer([.. list]);
        }
    }
}
