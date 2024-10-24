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

public class BetterLemurController : DevotedLemurianController
{
    #region Lemur Instance
    public BetterInventoryController BetterInventoryController => base._devotionInventoryController as BetterInventoryController;
    public new Inventory LemurianInventory => this._lemurianMaster ? this._lemurianMaster.inventory : null;
    public new CharacterBody LemurianBody => this._lemurianMaster ? this._lemurianMaster.GetBody() : null;
    public Inventory PersonalInventory { get; set; }

    public void InitializeDevotedLemurian()
    {
        this._leashDistSq = PluginConfig.teleportDistance.Value * PluginConfig.teleportDistance.Value;

        if (!this.PersonalInventory)
            this.PersonalInventory = this._lemurianMaster.GetComponents<Inventory>().Last();

        if (LemurianInventory)
        {
            var inv = this._lemurianMaster.inventory;
            if (inv.GetItemCount(CU8Content.Items.LemurianHarness) == 0)
                inv.AddItemsFrom(this.BetterInventoryController._devotionMinionInventory, ConfigExtended.Blacklist_Filter);

            ShareItem(this.DevotionItem);

            inv.GiveItem(this.DevotionItem);
            inv.GiveItem(CU8Content.Items.LemurianHarness.itemIndex);
            inv.ResetItem(RoR2Content.Items.MinionLeash.itemIndex, 1);
            inv.ResetItem(RoR2Content.Items.UseAmbientLevel.itemIndex, 1);
            inv.ResetItem(RoR2Content.Items.TeleportWhenOob.itemIndex, 1);
            if (LemurFusionPlugin.riskyInstalled)
                AddRiskyAllyItem();
        }
    }

    public void ShareItem(ItemIndex item)
    {
        if (this.PersonalInventory)
            this.PersonalInventory.GiveItem(item);

        if (PluginConfig.enableSharedInventory.Value)
        {
            foreach (var friend in this.BetterInventoryController.GetFriends(this))
            {
                friend.LemurianInventory.GiveItem(item);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public void AddRiskyAllyItem()
    {
        LemurianInventory.ResetItem(RiskyMod.Allies.AllyItems.AllyMarkerItem.itemIndex, 1);
    }

    public void KillYourSelf()
    {
        bool sharing = DevotionTweaks.instance.EnableSharedInventory && this.BetterInventoryController;
        var friends = sharing ? this.BetterInventoryController.GetFriends() : [];

        if (this.PersonalInventory)
        {
            foreach (var item in this.PersonalInventory.itemAcquisitionOrder.ToList())
            {
                var count = this.PersonalInventory.GetItemCount(item);
                if (count <= 0)
                    continue;

                if (sharing)
                {
                    this.BetterInventoryController.RemoveItem(item, System.Math.Min(count, this.BetterInventoryController._devotionMinionInventory.GetItemCount(item)));
                    foreach (var friend in friends)
                    {
                        friend.LemurianInventory.RemoveItem(item, System.Math.Min(count, friend.LemurianInventory.GetItemCount(item)));
                    }
                }

                var pickupIndex = FindPickupIndex(item);
                if (pickupIndex != PickupIndex.none)
                {
                    var dropCount = ConfigExtended.DeathDrop_DropAll.Value ? count : 1;
                    for (var i = 0; i < dropCount; i++)
                    {
                        PickupDropletController.CreatePickupDroplet(pickupIndex, this.LemurianBody.corePosition, Random.insideUnitCircle * 15f);
                    }
                }
            }
        }

        if (ConfigExtended.DeathDrop_DropEgg.Value && this.LemurianBody && Physics.Raycast(this.LemurianBody.corePosition,
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
