using BepInEx.Configuration;
using JetBrains.Annotations;
using RoR2;
using RoR2.CharacterAI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
        internal static ConfigEntry<DeathItem> ItemDrop_Type;

        internal static ConfigEntry<string> Blacklisted_ItemTiers_Raw;
        internal static ConfigEntry<string> Blacklisted_Items_Raw;
        internal static ConfigEntry<string> DeathDrops_TierToItem_Map_Raw;

        internal static List<ItemTier> Blacklisted_ItemTiers;
        internal static List<ItemIndex> Blacklisted_Items;
        internal static SortedList<ItemTier, ItemIndex> DeathDrops_TierToItem_Map;

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

        internal static void PostLoad()
        {
            LemurFusionPlugin._logger.LogInfo("Light PostLoad");
            if (Blacklist_Enable.Value)
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

                Blacklisted_ItemTiers = [.. from tier in Blacklisted_ItemTiers_Raw.Value.Split(',')
                                            let def = ItemTierCatalog.FindTierDef(tier.Trim())
                                            where def
                                            select def.tier];


                // just for the fun of it
                var itemPairList = DeathDrops_TierToItem_Map_Raw.Value
                    .Split(';')
                    .Select(s => s.Split(','))
                    .Select(s => 
                        (ItemTierCatalog.FindTierDef(s[0].Trim()),
                         ItemCatalog.FindItemIndex(s[1].Trim())
                         ));

                foreach (var (tierDef, itemIndex) in itemPairList)
                {
                    if (tierDef && itemIndex != ItemIndex.None)
                    {
                        if (!Blacklisted_ItemTiers.Contains(tierDef.tier) &&
                            !Blacklisted_Items.Contains(itemIndex))
                        DeathDrops_TierToItem_Map[tierDef.tier] = itemIndex;
                    }
                    else
                    {
                        LemurFusionPlugin._logger.LogWarning($"{LemurFusionPlugin.PluginName}Could not find (TierDef, ItemDef) pair '({tierDef?.name},{itemIndex})' for Custom Drop List.");
                    }
                }
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
