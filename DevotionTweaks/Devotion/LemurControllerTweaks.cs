using LemurFusion.Config;
using RoR2;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace LemurFusion.Devotion
{
    internal class LemurControllerTweaks
    {
        public static LemurControllerTweaks instance { get; private set; }

        private LemurControllerTweaks()
        {
            On.DevotedLemurianController.InitializeDevotedLemurian += DevotedLemurianController_InitializeDevotedLemurian;
            On.DevotedLemurianController.OnDevotedBodyDead += DevotedLemurianController_OnDevotedBodyDead;
        }

        public static void Init()
        {
            if (instance != null)
                return;

            instance = new LemurControllerTweaks();
        }

        #region Hooks

        private static void DevotedLemurianController_InitializeDevotedLemurian(On.DevotedLemurianController.orig_InitializeDevotedLemurian orig,
            DevotedLemurianController self, ItemIndex itemIndex, DevotionInventoryController devInvCtrl)
        {
            orig(self, itemIndex, devInvCtrl);

            if (self is BetterLemurController lemCtrl)
            {
                lemCtrl._leashDistSq = PluginConfig.teleportDistance.Value * PluginConfig.teleportDistance.Value;
                
                if (lemCtrl.FusionCount == 0 && PluginConfig.enableSharedInventory.Value)
                    lemCtrl.LemurianInventory.AddItemsFrom(lemCtrl.BetterInventoryController._devotionMinionInventory, ConfigExtended.Blacklist_Filter);

                Utils.AddItem(lemCtrl._devotedItemList, itemIndex);
                if (PluginConfig.enableSharedInventory.Value)
                    lemCtrl.BetterInventoryController.ShareItemWithFriends(itemIndex);
                else
                    lemCtrl.LemurianInventory.GiveItem(itemIndex);

                Utils.AddItem(lemCtrl._untrackedItemList, CU8Content.Items.LemurianHarness.itemIndex);
                lemCtrl.SyncPersonalInventory();
            }
        }

        private static void DevotedLemurianController_OnDevotedBodyDead(On.DevotedLemurianController.orig_OnDevotedBodyDead orig, DevotedLemurianController self)
        {
            if (self is not BetterLemurController lemCtrl)
            {
                orig(self);
                return;
            }
            
            if (!lemCtrl._lemurianMaster.IsInvoking("RespawnExtraLife") && !lemCtrl._lemurianMaster.IsInvoking("RespawnExtraLife"))
            {
                lemCtrl.KillYourSelf();
            }
        }
        #endregion
    }
}
