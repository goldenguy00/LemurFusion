using DevotionTweaks;
using RoR2;
using System.Collections.Generic;
using System.Linq;

public class BetterLemurController : DevotedLemurianController
{
    #region Lemur Instance
    public SortedList<ItemIndex, int> _devotedItemList { get; set; } = [];
    public SortedList<ItemIndex, int> _untrackedItemList { get; set; } = [];

    public int MeldCount
    {
        get
        {
            if (base.LemurianInventory == null) return 0;
            return base.LemurianInventory.GetItemCount(CU8Content.Items.LemurianHarness);
        }
        set
        {
            if (base.LemurianInventory)
            {
                var heldItems = MeldCount;
                if (heldItems != value)
                {
                    AddItem(_untrackedItemList, CU8Content.Items.LemurianHarness, value - heldItems);
                    StatHooks.ResizeBody(_untrackedItemList[CU8Content.Items.LemurianHarness.itemIndex], base.LemurianBody);
                }
            }
        }
    }
    public int MultiplyStatsCount
    {
        get
        {
            // dont fucking question it
            return base.DevotedEvolutionLevel switch
            {
                0 or 2 => 10,
                1 => 20,
                _ => 17 + base.DevotedEvolutionLevel,
            };
        }
    }

    public void ReturnUntrackedItems()
    {
        // mechanics
        if (base.LemurianBody)
        {
            if (PluginConfig.disableFallDamage.Value)
                base.LemurianBody.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
        }

        if (base.LemurianInventory && _untrackedItemList.Any())
        {
            foreach (var item in _untrackedItemList)
            {
                base.LemurianInventory.GiveItem(item.Key, item.Value);
            }
        }
    }
    #endregion

    #region Hooks
    public static void InitHooks()
    {
        On.DevotedLemurianController.InitializeDevotedLemurian += DevotedLemurianController_InitializeDevotedLemurian;
        On.DevotedLemurianController.OnDevotedBodyDead += DevotedLemurianController_OnDevotedBodyDead;
    }

    private static void DevotedLemurianController_InitializeDevotedLemurian(On.DevotedLemurianController.orig_InitializeDevotedLemurian orig,
        DevotedLemurianController self, ItemIndex idx, DevotionInventoryController devInvCtrl)
    {
        orig(self, idx, devInvCtrl);

        if (self is BetterLemurController lemCtrl)
        {
            lemCtrl._leashDistSq = PluginConfig.teleportDistance.Value * PluginConfig.teleportDistance.Value;

            lemCtrl._devotedItemList ??= [];
            AddItem(lemCtrl._devotedItemList, idx);

            lemCtrl._untrackedItemList ??= [];
            SetItem(lemCtrl._untrackedItemList, RoR2Content.Items.MinionLeash);
            SetItem(lemCtrl._untrackedItemList, RoR2Content.Items.UseAmbientLevel);

            if (LemurFusionPlugin.riskyInstalled)
            {
                SetItem(lemCtrl._untrackedItemList, RiskyMod.Allies.AllyItems.AllyMarkerItem);
                SetItem(lemCtrl._untrackedItemList, RiskyMod.Allies.AllyItems.AllyRegenItem, 60);
            }
        }
    }

    private static void DevotedLemurianController_OnDevotedBodyDead(On.DevotedLemurianController.orig_OnDevotedBodyDead orig, DevotedLemurianController self)
    {
        if (self._devotionInventoryController.HasItem(RoR2Content.Items.ExtraLife) || self is not BetterLemurController lemCtrl)
        {
            orig(self);
            return;
        }

        foreach (var item in lemCtrl._devotedItemList)
        {
            ItemDef itemDef = ItemCatalog.GetItemDef(item.Key);
            if (itemDef && itemDef.itemIndex != ItemIndex.None && item.Key != lemCtrl.DevotionItem)
            {
                PickupIndex pickupIndex = PickupIndex.none;
                switch (itemDef.tier)
                {
                    case ItemTier.Tier1:
                        pickupIndex = PickupCatalog.FindPickupIndex("ItemIndex.ScrapWhite");
                        break;
                    case ItemTier.Tier2:
                        pickupIndex = PickupCatalog.FindPickupIndex("ItemIndex.ScrapGreen");
                        break;
                    case ItemTier.Tier3:
                        pickupIndex = PickupCatalog.FindPickupIndex("ItemIndex.ScrapRed");
                        break;
                    case ItemTier.Boss:
                        pickupIndex = PickupCatalog.FindPickupIndex("ItemIndex.ScrapYellow");
                        break;
                }
                if (pickupIndex != PickupIndex.none)
                {
                    PickupDropletController.CreatePickupDroplet(pickupIndex, lemCtrl.LemurianBody.corePosition, UnityEngine.Random.insideUnitCircle * 15f);

                    lemCtrl._devotionInventoryController.RemoveItem(item.Key, item.Value);
                }
            }
        }

        orig(self);
    }
    #endregion

    #region List Utils
    public static void AddItem(SortedList<ItemIndex, int> target, ItemDef itemDef, int count = 1)
    {
        AddItem(target, itemDef.itemIndex, count);
    }

    public static void AddItem(SortedList<ItemIndex, int> target, ItemIndex itemIndex, int count = 1)
    {
        if (itemIndex == ItemIndex.None) return;

        target ??= [];
        if (target.ContainsKey(itemIndex))
            target[itemIndex] += count;
        else
            target.Add(itemIndex, count);
    }

    public static void SetItem(SortedList<ItemIndex, int> target, ItemDef itemDef, int count = 1)
    {
        SetItem(target, itemDef.itemIndex, count);
    }

    public static void SetItem(SortedList<ItemIndex, int> target, ItemIndex itemIndex, int count = 1)
    {
        if (itemIndex == ItemIndex.None) return;

        target ??= [];
        if (target.ContainsKey(itemIndex))
            target[itemIndex] = count;
        else
            target.Add(itemIndex, count);
    }
    #endregion
}
