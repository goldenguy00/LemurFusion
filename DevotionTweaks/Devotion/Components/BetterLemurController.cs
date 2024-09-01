using LemurFusion;
using LemurFusion.Config;
using RoR2;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using LemurFusion.Devotion;
using LemurFusion.Devotion.Components;
using UnityEngine.AddressableAssets;
using UnityEngine;

public class BetterLemurController : DevotedLemurianController
{
    #region Lemur Instance
    public SortedList<ItemIndex, int> _devotedItemList { get; set; } = [];
    public SortedList<ItemIndex, int> _untrackedItemList { get; set; } = [];
    
    public BetterInventoryController BetterInventoryController => base._devotionInventoryController as BetterInventoryController;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public void AddRiskyAllyItem()
    {
        var item = RiskyMod.Allies.AllyItems.AllyMarkerItem.itemIndex;
        Utils.SetItem(this._untrackedItemList, item);
    }

    public void KillYourSelf()
    {
        if (DevotionTweaks.instance.EnableSharedInventory)
            this.BetterInventoryController.RemoveSharedItemsFromFriends(this._devotedItemList);

        var dropType = ConfigExtended.DeathDrop_ItemType.Value;
        if (dropType != DevotionTweaks.DeathItem.None)
        {
            foreach (var item in this._devotedItemList)
            {
                var pickupIndex = FindPickupIndex(item.Key, dropType);
                if (pickupIndex != PickupIndex.none)
                {
                    var dropCount = ConfigExtended.DeathDrop_DropAll.Value ? item.Value : 1;
                    for (var i = 0; i < dropCount; i++)
                    {
                        PickupDropletController.CreatePickupDroplet(pickupIndex, this.LemurianBody.corePosition, UnityEngine.Random.insideUnitCircle * 15f);
                    }
                }
            }
        }

        if (ConfigExtended.DeathDrop_DropEgg.Value && Physics.Raycast(this.LemurianBody.corePosition,
            Vector3.down, out var raycastHit, float.PositiveInfinity, LayerIndex.world.mask))
        {
            DirectorPlacementRule placementRule = new()
            {
                placementMode = DirectorPlacementRule.PlacementMode.Direct,
                position = raycastHit.point
            };
            DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(Addressables.LoadAssetAsync<SpawnCard>
                ("RoR2/CU8/LemurianEgg/iscLemurianEgg.asset").WaitForCompletion(), placementRule, new Xoroshiro128Plus(0UL)));
        }

        Destroy(this._lemurianMaster.gameObject, 1f);
    }

    public PickupIndex FindPickupIndex(ItemIndex itemIndex, DevotionTweaks.DeathItem dropType)
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
    #endregion
}
