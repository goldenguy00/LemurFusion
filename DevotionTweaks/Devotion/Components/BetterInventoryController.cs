using Grumpy.UnitTest;
using HG;
using LemurFusion.Config;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

namespace LemurFusion.Devotion.Components
{
    public class BetterInventoryController : DevotionInventoryController
    {

        #region Static Methods
        public static List<EquipmentIndex> gigaChadLvl = [];

        public static bool initialized { get; set; }

        public static void CreateEliteLists()
        {
            HashSet<EquipmentIndex> lowLvl = [];
            HashSet<EquipmentIndex> highLvl = [];
            HashSet<EquipmentIndex> gigaChad = [];

            // thank you moffein for showing me the way, fuck this inconsistent bs
            // holy shit this is horrible i hate it i hate it i hate it
            foreach (var etd in (EliteAPI.GetCombatDirectorEliteTiers() ?? []).ToList())
            {
                if (etd != null && etd.eliteTypes.Length > 0)
                {
                    if (etd.eliteTypes.Any(ed => ed != null && ed.name.Contains("Honor")))
                        continue;

                    var isT2 = false;
                    var isT1 = false;
                    foreach (var ed in etd.eliteTypes)
                    {
                        if (!ed || !ed.eliteEquipmentDef)
                            continue;

                        if (ed.eliteEquipmentDef == RoR2Content.Equipment.AffixPoison || ed.eliteEquipmentDef == RoR2Content.Equipment.AffixLunar)
                        {
                            isT2 = true;
                            break;
                        }
                        else if (ed.eliteEquipmentDef == RoR2Content.Equipment.AffixBlue)
                        {
                            isT1 = true;
                            break;
                        }
                    }

                    if (isT1 || isT2)
                    {
                        foreach (var ed in etd.eliteTypes)
                        {
                            if (!ed || !ed.eliteEquipmentDef ||
                                ed.eliteEquipmentDef.equipmentIndex == EquipmentIndex.None ||
                                !ed.eliteEquipmentDef.passiveBuffDef ||
                                !ed.eliteEquipmentDef.passiveBuffDef.isElite) continue;

                            if (isT1)
                            {
                                lowLvl.Add(ed.eliteEquipmentDef.equipmentIndex);
                                highLvl.Add(ed.eliteEquipmentDef.equipmentIndex);
                            }
                            if (isT2)
                            {
                                highLvl.Add(ed.eliteEquipmentDef.equipmentIndex);
                                gigaChad.Add(ed.eliteEquipmentDef.equipmentIndex);
                            }
                        }
                    }
                }
            }

            lowLevelEliteBuffs = [.. lowLvl];
            highLevelEliteBuffs = [.. highLvl];
            gigaChadLvl = [.. gigaChad];
        }

        public static void ClearEliteLists()
        {
            BetterInventoryController.gigaChadLvl.Clear();
            DevotionInventoryController.lowLevelEliteBuffs.Clear();
            DevotionInventoryController.highLevelEliteBuffs.Clear();
        }

        public static void OnRunStartGlobal()
        {
            if (!BetterInventoryController.initialized && (Config.PluginConfig.permaDevotion.Value || RunArtifactManager.instance.IsArtifactEnabled(CU8Content.Artifacts.Devotion)))
            {
                BetterInventoryController.initialized = true;
                Run.onRunDestroyGlobal += DevotionInventoryController.OnRunDestroy;
                BossGroup.onBossGroupDefeatedServer += DevotionInventoryController.OnBossGroupDefeatedServer;
                On.RoR2.MasterSummon.Perform += DevotionTweaks.instance.MasterSummon_Perform;

                StatTweaks.InitHooks();
                BetterInventoryController.CreateEliteLists();
            }
        }

        public static new void OnDevotionArtifactDisabled(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (BetterInventoryController.initialized && !Config.PluginConfig.permaDevotion.Value && artifactDef == CU8Content.Artifacts.Devotion)
            {
                BetterInventoryController.initialized = false;
                Run.onRunDestroyGlobal -= DevotionInventoryController.OnRunDestroy;
                BossGroup.onBossGroupDefeatedServer -= DevotionInventoryController.OnBossGroupDefeatedServer;
                On.RoR2.MasterSummon.Perform -= DevotionTweaks.instance.MasterSummon_Perform;

                StatTweaks.RemoveHooks();
                BetterInventoryController.ClearEliteLists();
            }
        }
        #endregion

        public List<BetterLemurController> GetFriends(BetterLemurController self = null)
        {
            List<BetterLemurController> friends = [];
            if (SummonerMaster)
            {
                var minionGroup = MinionOwnership.MinionGroup.FindGroup(SummonerMaster.netId);
                if (minionGroup != null)
                {
                    foreach (var minionOwnership in minionGroup.members)
                    {
                        if (minionOwnership && minionOwnership.TryGetComponent<BetterLemurController>(out var friend) &&
                            friend && friend != self && friend.LemurianInventory && friend.PersonalInventory)
                        {
                            friends.Add(friend);
                        }
                    }
                }
            }
            return friends;
        }
        public static List<BetterLemurController> GetFriends(NetworkInstanceId netId)
        {
            List<BetterLemurController> friends = [];
            var minionGroup = MinionOwnership.MinionGroup.FindGroup(netId);
            if (minionGroup != null)
            {
                foreach (var minionOwnership in minionGroup.members)
                {
                    if (minionOwnership && minionOwnership.TryGetComponent<BetterLemurController>(out var friend) &&
                        friend && friend.LemurianInventory && friend.PersonalInventory)
                    {
                        friends.Add(friend);
                    }
                }
            }
            return friends;
        }

