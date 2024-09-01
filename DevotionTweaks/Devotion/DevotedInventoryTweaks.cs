using LemurFusion.Config;
using MonoMod.Cil;
using RoR2;
using UE = UnityEngine;
using UnityEngine.Networking;
using Mono.Cecil.Cil;
using LemurFusion.Devotion.Components;
using System.Linq;

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
        }

        private static void DevotionInventoryController_Init(On.RoR2.DevotionInventoryController.orig_Init orig)
        {
            Run.onRunStartGlobal += (_) => BetterInventoryController.OnRunStartGlobal();
            RunArtifactManager.onArtifactEnabledGlobal += (_, _) => BetterInventoryController.OnRunStartGlobal();
            RunArtifactManager.onArtifactDisabledGlobal += BetterInventoryController.OnDevotionArtifactDisabled;
        }

        #region IL Hooks
        private static void EvolveDevotedLumerian(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(x => x.MatchLdstr(DevotionTweaks.bigLemBodyName)))
                c.Next.Operand = DevotionTweaks.devotedBigLemBodyName;
            else
                LemurFusionPlugin.LogError("Hook failed for DevotionInventoryController_EvolveDevotedLumerian #1");

            if (c.TryGotoNext(MoveType.AfterLabel,
                i => i.MatchLdstr("shouldn't evolve!"),
                i => i.MatchCall<UE.Debug>("LogError")))
            {
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate(delegate (DevotedLemurianController lem)
                {
                    var list = PluginConfig.highTierElitesOnly.Value ? BetterInventoryController.gigaChadLvl : DevotionInventoryController.highLevelEliteBuffs;
                    
                    var idx = UE.Random.Range(0, list.Count);
                    lem.LemurianInventory.SetEquipmentIndex(list[idx]);
                });
                c.RemoveRange(2);
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
