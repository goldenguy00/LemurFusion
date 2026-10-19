using LemurFusion.Config;
using MonoMod.Cil;
using RoR2;
using UE = UnityEngine;
using UnityEngine.Networking;
using Mono.Cecil.Cil;
using LemurFusion.Devotion.Components;
using System.Linq;
using MonoMod.RuntimeDetour;
using HarmonyLib;
using System;

namespace LemurFusion.Devotion
{
    public class DevotedInventoryTweaks
    {
        public static DevotedInventoryTweaks instance { get; private set; }

        public static void Init() => instance ??= new DevotedInventoryTweaks();

        private DevotedInventoryTweaks()
        {
            //       //
            // hooks //
            //       //
            IL.RoR2.DevotionInventoryController.EvolveDevotedLumerian += EvolveDevotedLumerian;

            // get em out
            On.RoR2.DevotionInventoryController.Init += DevotionInventoryController_Init;

            On.RoR2.DevotionInventoryController.UpdateMinionInventory += UpdateMinionInventory;
            On.RoR2.DevotionInventoryController.UpdateAllMinions += UpdateAllMinions;
            new Hook(AccessTools.PropertyGetter(typeof(DevotionInventoryController), nameof(DevotionInventoryController.isDevotionEnable)), DevotionInventoryController_isDevotionEnable);
        }

        private static void DevotionInventoryController_Init(On.RoR2.DevotionInventoryController.orig_Init orig)
        {
            Run.onRunStartGlobal += (_) => BetterInventoryController.OnRunStartGlobal();
            RunArtifactManager.onArtifactEnabledGlobal += (_, _) => BetterInventoryController.OnRunStartGlobal();
            RunArtifactManager.onArtifactDisabledGlobal += BetterInventoryController.OnDevotionArtifactDisabled;
        }
        private static bool DevotionInventoryController_isDevotionEnable(Func<bool> orig)
        {
            return orig() || PluginConfig.permaDevotion.Value;
        }

        #region IL Hooks
        private static void EvolveDevotedLumerian(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.After,
                    x => x.MatchSwitch(out _)
                ))
            {
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate<Action<DevotedLemurianController>>((lem) =>
                {
                    var list = PluginConfig.highTierElitesOnly.Value ? BetterInventoryController.gigaChadLvl : DevotionInventoryController.highLevelEliteBuffs;
                    if (list.Any())
                        lem.LemurianInventory.SetEquipmentIndex(list[UE.Random.Range(0, list.Count)]);
                });
            }
            else
                LemurFusionPlugin.LogError("Hook failed for DevotionInventoryController_EvolveDevotedLumerian #2");
        }
        #endregion

        #region DevInvCtrl Hooks

        private void UpdateAllMinions(On.RoR2.DevotionInventoryController.orig_UpdateAllMinions orig,
            DevotionInventoryController self, bool shouldEvolve)
        {
            if (shouldEvolve)
                orig(self, shouldEvolve);
        }

        private void UpdateMinionInventory(On.RoR2.DevotionInventoryController.orig_UpdateMinionInventory orig,
            DevotionInventoryController self, DevotedLemurianController lem, bool shouldEvolve)
        {
            if (!NetworkServer.active)
                return;

            if (lem is BetterLemurController lemCtrl && lemCtrl && self is BetterInventoryController betterCtrl)
            {
                betterCtrl.GiveItemsToGroup(lemCtrl);

                var body = lemCtrl.LemurianBody;
                if (BetterInventoryController.activationSoundEventDef && body)
                {
                    EffectManager.SimpleSoundEffect(BetterInventoryController.activationSoundEventDef.index, lemCtrl.LemurianBody.transform.position, transmit: true);
                }

                lemCtrl.DevotedEvolutionLevel++;
                self.EvolveDevotedLumerian(lemCtrl);
            }
        }
        #endregion
    }
}
