using HarmonyLib;
using System;

namespace LemurFusion
{
    [HarmonyPatch(typeof(LemurianNames.LemurianNames), "UpdateNameFriend")]
    public class LemurianUpdateNameFriend
    {
        [HarmonyFinalizer]
        public static Exception Finalizer(Exception __exception)
        {
            if (__exception != null)
            {
                // An exception was thrown by the method!
                LemurFusionPlugin.LogWarning("Exception was thrown by dependency bouncyshield.LemurianNames!");
                LemurFusionPlugin.LogWarning(__exception.Message);
                LemurFusionPlugin.LogWarning(__exception.StackTrace);
            }

            // return null so that no Exception is thrown. You could re-throw as a different Exception as well.
            return null;
        }
    }

    [HarmonyPatch(typeof(LemurianNames.LemurianNames), "NameFriend")]
    public class LemurianNameFriend
    {
        [HarmonyFinalizer]
        public static Exception Finalizer(Exception __exception)
        {
            if (__exception != null)
            {
                // An exception was thrown by the method!
                LemurFusionPlugin.LogWarning("Exception was thrown by dependency bouncyshield.LemurianNames!");
                LemurFusionPlugin.LogWarning(__exception.Message);
                LemurFusionPlugin.LogWarning(__exception.StackTrace);
            }

            // return null so that no Exception is thrown. You could re-throw as a different Exception as well.
            return null;
        }
    }
}
