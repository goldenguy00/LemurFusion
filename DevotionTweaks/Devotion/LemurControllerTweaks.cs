using RoR2;
using UnityEngine;

namespace LemurFusion.Devotion
{
    internal class LemurControllerTweaks
    {
        public static LemurControllerTweaks Instance { get; private set; }
        public static void Init() => Instance ??= new LemurControllerTweaks();

        private LemurControllerTweaks()
        {
            On.DevotedLemurianController.InitializeDevotedLemurian += DevotedLemurianController_InitializeDevotedLemurian;
            On.DevotedLemurianController.OnDevotedBodyDead += DevotedLemurianController_OnDevotedBodyDead;
        }

        #region Hooks

        private static void DevotedLemurianController_InitializeDevotedLemurian(On.DevotedLemurianController.orig_InitializeDevotedLemurian orig,
            DevotedLemurianController self, ItemIndex itemIndex, DevotionInventoryController devInvCtrl)
        {
            orig(self, itemIndex, devInvCtrl);

            if (self is BetterLemurController lemCtrl && lemCtrl)
            {
                lemCtrl.InitializeDevotedLemurian();
            }
        }

        private static void DevotedLemurianController_OnDevotedBodyDead(On.DevotedLemurianController.orig_OnDevotedBodyDead orig, DevotedLemurianController self)
        {
            if (self is BetterLemurController lemCtrl && lemCtrl && lemCtrl._lemurianMaster &&
                !lemCtrl._lemurianMaster.IsInvoking("RespawnExtraLife") &&
                !lemCtrl._lemurianMaster.IsInvoking("RespawnExtraLifeShrine") &&
                !lemCtrl._lemurianMaster.IsInvoking("RespawnExtraLifeVoid"))
            {
                lemCtrl.KillYourSelf();
            }
        }
        #endregion
    }
}
