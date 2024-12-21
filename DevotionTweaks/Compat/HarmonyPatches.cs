using HarmonyLib;
using LemurFusion.Devotion;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LemurFusion.Compat
{
    [HarmonyPatch]
    public class LemurianUpdateNameFriend
    {
        [HarmonyPatch(typeof(LemurianNames.LemurianNames), "UpdateNameFriend")]
        [HarmonyPatch(typeof(LemurianNames.LemurianNames), "NameFriend")]
        [HarmonyFinalizer]
        public static Exception Finalizer() => null;
    }

    [HarmonyPatch]
    public class FixProperSave
    {
        [HarmonyPatch(typeof(ProperSave.Data.CharacterMasterData), MethodType.Constructor, [typeof(CharacterMaster)])]
        [HarmonyPostfix]
        public static void Postfix(CharacterMaster master, ProperSave.Data.CharacterMasterData __instance)
        {
            if (__instance.devotionInventory == null)
            {
                foreach (var dev in DevotionInventoryController.InstanceList)
                {
                    if (dev.SummonerMaster == master)
                    {
                        __instance.devotionInventory = new ProperSave.Data.InventoryData(dev._devotionMinionInventory);
                    }
                }
            }
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
                .. CreateDevotionProvider(__result.Where(vd => vd.bodyName == "LemurianBody")),
                .. CreateDevotionProvider(__result.Where(vd => vd.bodyName == "LemurianBruiserBody"))];
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
                    foreach (var itemPair in newVariant.variantInventory.itemInventory.Where(i => i?.item != null))
                    {
                        var itemDef = itemPair.item.Asset;
                        if (itemDef != null && itemDef == RoR2.RoR2Content.Items.Hoof)
                        {
                            LemurFusionPlugin.LogDebug("Limiting hoof amount for variant to 2");
                            itemPair.amount = Math.Min(2, itemPair.amount);
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
