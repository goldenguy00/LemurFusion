using RoR2;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine.Networking;

namespace LemurFusion
{
    [DataContract]
    public class BetterLemurianData
    {
        [DataMember(Name = "bldsi")]
        public ProperSave.Data.UserIDData summonerId;

        [DataMember(Name = "bldid")]
        public ProperSave.Data.ItemData[] itemData;

        [DataMember(Name = "bldfl")]
        public int fusionLevel;

        public BetterLemurianData() { }

        public BetterLemurianData(ProperSave.Data.UserIDData userID, BetterLemurController lemCtrl)
        {
            summonerId = userID;
            fusionLevel = lemCtrl.FusionCount;
            itemData = lemCtrl._devotedItemList.Select(kvp =>
                new ProperSave.Data.ItemData()
                {
                    itemIndex = (int)kvp.Key,
                    count = kvp.Value
                }).ToArray();
        }

        public void LoadData(BetterLemurController lemCtrl)
        {
            lemCtrl.FusionCount = fusionLevel;
            lemCtrl._devotedItemList = [];
            foreach (var item in itemData)
            {
                lemCtrl._devotedItemList[(ItemIndex)item.itemIndex] = item.count;
            }
        }
    }

    public class ProperSaveManager
    {
        public static void Init()
        {
            ProperSave.SaveFile.OnGatherSaveData += SaveFile_OnGatherSaveData;
            ProperSave.Loading.OnLoadingEnded += Loading_OnLoadingEnded;
        }

        private static void SaveFile_OnGatherSaveData(Dictionary<string, object> obj)
        {
            if (!RunArtifactManager.instance.IsArtifactEnabled(CU8Content.Artifacts.Devotion)) return;

            List<BetterLemurianData> lemurData = [];

            foreach (PlayerCharacterMasterController player in PlayerCharacterMasterController.instances)
            {
                if (player.networkUser && player.master)
                {
                    var userID = new ProperSave.Data.UserIDData(player.networkUser.id);
                    var lemList = GetLemurControllers(player.master.netId);

                    foreach (var lem in lemList)
                    {
                        lemurData.Add(new BetterLemurianData(userID, lem));
                    }
                }
            }

            obj[LemurFusionPlugin.PluginGUID] = lemurData;
        }

        private static void Loading_OnLoadingEnded(ProperSave.SaveFile saveFile)
        {
            if (saveFile.ModdedData.TryGetValue(LemurFusionPlugin.PluginGUID, out var rawData) && rawData != null)
            {
                if (rawData.Value is List<BetterLemurianData> lemurData && lemurData.Any())
                {
                    foreach (NetworkUser user in NetworkUser.readOnlyInstancesList)
                    {
                        var savedLemData = lemurData.Where(lem => lem.summonerId.Load().Equals(user.id)).ToList();
                        var lemCtrlList = GetLemurControllers(user.Network_masterObjectId);
                        if (lemCtrlList?.Any() == true && lemCtrlList.Count == savedLemData?.Count())
                        {
                            for(int i = 0; i<lemCtrlList.Count; i++) 
                            {
                                savedLemData[i].LoadData(lemCtrlList[i]);
                            }
                        }

                        UpdateDevotionInventoryController(user.master);
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

        private static void UpdateDevotionInventoryController(CharacterMaster master)
        {
            foreach (DevotionInventoryController instance in DevotionInventoryController.InstanceList)
            {
                if (instance.SummonerMaster == master)
                {
                    instance.UpdateAllMinions(false);
                    return;
                }
            }
        }
    }

}