        private new void Awake()
        {
            _devotionMinionInventory = GetComponent<Inventory>();
        }


        public void ShareInitialItem(BetterLemurController lem, ItemIndex item)
        {
            this.GiveItem(item);
            lem.PersonalInventory.GiveItemPermanent(item);
            lem.LemurianInventory.GiveItemPermanent(item);

            if (DevotionTweaks.instance.EnableSharedInventory == false)
                return;

            foreach (var friend in GetFriends(lem))
            {
                friend.LemurianInventory.GiveItemChanneled(item);
            }
        }

        public void GiveItemsToGroup(BetterLemurController lem)
        {
            Inventory src = lem.PersonalInventory;
            if (src == null)
                return;

            using var _1 = ListPool<ItemIndex>.RentCollection(out var allItems);
            using var _2 = ItemCatalog.PerItemBufferPool.RequestTemp(out int[] allStacks);

            src.permanentItemStacks.GetNonZeroIndices(allItems);
            src.WriteAllPermanentItemStacks(allStacks);


            Inventory dest = this._devotionMinionInventory;
            if (dest)
            {
                using (var _0 = new Inventory.InventoryChangeScope(dest))
                {
                    for (int i = 0; i < allItems.Count; i++)
                    {
                        ItemIndex itemIndex = allItems[i];
                        dest.ChangeItemStacksCount(new Inventory.GiveItemPermanentImpl
                        {
                            inventory = dest
                        }, itemIndex, allStacks[(int)itemIndex]);
                    }
                }
            }


            foreach (var friend in GetFriends())
            {
                if (DevotionTweaks.instance.EnableSharedInventory == false && friend != lem)
                    continue;

                dest = friend.LemurianInventory;
                using (var _0 = new Inventory.InventoryChangeScope(dest))
                {
                    for (int i = 0; i < allItems.Count; i++)
                    {
                        ItemIndex itemIndex = allItems[i];
                        dest.ChangeItemStacksCount(new Inventory.GiveItemChanneledImpl
                        {
                            inventory = dest
                        }, itemIndex, allStacks[(int)itemIndex]);
                    }
                }
            }
        }

        public void TakeItemsFromGroup(BetterLemurController lem)
        {
            if (DevotionTweaks.instance.EnableSharedInventory == false)
                return;

            Inventory dest = lem.LemurianInventory;
            if (dest == null)
                return;

            using var _1 = CollectionPool<ItemIndex, HashSet<ItemIndex>>.RentCollection(out var allItems);
            using var _2 = ItemCatalog.PerItemBufferPool.RequestTemp(out int[] allStacks);
            using var _3 = ListPool<ItemIndex>.RentCollection(out var lemItems);

            foreach (var friend in GetFriends(lem))
            {
                var src = friend.PersonalInventory;

                lemItems.Clear();
                src.permanentItemStacks.GetNonZeroIndices(lemItems);
                for (int i = 0; i < lemItems.Count; i++)
                {
                    ItemIndex itemIndex = lemItems[i];
                    allItems.Add(itemIndex);

                    ref var stack = ref allStacks[(int)itemIndex];
                    stack += src.permanentItemStacks.GetStackValue(itemIndex) * (1 + friend.DevotedEvolutionLevel);
                }
            }

            using var _0 = new Inventory.InventoryChangeScope(dest);
            foreach (var itemIndex in allItems)
            {
                dest.ChangeItemStacksCount(new Inventory.GiveItemChanneledImpl
                {
                    inventory = dest
                }, itemIndex, allStacks[(int)itemIndex]);
            }
        }

        public void RemovesItemFromGroup(BetterLemurController lem)
        {
            Inventory src = lem.PersonalInventory;
            if (src == null)
                return;

            using var _1 = ListPool<ItemIndex>.RentCollection(out var allItems);
            using var _2 = ItemCatalog.PerItemBufferPool.RequestTemp(out int[] allStacks);

            src.permanentItemStacks.GetNonZeroIndices(allItems);
            src.WriteAllPermanentItemStacks(allStacks);


            Inventory dest = this._devotionMinionInventory;
            if (dest)
            {
                using (var _0 = new Inventory.InventoryChangeScope(dest))
                {
                    for (int i = 0; i < allItems.Count; i++)
                    {
                        ItemIndex itemIndex = allItems[i];
                        dest.ChangeItemStacksCount(new Inventory.GiveItemPermanentImpl
                        {
                            inventory = dest
                        }, itemIndex, -allStacks[(int)itemIndex]);
                    }
                }
            }

            if (DevotionTweaks.instance.EnableSharedInventory == false)
                return;

            foreach (var friend in GetFriends(lem))
            {
                dest = friend.LemurianInventory;
                using (var _0 = new Inventory.InventoryChangeScope(dest))
                {
                    for (int i = 0; i < allItems.Count; i++)
                    {
                        ItemIndex itemIndex = allItems[i];
                        dest.ChangeItemStacksCount(new Inventory.GiveItemChanneledImpl
                        {
                            inventory = dest
                        }, itemIndex, -allStacks[(int)itemIndex]);
                    }
                }
            }
        }
    }
}
