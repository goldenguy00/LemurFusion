using LemurFusion;
using LemurFusion.Config;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AddressableAssets;
using UnityEngine;
using System.Runtime.CompilerServices;
using LemurFusion.AI;

public class BetterLemurController : DevotedLemurianController
{
    #region Lemur Instance
    public SortedList<ItemIndex, int> _devotedItemList { get; set; } = [];
    public SortedList<ItemIndex, int> _untrackedItemList { get; set; } = [];

    public int FusionCount
    {
        get
        {
            if (!base.LemurianInventory) return 0;
            return base.LemurianInventory.GetItemCount(CU8Content.Items.LemurianHarness);
        }
        set
        {
            Utils.SetItem(_untrackedItemList, CU8Content.Items.LemurianHarness, value);
            //StatHooks.ResizeBody(_untrackedItemList[CU8Content.Items.LemurianHarness.itemIndex], base.LemurianBody);
        }
    }

    public void ReturnItems()
    {
        // mechanics
        if (base.LemurianInventory)
        {
            if (_untrackedItemList != null)
            {
                foreach (var item in _untrackedItemList)
                {
                    base.LemurianInventory.GiveItem(item.Key, item.Value);
                }
            }

            if (!DevotionTweaks.EnableSharedInventory && _devotedItemList != null)
            {
                foreach (var item in _devotedItemList)
                {
                    base.LemurianInventory.GiveItem(item.Key, item.Value);
                }
            }
        }
    }

    private PickupIndex FindPickupIndex(ItemIndex itemIndex, DevotionTweaks.DeathItem dropType)
    {
        if (itemIndex != ItemIndex.None)
        {
            switch (dropType)
            {
                // this is a warcrime
                case DevotionTweaks.DeathItem.Scrap:
                {
                    return ItemCatalog.GetItemDef(itemIndex).tier switch
                    {
                        ItemTier.Tier1 => PickupCatalog.FindPickupIndex("ItemIndex.ScrapWhite"),
                        ItemTier.Tier2 => PickupCatalog.FindPickupIndex("ItemIndex.ScrapGreen"),
                        ItemTier.Tier3 => PickupCatalog.FindPickupIndex("ItemIndex.ScrapRed"),
                        ItemTier.Boss => PickupCatalog.FindPickupIndex("ItemIndex.ScrapYellow"),
                        _ => PickupIndex.none,
                    };
                }

                case DevotionTweaks.DeathItem.Original:
                    return PickupCatalog.FindPickupIndex(itemIndex);

                case DevotionTweaks.DeathItem.Custom:
                    if (ConfigExtended.DeathDrops_TierToItem_Map.TryGetValue(ItemCatalog.GetItemDef(itemIndex).tier, out var idx) && idx != ItemIndex.None)
                        return PickupCatalog.FindPickupIndex(idx);
                    break;
            }
        }
        return PickupIndex.none;
    }

