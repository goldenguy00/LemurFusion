using HarmonyLib;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace LemurFusion
{
    [HarmonyPatch(typeof(LemurianNames.LemurianNames), "NameFriend")]
    public class LemurianNamesPatch
    {
        [HarmonyFinalizer]
        public static Exception Finalizer(Exception __exception)
        {
            if (__exception != null)
            {
                // An exception was thrown by the method!
                LemurFusionPlugin._logger.LogWarning("Exception was thrown by dependency bouncyshield.LemurianNames!");
                LemurFusionPlugin._logger.LogWarning(__exception.Message);
                LemurFusionPlugin._logger.LogWarning(__exception.StackTrace);
            }

            // return null so that no Exception is thrown. You could re-throw as a different Exception as well.
            return null;
        }
    }

    [HarmonyPatch(typeof(ProperSave.SaveData.MinionData), MethodType.Constructor, [typeof(CharacterMaster)])]
    public class ProperSavePatch
    {
        [HarmonyPostfix]
        public static void Postfix(ProperSave.SaveData.MinionData __instance, CharacterMaster master)
        {
            if (master.TryGetComponent<BetterLemurController>(out var devotedLemurianController))
            {
                __instance.devotedLemurianData = new BetterDevotedLemurianData(devotedLemurianController);
            }
        }
    }

    [HarmonyPatch(typeof(ProperSave.Data.DevotedLemurianData), nameof(ProperSave.Data.DevotedLemurianData.LoadData))]
    public class LemurDataPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ProperSave.Data.DevotedLemurianData __instance, DevotedLemurianController controller)
        {
            if (__instance is BetterDevotedLemurianData data)
            {
                data.LoadOtherData(controller);
            }
            else
            {
                LemurFusionPlugin._logger.LogError("Propersave cannot read lemurian data because it is not of the type BetterDevotedLemurianData!");
            }
        }
    }

    public class BetterDevotedLemurianData : ProperSave.Data.DevotedLemurianData
    {
        [DataMember(Name = "dil")]
        public List<ProperSave.Data.ItemData> devotedItemList;

        [DataMember(Name = "dfc")]
        public int devotedFusionCount;

        public BetterDevotedLemurianData(DevotedLemurianController controller) : base(controller)
        {
            if (controller is BetterLemurController lemCtrl)
            {
                if (lemCtrl?._devotedItemList?.Any() != true)
                {
                    devotedItemList = [];
                    foreach (var item in lemCtrl._devotedItemList)
                    {
                        devotedItemList.Add(new ProperSave.Data.ItemData
                        {
                            itemIndex = (int)item.Key,
                            count = item.Value
                        });
                    }
                    devotedFusionCount = lemCtrl.FusionCount;
                }
                else
                {
                    LemurFusionPlugin._logger.LogError("Propersave cannot save lemurian data because the lemurian controller is null!");
                }
            }
            else
            {
                LemurFusionPlugin._logger.LogError("Propersave cannot save lemurian data because it is not of the type BetterLemurController!");
            }
        }

        public void LoadOtherData(DevotedLemurianController controller)
        {
            if (controller is BetterLemurController lemCtrl)
            {
                lemCtrl._devotedItemList = [];
                if (devotedItemList?.Any() != true)
                {
                    foreach (var item in devotedItemList)
                    {
                        Utils.SetItem(lemCtrl._devotedItemList, (RoR2.ItemIndex)item.itemIndex, item.count);
                    }
                    lemCtrl.FusionCount = devotedFusionCount;
                }
                else
                {
                    LemurFusionPlugin._logger.LogError("Propersave cannot load lemurian data because the expected serialized list is null or empty!");
                }
            }
            else
            {
                LemurFusionPlugin._logger.LogError("Propersave cannot load lemurian data because it is not of the type BetterLemurController!");
            }
        }
    }
}
