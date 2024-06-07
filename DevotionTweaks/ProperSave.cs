using ProperSave.Data;
using ProperSave.SaveData;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Networking;

namespace LemurFusion
{
    public class BetterLemurianData
    {
        [DataMember(Name = "bldsi")]
        public UserIDData summonerId;

        [DataMember(Name = "bldid")]
        public ItemData[] itemData;

        [DataMember(Name = "bldfl")]
        public int fusionLevel;

        public BetterLemurianData() { }

        public BetterLemurianData(UserIDData userID, BetterLemurController lemCtrl)
        {
            summonerId = userID;
            fusionLevel = lemCtrl.FusionCount;
            itemData = lemCtrl._devotedItemList.Select(kvp =>
                new ItemData()
                {
                    itemIndex = (int)kvp.Key,
                    count = kvp.Value
                }).ToArray();
        }

        public void LoadData(BetterLemurController lemCtrl)
        {
            lemCtrl._devotedItemList = [];
            for (int i = 0; i < itemData.Length; i++)
            {
                var item = itemData[i];
                Utils.SetItem(lemCtrl._devotedItemList, (ItemIndex)item.itemIndex, item.count);
            }

            lemCtrl._untrackedItemList = [];
            Utils.SetItem(lemCtrl._untrackedItemList, CU8Content.Items.LemurianHarness, fusionLevel);
            Utils.SetItem(lemCtrl._untrackedItemList, RoR2Content.Items.MinionLeash);
            Utils.SetItem(lemCtrl._untrackedItemList, RoR2Content.Items.UseAmbientLevel);
            Utils.SetItem(lemCtrl._untrackedItemList, RoR2Content.Items.TeleportWhenOob);
        }
    }

    public class ProperSaveManager : MonoBehaviour
    {
        public ProperSaveManager()
        {
            ProperSave.SaveFile.OnGatherSaveData += SaveFile_OnGatherSaveData;
            ProperSave.Loading.OnLoadingEnded += Loading_OnLoadingEnded;
        }

        private void SaveFile_OnGatherSaveData(Dictionary<string, object> obj)
        {
            List<BetterLemurianData> lemurData = [];

            foreach (PlayerCharacterMasterController player in PlayerCharacterMasterController.instances)
            {
                if (player.networkUser && player.master)
                {
                    var userID = new UserIDData(player.networkUser.id);
                    var lemList = GetLemurControllers(player.master.netId);

                    foreach (var lem in lemList)
                    {
                        lemurData.Add(new BetterLemurianData(userID, lem));
                    }
                }
            }

            obj[LemurFusionPlugin.PluginGUID] = lemurData;
        }

        private void Loading_OnLoadingEnded(ProperSave.SaveFile saveFile)
        {
            if (saveFile.ModdedData.TryGetValue(LemurFusionPlugin.PluginGUID, out var rawData) && 
                rawData?.Value is List<BetterLemurianData> lemurData && lemurData.Any())
            {
                CharacterMaster.onStartGlobal += SpawnMinion;
                void SpawnMinion(CharacterMaster master)
                {
                    if (master && master.TryGetComponent<BetterLemurController>(out var lemCtrl))
                    {
                        var netId = master.minionOwnership?.ownerMaster?.playerCharacterMasterController?.networkUser?.id;
                        if (!netId.HasValue) return;

                        var savedLemData = lemurData.FirstOrDefault(lem => lem.summonerId.Load().Equals(netId));
                        if (savedLemData != null)
                        {
                            savedLemData.LoadData(lemCtrl);
                            lemurData.Remove(savedLemData);
                        }

                        if (!lemurData.Any())
                        {
                            CharacterMaster.onStartGlobal -= SpawnMinion;
                            
                            // sync
                            foreach (DevotionInventoryController instance in DevotionInventoryController.InstanceList)
                            {
                                instance.UpdateAllMinions(false);
                            }
                        }
                    }
                }
            }
        }

        private static List<BetterLemurController> GetLemurControllers(NetworkInstanceId masterID)
        {
            List<BetterLemurController> lemCtrlList = [];
            foreach (MinionOwnership minionOwnership in MinionOwnership.MinionGroup.FindGroup(masterID)?.members ?? [])
            {
                if (minionOwnership && minionOwnership.gameObject.TryGetComponent<BetterLemurController>(out var lemCtrl))
                {
                    lemCtrlList.Add(lemCtrl);
                }
            }

            return lemCtrlList;
        }
    }

}
