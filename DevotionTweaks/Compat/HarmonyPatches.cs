using HarmonyLib;
using LemurFusion.Devotion;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LemurFusion.Compat
{
    [HarmonyPatch(typeof(RoR2.CombatSquad), "FixedUpdate")]
    public class CombatSquadFixedUpdate
    {
        [HarmonyFinalizer]
        public static Exception Finalizer(Exception __exception)
        {
            return null;
        }
    }

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
            }

            // return null so that no Exception is thrown. You could re-throw as a different Exception as well.
            return null;
        }
    }

    [HarmonyPatch(typeof(VAPI.VariantCatalog), "RegisterVariantsFromPacks")]
    public class VarianceAPI
    {
        private static int num = 0;
        [HarmonyPostfix]
        public static void Postfix(ref VAPI.VariantDef[] __result)
        {
            num = __result.Length;
            __result = [.. __result,
                .. CreateDevotionProvider(__result.Where(vd => vd.bodyName == DevotionTweaks.lemBodyName)),
                .. CreateDevotionProvider(__result.Where(vd => vd.bodyName == DevotionTweaks.bigLemBodyName))];
        }

        private static List<VAPI.VariantDef> CreateDevotionProvider(IEnumerable<VAPI.VariantDef> originalVariants)
        {
            List<VAPI.VariantDef> devotedVariants = [];

            var registerVariant = AccessTools.Method(typeof(VAPI.VariantCatalog), "RegisterVariant");
            foreach (var variant in originalVariants)
            {
                var newVariant = UnityEngine.Object.Instantiate(variant);
                newVariant.name = DevotionTweaks.devotedPrefix + variant.name;
                newVariant.bodyName = DevotionTweaks.devotedPrefix + variant.bodyName;
                newVariant.spawnRate = 100f / originalVariants.Count();
                newVariant.aiModifier = VAPI.BasicAIModifier.Default;
                

                registerVariant.Invoke(null, [newVariant, (VAPI.VariantIndex)num]);
                num++;
                var pack = VAPI.VariantPackCatalog.FindVariantPackDef(variant);
                pack.variants = [.. pack.variants, newVariant];

                devotedVariants.Add(newVariant);
            }
            return devotedVariants;
        }
    }
}
