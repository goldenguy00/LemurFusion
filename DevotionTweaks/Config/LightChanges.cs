using BepInEx.Configuration;
using RoR2;
using RoR2.CharacterAI;
using System.Collections.Generic;
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

        internal static string BlackList_TierList_Raw;
        internal static string BlackList_ItemList_Raw;
        internal static string ItemDrop_CustomDropList_Raw;

        internal static bool Blacklist_Enable = false;
        internal static bool BlackList_Filter_CannotCopy = true;
        internal static bool BlackList_Filter_Scrap = true;
        internal static List<ItemTier> BlackList_TierList;
        internal static List<ItemDef> BlackList_ItemList;
        internal static bool FilterItems = false;
        internal static bool FilterTiers = false;

        internal static bool ItemDrop_Enable = false;
        internal static ItemIndex[] ItemDrop_CustomDropList;
        internal static ConfigEntry<DeathItem> ItemDrop_Type;

        internal static bool Misc_FixEvo = true;

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
                itemDef.tags = [.. itemDef.tags, ItemTag.BrotherBlacklist, ItemTag.CannotSteal];
            }
        }

        internal static void PostLoad()
        {
            if (Blacklist_Enable)
            {
                BlackList_ItemList = new List<ItemDef>();
                string[] itemString = BlackList_ItemList_Raw.Split(',');
                for (int i = 0; i < itemString.Length; i++)
                {
                    ItemIndex itemIndex = ItemCatalog.FindItemIndex(itemString[i].Trim());
                    if (itemIndex > ItemIndex.None)
                    {
                        ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                        if (itemDef)
                        {
                            BlackList_ItemList.Add(itemDef);
                            FilterItems = true;
                        }
                    }
                }

                BlackList_TierList = new List<ItemTier>();
                itemString = BlackList_TierList_Raw.Split(',');
                for (int i = 0; i < itemString.Length; i++)
                {

                    ItemTierDef itemTier = ItemTierCatalog.FindTierDef(itemString[i].Trim());
                    if (itemTier)
                    {
                        BlackList_TierList.Add(itemTier.tier);
                        FilterTiers = true;
                    }
                }

                ItemDrop_CustomDropList = new ItemIndex[ItemTierCatalog.allItemTierDefs.Length];
                for (int i = 0; i < ItemDrop_CustomDropList.Length; i++)
                {
                    ItemDrop_CustomDropList[i] = ItemIndex.None;
                }
                itemString = ItemDrop_CustomDropList_Raw.Split(',');
                for (int i = 0; i + 1 < itemString.Length; i += 2)
                {
                    ItemTierDef itemTier = ItemTierCatalog.FindTierDef(itemString[i].Trim());
                    if (itemTier)
                    {
                        int tierIndex = GetItemTierIndex(itemTier);
                        if (tierIndex > -1 && tierIndex < ItemDrop_CustomDropList.Length)
                        {
                            ItemIndex itemIndex = ItemCatalog.FindItemIndex(itemString[i + 1].Trim());
                            if (itemIndex > ItemIndex.None)
                            {
                                ItemDrop_CustomDropList[tierIndex] = itemIndex;
                            }
                        }
                        else
                        {
                            LemurFusionPlugin._logger.LogWarning(string.Format("{0}Could not find Tier '{1}' for Custom Drop List.", LemurFusionPlugin.PluginName, itemString[i].Trim()));
                        }
                    }
                }
            }
        }

        private static int GetItemTierIndex(ItemTierDef itemTier)
        {
            for (int i = 0; i < ItemTierCatalog.allItemTierDefs.Length; i++)
            {
                if (ItemTierCatalog.allItemTierDefs[i] == itemTier)
                {
                    return i;
                }
            }
            return -1;
        }

        private void Hooks()
        {
            if (Blacklist_Enable)
            {
                On.RoR2.PickupPickerController.SetOptionsFromInteractor += SetPickupPicker;
            }
            if (Misc_FixEvo)
            {
                On.RoR2.DevotionInventoryController.OnBossGroupDefeatedServer += (orig, group) => { };
                BossGroup.onBossGroupDefeatedServer += BossGroupDefeatedServer;
                On.RoR2.CharacterAI.LemurianEggController.CreateItemTakenOrb += CreateItemTakeOrb_Egg;
            }
        }

        private void BossGroupDefeatedServer(BossGroup group)
        {
            if (!SceneCatalog.GetSceneDefForCurrentScene().needSkipDevotionRespawn)
            {
                DevotionInventoryController.ActivateDevotedEvolution();
            }
        }

        private void CreateItemTakeOrb_Egg(On.RoR2.CharacterAI.LemurianEggController.orig_CreateItemTakenOrb orig, LemurianEggController self, Vector3 effectOrigin, GameObject targetObject, ItemIndex itemIndex)
        {
            // why the fuck are they nulling this out lmao
            DevotionInventoryController.s_effectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OrbEffects/ItemTakenOrbEffect");
            orig(self, effectOrigin, targetObject, itemIndex);
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
                    if (!FilterTiers || !BlackList_TierList.Contains(itemTier.tier))
                    {
                        if ((!BlackList_Filter_Scrap || !itemDef.ContainsTag(ItemTag.Scrap) && !itemDef.ContainsTag(ItemTag.PriorityScrap)) && (!BlackList_Filter_CannotCopy || !itemDef.ContainsTag(ItemTag.CannotCopy)))
                        {
                            if (!FilterItems || !BlackList_ItemList.Contains(itemDef))
                            {
                                list.Add(new PickupPickerController.Option
                                {
                                    available = true,
                                    pickupIndex = pickupIndex
                                });
                            }
                        }
                    }
                }
            }

            self.SetOptionsServer([.. list]);
        }
    }
}
