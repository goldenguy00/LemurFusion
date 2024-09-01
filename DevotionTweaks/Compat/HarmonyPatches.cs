﻿using HarmonyLib;
using LemurFusion.Devotion;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LemurFusion.Compat
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
    /*
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
                newVariant.moveSpeedMultiplier = Mathf.Clamp(variant.moveSpeedMultiplier, 0.5f, 2f);
                newVariant.aiModifier = VAPI.BasicAIModifier.Default;

                if (newVariant.variantInventory && newVariant.variantInventory.itemInventory?.Any() == true)
                {
                    foreach (var itemPair in newVariant.variantInventory.itemInventory.Where(i => i?.itemDef != null))
                    {
                        var itemDef = itemPair.itemDef.Asset;
                        if (itemDef != null)
                        {
                            LemurFusionPlugin.LogInfo("Item def found " + itemDef.name + " | " + itemDef.nameToken);
                            if(itemDef == RoR2.RoR2Content.Items.Hoof)
                            {
                                itemPair.amount = Math.Min(2, itemPair.amount);
                            }
                        }
                    }
                }
                

                registerVariant.Invoke(null, [newVariant, (VAPI.VariantIndex)num]);
                num++;
                var pack = VAPI.VariantPackCatalog.FindVariantPackDef(variant);
                pack.variants = [.. pack.variants, newVariant];

                devotedVariants.Add(newVariant);
            }
            return devotedVariants;
        }
    }*/
}
