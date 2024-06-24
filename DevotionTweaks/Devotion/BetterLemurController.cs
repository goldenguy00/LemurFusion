using LemurFusion;
using LemurFusion.Config;
using RoR2;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using LemurFusion.Devotion.Tweaks;

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

    public void DropScrapOnDeath()
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
    public void AddRiskyAllyItem()
    {
        Utils.SetItem(this._untrackedItemList, RiskyMod.Allies.AllyItems.AllyMarkerItem);
    }
    #endregion
}
