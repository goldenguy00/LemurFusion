using RoR2;
using R2API;
using System.Collections.Generic;
using System.Linq;

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

        public List<BetterLemurController> GetFriends()
        {
            List<BetterLemurController> friends = [];
            if (SummonerMaster)
            {
                var minionGroup = MinionOwnership.MinionGroup.FindGroup(SummonerMaster.netId);
                if (minionGroup != null)
                {
                    foreach (var minionOwnership in minionGroup.members)
                    {
                        if (minionOwnership && minionOwnership.TryGetComponent<BetterLemurController>(out var friend))
                        {
                            friends.Add(friend);
                        }
                    }
                }
            }
            return friends;
        }

        public void ShareItemWithFriends(ItemIndex item, int count = 1)
        {
            foreach (var friend in GetFriends())
            {
                if (friend && friend.LemurianInventory)
                    friend.LemurianInventory.GiveItem(item, count);
            }
        }

        public void RemoveSharedItemsFromFriends(SortedList<ItemIndex, int> itemList)
        {
            foreach (var item in itemList)
            {
                _devotionMinionInventory.RemoveItem(item.Key, item.Value);
                foreach (var friend in GetFriends())
                {
                    if (friend && friend.LemurianInventory)
                    {
                        friend.LemurianInventory.RemoveItem(item.Key, System.Math.Min(item.Value, friend.LemurianInventory.GetItemCount(item.Key)));
                    }
                }
            }
        }
    }
}
