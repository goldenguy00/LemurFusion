using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Networking;

namespace LemurFusion.Compat
{
    public class BetterLemurianData
    {
        [DataMember(Name = "bldsi")]
        public ProperSave.Data.UserIDData summonerId;

        [DataMember(Name = "blddid")]
        public ProperSave.Data.ItemData[] devotedItemData;

        [DataMember(Name = "blduid")]
        public ProperSave.Data.ItemData[] untrackedItemData;

        public BetterLemurianData() { }

        public BetterLemurianData(ProperSave.Data.UserIDData userID, BetterLemurController lemCtrl)
        {
            summonerId = userID;

            devotedItemData = [.. lemCtrl._devotedItemList.Select(kvp =>
                new ProperSave.Data.ItemData()
                {
                    itemIndex = (int)kvp.Key,
                    count = kvp.Value
                })];

            untrackedItemData = [.. lemCtrl._untrackedItemList.Select(kvp =>
                new ProperSave.Data.ItemData()
                {
                    itemIndex = (int)kvp.Key,
                    count = kvp.Value
                })];
        }

        public void LoadData(BetterLemurController lemCtrl)
        {
            lemCtrl._devotedItemList = [];
            lemCtrl._untrackedItemList = [];

            for (var i = 0; i < devotedItemData.Length; i++)
            {
                var item = devotedItemData[i];
                Utils.SetItem(lemCtrl._devotedItemList, (ItemIndex)item.itemIndex, item.count);
            }

            for (var i = 0; i < untrackedItemData.Length; i++)
            {
                var item = untrackedItemData[i];
                Utils.SetItem(lemCtrl._untrackedItemList, (ItemIndex)item.itemIndex, item.count);
            }
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

            foreach (var player in PlayerCharacterMasterController.instances)
            {
                if (player.networkUser && player.master)
                {
                    var userID = new ProperSave.Data.UserIDData(player.networkUser.id);
                    foreach (var lem in GetLemurControllers(player.master.netId))
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
                    if (master && master.TryGetComponent<BetterLemurController>(out var lemCtrl) && FuckMyAss.FuckingNullCheckNetId(master, out var netId))
                    {
                        var savedLemData = lemurData.FirstOrDefault(lem => lem.summonerId.Load().Equals(netId));
                        if (savedLemData != null)
                        {
                            savedLemData.LoadData(lemCtrl);
                            lemurData.Remove(savedLemData);
                        }

                        if (!lemurData.Any())
                        {
                            CharacterMaster.onStartGlobal -= SpawnMinion;
                        }
                    }
                }
            }
        }

        private static List<BetterLemurController> GetLemurControllers(NetworkInstanceId masterID)
        {
            List<BetterLemurController> lemCtrlList = [];
            var minionGroup = MinionOwnership.MinionGroup.FindGroup(masterID);
            if (minionGroup != null)
            {
                foreach (var minionOwnership in minionGroup.members)
                {
                    if (minionOwnership && minionOwnership.GetComponent<CharacterMaster>().TryGetComponent<BetterLemurController>(out var friend))
                    {
                        lemCtrlList.Add(friend);
                    }
                }
            }

            return lemCtrlList;
        }
    }

}