    private void DropScrapOnDeath()
    {
        var dropType = ConfigExtended.DeathDrop_ItemType.Value;
        foreach (var item in this._devotedItemList)
        {
            if (DevotionTweaks.EnableSharedInventory)
                this._devotionInventoryController.RemoveItem(item.Key, item.Value);

            if (dropType == DevotionTweaks.DeathItem.None)
                continue;

            var pickupIndex = FindPickupIndex(item.Key, dropType);
            if (pickupIndex != PickupIndex.none)
            {
                int dropCount = ConfigExtended.DeathDrop_DropAll.Value ? item.Value : 1;
                for (int i = 0; i < dropCount; i++)
                {
                    PickupDropletController.CreatePickupDroplet(pickupIndex, this.LemurianBody.corePosition, UnityEngine.Random.insideUnitCircle * 15f);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private void AddRiskyAllyItem()
    {
        Utils.SetItem(this._untrackedItemList, RiskyMod.Allies.AllyItems.AllyMarkerItem);
    }
    #endregion

    #region Hooks
    public static void InitHooks()
    {
        On.DevotedLemurianController.InitializeDevotedLemurian += DevotedLemurianController_InitializeDevotedLemurian;
        On.DevotedLemurianController.OnDevotedBodyDead += DevotedLemurianController_OnDevotedBodyDead;
        On.RoR2.CharacterMasterNotificationQueue.PushItemTransformNotification += CharacterMasterNotificationQueue_PushItemTransformNotification;
    }

    private static void CharacterMasterNotificationQueue_PushItemTransformNotification(
        On.RoR2.CharacterMasterNotificationQueue.orig_PushItemTransformNotification orig,
        CharacterMaster characterMaster, ItemIndex oldIndex, ItemIndex newIndex,
        CharacterMasterNotificationQueue.TransformationType transformationType)
    {
        orig(characterMaster, oldIndex, newIndex, transformationType);

        if (characterMaster.hasAuthority) 
            return;

        if (characterMaster.name == DevotionTweaks.masterCloneName && 
            characterMaster.TryGetComponent<BetterLemurController>(out var lemCtrl))
        {
            if (lemCtrl.LemurianInventory && lemCtrl._devotedItemList.TryGetValue(oldIndex, out var oldDevotionCount))
            {
                if (transformationType == CharacterMasterNotificationQueue.TransformationType.ContagiousVoid)
                {
                    if (DevotionTweaks.EnableSharedInventory)
                        lemCtrl._devotionInventoryController.RemoveItem(oldIndex, oldDevotionCount);
                    Utils.AddItem(lemCtrl._devotedItemList, newIndex, oldDevotionCount);
                    Utils.SetItem(lemCtrl._devotedItemList, oldIndex, 0);
                }
                else
                {
                    if (DevotionTweaks.EnableSharedInventory)
                        lemCtrl._devotionInventoryController.RemoveItem(oldIndex);
                    Utils.AddItem(lemCtrl._devotedItemList, newIndex);
                    Utils.RemoveItem(lemCtrl._devotedItemList, oldIndex);
                }


                lemCtrl._devotionInventoryController.UpdateAllMinions(false);
            }
        }
    }

    private static void DevotedLemurianController_InitializeDevotedLemurian(On.DevotedLemurianController.orig_InitializeDevotedLemurian orig,
        DevotedLemurianController self, ItemIndex itemIndex, DevotionInventoryController devInvCtrl)
    {
        orig(self, itemIndex, devInvCtrl);

        if (self is BetterLemurController lemCtrl)
        {
            lemCtrl._leashDistSq = PluginConfig.teleportDistance.Value * PluginConfig.teleportDistance.Value;

            if (PluginConfig.cloneReplacesRevive.Value)
            {
                if (itemIndex == RoR2Content.Items.ExtraLife.itemIndex)
                {
                    CreateTwin_ExtraLife(lemCtrl.FusionCount, lemCtrl.DevotedEvolutionLevel, RoR2Content.Items.ScrapRed.itemIndex,
                        self.gameObject.transform.position, self.gameObject.transform.rotation, devInvCtrl);
                    itemIndex = RoR2Content.Items.ExtraLifeConsumed.itemIndex;
                }
                else if (itemIndex == DLC1Content.Items.ExtraLifeVoid.itemIndex)
                {
                    CreateTwin_ExtraLife(lemCtrl.FusionCount, lemCtrl.DevotedEvolutionLevel, DLC1Content.Items.BleedOnHitVoid.itemIndex,
                        self.gameObject.transform.position, self.gameObject.transform.rotation, devInvCtrl);
                    itemIndex = DLC1Content.Items.ExtraLifeVoidConsumed.itemIndex;
                }
            }

            lemCtrl._devotedItemList ??= [];
            Utils.AddItem(lemCtrl._devotedItemList, itemIndex);

            lemCtrl._untrackedItemList ??= [];
            if (!lemCtrl._untrackedItemList.Any())
            {
                Utils.SetItem(lemCtrl._untrackedItemList, CU8Content.Items.LemurianHarness);
                Utils.SetItem(lemCtrl._untrackedItemList, RoR2Content.Items.MinionLeash);
                Utils.SetItem(lemCtrl._untrackedItemList, RoR2Content.Items.UseAmbientLevel);
                Utils.SetItem(lemCtrl._untrackedItemList, RoR2Content.Items.TeleportWhenOob);
                if (LemurFusionPlugin.riskyInstalled)
                {
                    lemCtrl.AddRiskyAllyItem();
                }
            }
        }
    }

    private static void CreateTwin_ExtraLife(int fusionCount, int evoCount, ItemIndex devotedItem, Vector3 position, Quaternion rotation, DevotionInventoryController devotionInventoryController)
    {
        if (devotionInventoryController)
        {
            CharacterMaster ownerMaster = devotionInventoryController._summonerMaster;
            if (ownerMaster)
            {
                CharacterBody ownerBody = ownerMaster.GetBody();
                if (ownerBody)
                {
                    MasterSummon masterSummon = new MasterSummon
                    {
                        masterPrefab = DevotionTweaks.masterPrefab,
                        position = position,
                        rotation = rotation,
                        summonerBodyObject = ownerBody.gameObject,
                        ignoreTeamMemberLimit = true,
                        useAmbientLevel = true
                    };
                    CharacterMaster twinMaster = masterSummon.Perform();

                    if (twinMaster && twinMaster.TryGetComponent<BetterLemurController>(out var lemCtrl))
                    {
                        lemCtrl.InitializeDevotedLemurian(devotedItem, devotionInventoryController);
                        if (fusionCount != 0) lemCtrl.FusionCount = fusionCount;
                        lemCtrl.DevotedEvolutionLevel = evoCount;
                        if (evoCount > 1) lemCtrl._lemurianMaster.TransformBody("LemurianBruiserBody");

                        devotionInventoryController.UpdateAllMinions(false);
                    }
                }
            }
        }
    }

    private static void DevotedLemurianController_OnDevotedBodyDead(On.DevotedLemurianController.orig_OnDevotedBodyDead orig, DevotedLemurianController self)
    {
        if (self is not BetterLemurController lemCtrl)
        {
            orig(self);
            return;
        }
        if (!lemCtrl || !lemCtrl._lemurianMaster)
            return;

        bool killYourSelf = !lemCtrl._lemurianMaster.IsInvoking("RespawnExtraLife") && !lemCtrl._lemurianMaster.IsInvoking("RespawnExtraLife");
        /*
        if (DevotionTweaks.EnableSharedInventory)
        {
            if (lemCtrl._devotionInventoryController.HasItem(RoR2Content.Items.ExtraLife))
            {
                lemCtrl._devotionInventoryController.RemoveItem(RoR2Content.Items.ExtraLife.itemIndex);
            }
            else if (lemCtrl._devotionInventoryController.HasItem(DLC1Content.Items.ExtraLifeVoid))
            {
                lemCtrl._devotionInventoryController.RemoveItem(DLC1Content.Items.ExtraLifeVoid.itemIndex);
            }
        }*/

        if (killYourSelf)
        {
            lemCtrl._lemurianMaster.destroyOnBodyDeath = true;
            lemCtrl.DropScrapOnDeath();

            if (ConfigExtended.DeathDrop_DropEgg.Value)
                PlaceDevotionEgg(lemCtrl.LemurianBody.footPosition);

            UnityEngine.Object.Destroy(lemCtrl._lemurianMaster.gameObject, 1f);
        }
        if (lemCtrl._devotionInventoryController)
            lemCtrl._devotionInventoryController.UpdateAllMinions(false);

        // not a fan of doing this but fuck it, the vanilla class is giga hard coded
        // id essentially just have to ILModify it to do literally nothing anyways.
        //
        // fuck you gearbox.
        //
        // orig(self);
    }

    private static void PlaceDevotionEgg(Vector3 spawnLoc)
    {
        if (!Run.instance || !DirectorCore.instance) return;
        if (Physics.Raycast(spawnLoc + Vector3.up * 1f, Vector3.down, out var raycastHit, float.PositiveInfinity, LayerIndex.world.mask))
        {
            DirectorPlacementRule placementRule = new()
            {
                placementMode = DirectorPlacementRule.PlacementMode.Direct,
                position = raycastHit.point
            };
            DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(Addressables.LoadAssetAsync<SpawnCard>
                ("RoR2/CU8/LemurianEgg/iscLemurianEgg.asset").WaitForCompletion(), placementRule, new Xoroshiro128Plus(0UL)));
        }
    }
    #endregion
}
