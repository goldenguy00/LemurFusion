using LemurFusion.Config;
using Mono.Cecil;
using RoR2;
using System.Linq;

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

            if (self is BetterLemurController lemCtrl && lemCtrl && lemCtrl.LemurianInventory)
            {
                lemCtrl._leashDistSq = PluginConfig.teleportDistance.Value * PluginConfig.teleportDistance.Value;
                
                if (lemCtrl.LemurianInventory.GetItemCount(CU8Content.Items.LemurianHarness) == 0 && PluginConfig.enableSharedInventory.Value)
                    lemCtrl.LemurianInventory.AddItemsFrom(lemCtrl.BetterInventoryController._devotionMinionInventory, ConfigExtended.Blacklist_Filter);

                Utils.AddItem(lemCtrl._devotedItemList, itemIndex);
                if (PluginConfig.enableSharedInventory.Value)
                    lemCtrl.BetterInventoryController.ShareItemWithFriends(itemIndex);
                else
                    lemCtrl.LemurianInventory.GiveItem(itemIndex);

                Utils.AddItem(lemCtrl._untrackedItemList, CU8Content.Items.LemurianHarness.itemIndex);
                Utils.SetItem(lemCtrl._untrackedItemList, RoR2Content.Items.MinionLeash.itemIndex);
                Utils.SetItem(lemCtrl._untrackedItemList, RoR2Content.Items.UseAmbientLevel.itemIndex);
                Utils.SetItem(lemCtrl._untrackedItemList, RoR2Content.Items.TeleportWhenOob.itemIndex);
                if (LemurFusionPlugin.riskyInstalled)
                {
                    lemCtrl.AddRiskyAllyItem();
                }

                foreach (var item in lemCtrl._untrackedItemList)
                {
                    var held = lemCtrl.LemurianInventory.GetItemCount(item.Key);
                    lemCtrl.LemurianInventory.GiveItem(item.Key, item.Value - held);
                }
            }
        }

        private static void DevotedLemurianController_OnDevotedBodyDead(On.DevotedLemurianController.orig_OnDevotedBodyDead orig, DevotedLemurianController self)
        {
            if (self is BetterLemurController lemCtrl && lemCtrl && lemCtrl._lemurianMaster &&
                !lemCtrl._lemurianMaster.IsInvoking("RespawnExtraLife") &&
                !lemCtrl._lemurianMaster.IsInvoking("RespawnExtraLife"))
            {
                lemCtrl.KillYourSelf();
            }
        }
        #endregion
    }
}
