using HarmonyLib;
using System;

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
}
