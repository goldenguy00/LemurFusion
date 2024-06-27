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

            devotedItemData = lemCtrl._devotedItemList.Select(kvp =>
                new ProperSave.Data.ItemData()
                {
                    itemIndex = (int)kvp.Key,
                    count = kvp.Value
                }).ToArray();

            untrackedItemData = lemCtrl._untrackedItemList.Select(kvp =>
                new ProperSave.Data.ItemData()
                {
                    itemIndex = (int)kvp.Key,
                    count = kvp.Value
                }).ToArray();
        }

        public void LoadData(BetterLemurController lemCtrl)
        {
            var itemCount = ItemCatalog.itemCount;
            lemCtrl._devotedItemList = [];
            lemCtrl._untrackedItemList = [];

            for (int i = 0; i < devotedItemData.Length; i++)
            {
                var item = devotedItemData[i];
                if (item.itemIndex < itemCount)
                {
                    Utils.SetItem(lemCtrl._devotedItemList, (ItemIndex)item.itemIndex, item.count);
                }
            }

            for (int i = 0; i < untrackedItemData.Length; i++)
            {
                var item = untrackedItemData[i];
                if (item.itemIndex < itemCount)
                {
                    Utils.SetItem(lemCtrl._untrackedItemList, (ItemIndex)item.itemIndex, item.count);
                }
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

        private void Loading_OnLoadingEnded(ProperSave.SaveFile saveFile)
        {
            if (saveFile.ModdedData.TryGetValue(LemurFusionPlugin.PluginGUID, out var rawData) &&
                rawData?.Value is List<BetterLemurianData> lemurData && lemurData.Any())
            {
                CharacterMaster.onStartGlobal += SpawnMinion;
                void SpawnMinion(CharacterMaster master)
                {
                    if (master && master.TryGetComponent<BetterLemurController>(out var lemCtrl) && FuckingNullCheck(master, out var netId))
                    {
                        var savedLemData = lemurData.FirstOrDefault(lem => lem.summonerId.Load().Equals(netId));
                        if (savedLemData != null)
                        {
                            savedLemData.LoadData(lemCtrl);
                            lemurData.Remove(savedLemData);
                            lemCtrl._devotionInventoryController.UpdateAllMinions();
                        }

                        if (!lemurData.Any())
                        {
                            CharacterMaster.onStartGlobal -= SpawnMinion;
                        }
                    }
                }
            }
        }

        private static bool FuckingNullCheck(CharacterMaster master, out NetworkInstanceId netId)
        {
            // this is how you correctly nullcheck in unity.
            // fucking kill me in the face man.
            netId = NetworkInstanceId.Zero;

            if (!master)
                return false;

            var minion = master.minionOwnership;
            if (!minion)
                return false;

            var ownerMaster = minion.ownerMaster;
            if (!ownerMaster)
                return false;

            var pCMC = ownerMaster.playerCharacterMasterController;
            if (!pCMC)
                return false;

            var networkUser = pCMC.networkUser;
            if (!networkUser) 
                return false;

            netId = networkUser.netId;
            return !netId.IsEmpty(); 
        }

        private static List<BetterLemurController> GetLemurControllers(NetworkInstanceId masterID)
        {
            List<BetterLemurController> lemCtrlList = [];
            foreach (MinionOwnership minionOwnership in MinionOwnership.MinionGroup.FindGroup(masterID)?.members ?? [])
            {
                if (minionOwnership && minionOwnership.TryGetComponent<BetterLemurController>(out var lemCtrl))
                {
                    lemCtrlList.Add(lemCtrl);
                }
            }

            return lemCtrlList;
        }
    }

}
