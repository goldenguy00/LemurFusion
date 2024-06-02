using LemurFusion;
using LemurFusion.Config;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AddressableAssets;
using UnityEngine;
using System.Runtime.CompilerServices;

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

    internal static void PlaceDevotionEgg(Vector3 spawnLoc)
    {
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
    private static void CreateTwin_ExtraLife(GameObject targetLocation, GameObject masterPrefab, DevotionInventoryController devotionInventoryController)
    {
        ItemIndex itemIndex = RoR2Content.Items.ExtraLifeConsumed.itemIndex;
        if (devotionInventoryController)
        {
            CharacterMaster ownerMaster = devotionInventoryController._summonerMaster;
            if (ownerMaster)
            {
                CharacterBody ownerBody = ownerMaster.GetBody();
                if (ownerBody)
                {
                    MasterSummon masterSummon = new()
                    {
                        masterPrefab = masterPrefab,
                        position = targetLocation.transform.position,
                        rotation = targetLocation.transform.rotation,
                        summonerBodyObject = ownerBody.gameObject,
                        ignoreTeamMemberLimit = true,
                        useAmbientLevel = true
                    };
                    CharacterMaster twinMaster = masterSummon.Perform();

                    if (twinMaster)
                    {
                        DevotedLemurianController devotedController = twinMaster.GetComponent<DevotedLemurianController>();
                        if (!devotedController)
                        {
                            devotedController = twinMaster.gameObject.AddComponent<DevotedLemurianController>();
                        }
                        devotedController.InitializeDevotedLemurian(itemIndex, devotionInventoryController);
                    }
                }
            }
        }
    }

    private static void CreateTwin_ExtraLifeVoid(GameObject targetLocation, GameObject masterPrefab, DevotionInventoryController devotionInventoryController)
    {
        ItemIndex itemIndex = DLC1Content.Items.BleedOnHitVoid.itemIndex;
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
                        masterPrefab = masterPrefab,
                        position = targetLocation.transform.position,
                        rotation = targetLocation.transform.rotation,
                        summonerBodyObject = ownerBody.gameObject,
                        ignoreTeamMemberLimit = true,
                        useAmbientLevel = true
                    };
                    CharacterMaster twinMaster = masterSummon.Perform();

                    if (twinMaster)
                    {
                        DevotedLemurianController devotedController = twinMaster.GetComponent<DevotedLemurianController>();
                        if (!devotedController)
                        {
                            devotedController = twinMaster.gameObject.AddComponent<DevotedLemurianController>();
                        }
                        devotedController.InitializeDevotedLemurian(itemIndex, devotionInventoryController);
                    }
                }
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

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static void DevotedLemurianController_InitializeDevotedLemurian(On.DevotedLemurianController.orig_InitializeDevotedLemurian orig,
        DevotedLemurianController self, ItemIndex itemIndex, DevotionInventoryController devInvCtrl)
    {
        orig(self, itemIndex, devInvCtrl);
        /*
        if (itemIndex == RoR2Content.Items.ExtraLife.itemIndex)
        {
            CreateTwin_ExtraLife(self.gameObject, DevotionTweaks.masterPrefab, devInvCtrl);
            itemIndex = RoR2Content.Items.ScrapRed.itemIndex;
        }
        else if (itemIndex == DLC1Content.Items.ExtraLifeVoid.itemIndex)
        {
            CreateTwin_ExtraLifeVoid(self.gameObject, DevotionTweaks.masterPrefab, devInvCtrl);
            itemIndex = DLC1Content.Items.BearVoid.itemIndex;
        }*/

        if (self is BetterLemurController lemCtrl)
        {
            lemCtrl._leashDistSq = PluginConfig.teleportDistance.Value * PluginConfig.teleportDistance.Value;

            lemCtrl._devotedItemList ??= [];
            AddItem(lemCtrl._devotedItemList, itemIndex);

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
        if (self is not BetterLemurController lemCtrl)
        {
            orig(self);
            return;
        }

        lemCtrl.CommitSudoku();

        // not a fan of doing this but fuck it, the vanilla class is giga hard coded
        // id essentially just have to ILModify it to do literally nothing anyways.
        //
        // fuck you gearbox.
        //
        // orig(self);
    }
    private PickupIndex FindPickupIndex(ItemIndex itemIndex)
    {
        PickupIndex pickupIndex = PickupIndex.none;
        if (itemIndex != ItemIndex.None)
        {
            var itemDef = ItemCatalog.GetItemDef(itemIndex);

            if (itemDef && itemDef.itemIndex != ItemIndex.None)
            {
                if (LightChanges.DeathDrops_TierToItem_Map.TryGetValue(itemDef.tier, out var idx) && idx != ItemIndex.None)
                {
                    pickupIndex = PickupCatalog.FindPickupIndex(idx);
                }
            }
        }
        return pickupIndex;
    }

    private void DropScrapOnDeath()
    {
        foreach (var item in this._devotedItemList)
        {
            var pickupIndex = FindPickupIndex(item.Key);
            if (pickupIndex != PickupIndex.none)
            {
                int dropCount = LightChanges.ItemDrop_DropAll.Value ? item.Value : 1;
                for (int i = 0; i < dropCount; i++)
                {
                    PickupDropletController.CreatePickupDroplet(pickupIndex, this.LemurianBody.corePosition, UnityEngine.Random.insideUnitCircle * 15f);
                }

                this._devotionInventoryController.RemoveItem(item.Key, item.Value);
            }
        }
    }

    private void DevolveLem(int newEvoLvl)
    {
        // adjust body stage
        this.MeldCount = newEvoLvl;
        base.DevotedEvolutionLevel = newEvoLvl;
    }

    private void CommitSudoku()
    {
        if (base._lemurianMaster.IsDeadAndOutOfLivesServer())
        {
            bool shouldDie;
            switch ((HeavyChanges.DeathPenalty)HeavyChanges.OnDeathPenalty.Value)
            {
                case HeavyChanges.DeathPenalty.Devolve:
                    //shouldDie = base.DevotedEvolutionLevel < 1;
                    //DevolveLem(base.DevotedEvolutionLevel - 1);
                    //break;
                case HeavyChanges.DeathPenalty.ResetToBaby:
                    //shouldDie = base.DevotedEvolutionLevel < 1;
                    //DevolveLem(0);
                    //break;
                case HeavyChanges.DeathPenalty.TrueDeath:
                default:
                    shouldDie = true;
                    break;
            }

            if (shouldDie)
            {
                base._lemurianMaster.destroyOnBodyDeath = true;
                this.DropScrapOnDeath();
                if (HeavyChanges.DropEggOnDeath.Value)
                {
                    PlaceDevotionEgg(this.LemurianBody.footPosition);
                }
                UnityEngine.Object.Destroy(this._lemurianMaster.gameObject, 1f);
            }
            else
            {
                this._lemurianMaster.Invoke("RespawnExtraLife", 2f);
            }
        }
        this._devotionInventoryController.UpdateAllMinions(false);
    }
    #endregion

    #region List Utils
    public static void AddItem(SortedList<ItemIndex, int> target, ItemDef itemDef, int count = 1)
    {
        if (!itemDef) return;
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
        if (!itemDef) return;
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
