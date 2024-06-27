using LemurFusion.Config;
using MonoMod.Cil;
using RoR2;
using System;
using UE = UnityEngine;
using UnityEngine.Networking;
using Mono.Cecil.Cil;
using LemurFusion.Devotion.Components;
using System.Linq;
using UnityEngine;

namespace LemurFusion.Devotion
{
    public class DevotedInventoryTweaks
    {
        public static DevotedInventoryTweaks instance { get; private set; }

        public static void Init()
        {
            if (instance != null)
                return;

            instance = new DevotedInventoryTweaks();
        }

        public bool initialized { get; private set; }

        private DevotedInventoryTweaks()
        {
            //       //
            // hooks //
            //       //
            IL.RoR2.SceneDirector.PopulateScene += PopulateScene;
            IL.RoR2.DevotionInventoryController.EvolveDevotedLumerian += EvolveDevotedLumerian;

            On.RoR2.DevotionInventoryController.ActivateDevotedEvolution += DevotionInventoryController_ActivateDevotedEvolution;
            On.RoR2.DevotionInventoryController.OnDevotionArtifactDisabled += OnDevotionArtifactDisabled;
            On.RoR2.DevotionInventoryController.OnDevotionArtifactEnabled += OnDevotionArtifactEnabled;
            On.RoR2.DevotionInventoryController.UpdateMinionInventory += UpdateMinionInventory;
            On.RoR2.DevotionInventoryController.UpdateAllMinions += UpdateAllMinions;
        }

        private static void DevotionInventoryController_ActivateDevotedEvolution(On.RoR2.DevotionInventoryController.orig_ActivateDevotedEvolution orig)
        {
            if (!NetworkServer.active)
            {
                UE.Debug.LogWarning("[Server] function 'System.Void RoR2.DevotionInventoryController::ActivateDevotedEvolution()' called on client");
                return;
            }
            foreach (var devotionInventoryController in DevotionInventoryController.InstanceList)
            {
                    if (devotionInventoryController.sfxLocator)
                        Util.PlaySound(devotionInventoryController.sfxLocator.openSound, devotionInventoryController.gameObject);
                    devotionInventoryController.UpdateAllMinions(true);
            }
        }

        #region IL Hooks
        private static void PopulateScene(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.Before,
                    i => i.MatchLdfld(typeof(SceneDirector), nameof(SceneDirector.lumerianEgg))
                ))
            {
                c.Remove();
                c.Emit(OpCodes.Ldloc_3);
                c.EmitDelegate<Func<SceneDirector, DirectorCard, DirectorCard>>((self, original) =>
                {
                    if (self.rng.RangeInt(0, 100) < 50)
                        return self.lumerianEgg;
                    return original;
                });
            }
            else
            {
                LemurFusionPlugin.LogError("IL Hook failed for SceneDirector_PopulateScene");
            }
        }

        private static void EvolveDevotedLumerian(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.Before,
                x => x.MatchLdstr(DevotionTweaks.bigLemBodyName)
                ))
            {
                c.Remove();
                c.Emit(OpCodes.Ldstr, DevotionTweaks.devotedBigLemBodyName);
            }
            else
            {
                LemurFusionPlugin.LogError("Hook failed for DevotionInventoryController_EvolveDevotedLumerian #1");
            }

            if (c.TryGotoNext(MoveType.AfterLabel,
                i => i.MatchLdstr("shouldn't evolve!"),
                i => i.MatchCall<UE.Debug>("LogError")))
            {
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate<Action<DevotedLemurianController>>((lem) =>
                {
                    var list = PluginConfig.highTierElitesOnly.Value ? BetterInventoryController.gigaChadLvl : DevotionInventoryController.highLevelEliteBuffs;

                    var idx = UE.Random.Range(0, list.Count);
                    lem.LemurianInventory.SetEquipmentIndex(list[idx]);
                });
                c.RemoveRange(2);
            }
            else
            {
                LemurFusionPlugin.LogError("Hook failed for DevotionInventoryController_EvolveDevotedLumerian #2");
            }
        }
        #endregion

        #region DevInvCtrl Hooks
        private static void OnDevotionArtifactEnabled(On.RoR2.DevotionInventoryController.orig_OnDevotionArtifactEnabled orig,
            RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (artifactDef != CU8Content.Artifacts.Devotion)
                return;

            if (!DevotedInventoryTweaks.instance.initialized)
            {
                DevotedInventoryTweaks.instance.initialized = true;
                Run.onRunDestroyGlobal += DevotionInventoryController.OnRunDestroy;
                BossGroup.onBossGroupDefeatedServer += DevotionInventoryController.OnBossGroupDefeatedServer;
                On.RoR2.MasterSummon.Perform += DevotionTweaks.MasterSummon_Perform;

                StatTweaks.InitHooks();
                BetterInventoryController.CreateEliteLists();
            }
        }

        private static void OnDevotionArtifactDisabled(On.RoR2.DevotionInventoryController.orig_OnDevotionArtifactDisabled orig, 
            RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (artifactDef != CU8Content.Artifacts.Devotion)
                return;

            if (DevotedInventoryTweaks.instance.initialized)
            {
                DevotedInventoryTweaks.instance.initialized = false;
                Run.onRunDestroyGlobal -= DevotionInventoryController.OnRunDestroy;
                BossGroup.onBossGroupDefeatedServer -= DevotionInventoryController.OnBossGroupDefeatedServer;
                On.RoR2.MasterSummon.Perform -= DevotionTweaks.MasterSummon_Perform;

                StatTweaks.RemoveHooks();
                BetterInventoryController.ClearEliteLists();
            }
        }
        private static void UpdateAllMinions(On.RoR2.DevotionInventoryController.orig_UpdateAllMinions orig,
            DevotionInventoryController self, bool shouldEvolve)
        {
            if (shouldEvolve)
                orig(self, shouldEvolve);
        }

        private static void UpdateMinionInventory(On.RoR2.DevotionInventoryController.orig_UpdateMinionInventory orig,
            DevotionInventoryController self, DevotedLemurianController lem, bool shouldEvolve)
        {
            if (!NetworkServer.active || lem is not BetterLemurController lemCtrl)
                return;

            foreach (var item in lemCtrl._devotedItemList.Keys.ToList())
            {
                lemCtrl._devotedItemList[item]++;
                lemCtrl.BetterInventoryController.ShareItemWithFriends(item);
                lemCtrl.BetterInventoryController.GiveItem(item);
            }

            Util.PlaySound("Play_obj_devotion_egg_evolve", lemCtrl.LemurianBody.gameObject);
            lem.DevotedEvolutionLevel++;
            self.EvolveDevotedLumerian(lem);
        }
        #endregion
    }
}
