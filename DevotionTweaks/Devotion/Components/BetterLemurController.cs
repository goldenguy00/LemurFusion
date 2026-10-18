using LemurFusion;
using LemurFusion.Config;
using RoR2;
using LemurFusion.Devotion;
using LemurFusion.Devotion.Components;
using UnityEngine.AddressableAssets;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HG;

public class BetterLemurController : DevotedLemurianController
{
    #region Lemur Instance
    public BetterInventoryController BetterInventoryController => base._devotionInventoryController as BetterInventoryController;
    public int FusionCount => LemurianInventory.GetItemCountPermanent(CU8Content.Items.LemurianHarness);
    public CharacterMaster LemurianMaster => this._lemurianMaster;

    public new Inventory LemurianInventory { get; private set; }
    public Inventory PersonalInventory { get; private set; }

    private void Awake()
    {
        this._lemurianMaster = GetComponent<CharacterMaster>();
        this.LemurianInventory = GetComponent<Inventory>();
        this.PersonalInventory = GetComponents<Inventory>().Last();
    }

    public void InitializeDevotedLemurian()
    {
        this._leashDistSq = PluginConfig.teleportDistance.Value * PluginConfig.teleportDistance.Value;

        if (this.FusionCount == 0)
            this.BetterInventoryController.ChannelSharedItems(this);

        this.BetterInventoryController.ShareInitialItem(this, this.DevotionItem);

        LemurianInventory.GiveItemPermanent(CU8Content.Items.LemurianHarness.itemIndex);
        LemurianInventory.ResetItem(RoR2Content.Items.MinionLeash.itemIndex, 1);
        LemurianInventory.ResetItem(RoR2Content.Items.UseAmbientLevel.itemIndex, 1);
        LemurianInventory.ResetItem(RoR2Content.Items.TeleportWhenOob.itemIndex, 1);
        //if (LemurFusionPlugin.riskyInstalled)
        //    AddRiskyAllyItem();
    }

    //[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    //public void AddRiskyAllyItem()
    //{
    //    LemurianInventory.ResetItem(RiskyMod..AllyMarkerItem.itemIndex, 1);
    //}

    public void KillYourSelf()
    {
        using var _0 = ListPool<ItemIndex>.RentCollection(out var itemStacks);
        this.PersonalInventory.permanentItemStacks.GetNonZeroIndices(itemStacks);
        foreach (var item in itemStacks)
        {
            var pickupIndex = FindPickupIndex(item);
            if (pickupIndex != PickupIndex.none)
            {
                var dropCount = ConfigExtended.DeathDrop_DropAll.Value ? this.PersonalInventory.GetItemCountPermanent(item) : 1;
                for (var i = 0; i < dropCount; i++)
                {
                    PickupDropletController.CreatePickupDroplet(new UniquePickup(pickupIndex), this.LemurianBody.corePosition, Random.insideUnitCircle * 15f, false);
                }
            }
        }

        BetterInventoryController.UnChannelSharedItems(this);

        if (ConfigExtended.DeathDrop_DropEgg.Value && this.LemurianBody && Physics.Raycast(this.LemurianBody.corePosition,
            Vector3.down, out var raycastHit, float.PositiveInfinity, LayerIndex.world.mask))
        {
            DirectorPlacementRule placementRule = new()
            {
                placementMode = DirectorPlacementRule.PlacementMode.Direct,
                position = raycastHit.point
            };
            DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(
                Addressables.LoadAssetAsync<SpawnCard>("RoR2/CU8/LemurianEgg/iscLemurianEgg.asset").WaitForCompletion(), 
                placementRule, new Xoroshiro128Plus(0UL)));
        }

        if (this._lemurianMaster)
            GameObject.Destroy(this._lemurianMaster.gameObject, 1f);
    }

    public PickupIndex FindPickupIndex(ItemIndex itemIndex)
    {
        if (itemIndex != ItemIndex.None && this.LemurianBody)
        {
            switch (ConfigExtended.DeathDrop_ItemType.Value)
            {
                // this is a warcrime
                case DevotionTweaks.DeathItem.Scrap:
                    return ItemCatalog.GetItemDef(itemIndex).tier switch
                    {
                        ItemTier.Tier1 => PickupCatalog.FindPickupIndex("ItemIndex.ScrapWhite"),
                        ItemTier.Tier2 => PickupCatalog.FindPickupIndex("ItemIndex.ScrapGreen"),
                        ItemTier.Tier3 => PickupCatalog.FindPickupIndex("ItemIndex.ScrapRed"),
                        ItemTier.Boss => PickupCatalog.FindPickupIndex("ItemIndex.ScrapYellow"),
                        _ => PickupIndex.none,
                    };

                case DevotionTweaks.DeathItem.Original:
                    return PickupCatalog.FindPickupIndex(itemIndex);

                case DevotionTweaks.DeathItem.Custom:
                    var itemDef = ItemCatalog.GetItemDef(itemIndex);
                    if (itemDef && ConfigExtended.DeathDrops_TierToItem_Map.TryGetValue(itemDef.tier, out var idx) && idx != ItemIndex.None)
                        return PickupCatalog.FindPickupIndex(idx);
                    break;
            }
        }
        return PickupIndex.none;
    }
    #endregion
}
