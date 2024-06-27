using RoR2;
using R2API;
using System.Collections.Generic;
using System.Linq;

namespace LemurFusion.Devotion.Components
{
    public class BetterInventoryController : DevotionInventoryController
    {
        public static List<EquipmentIndex> gigaChadLvl = [];

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
                    foreach (EliteDef ed in etd.eliteTypes)
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

        public List<BetterLemurController> GetFriends()
        {
            List<BetterLemurController> friends = [];
            if (SummonerMaster)
            {
                MinionOwnership.MinionGroup minionGroup = MinionOwnership.MinionGroup.FindGroup(SummonerMaster.netId);
                if (minionGroup != null)
                {
                    foreach (MinionOwnership minionOwnership in minionGroup.members)
                    {
                        if (minionOwnership && minionOwnership.GetComponent<CharacterMaster>().TryGetComponent<BetterLemurController>(out var friend))
                        {
                            friends.Add(friend);
                        }
                    }
                }
            }
            return friends;
        }

        public void ShareItemWithFriends(ItemIndex item)
        {
            var friends = GetFriends();
            foreach (var friend in friends)
            {
                friend.LemurianInventory.GiveItem(item);
            }
        }

        public void RemoveSharedItemsFromFriends(SortedList<ItemIndex, int> itemList)
        {
            var friends = GetFriends();
            foreach (var item in itemList)
            {
                _devotionMinionInventory.RemoveItem(item.Key, item.Value);
                foreach (var friend in friends)
                {
                    friend.LemurianInventory.RemoveItem(item.Key, item.Value);
                }
            }
        }
    }
}
