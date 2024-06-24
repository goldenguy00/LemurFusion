using LemurFusion.Config;
using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UE = UnityEngine;
using UnityEngine.Networking;
using Mono.Cecil.Cil;

namespace LemurFusion.Devotion.Tweaks
{
    internal class DevotedInventoryTweaks
    {
        public static DevotedInventoryTweaks instance { get; private set; }

        private DevotedInventoryTweaks()
        {
            //       //
            // hooks //
            //       //

            // always enabled
            IL.RoR2.DevotionInventoryController.EvolveDevotedLumerian += DevotionInventoryController_EvolveDevotedLumerian;
            IL.RoR2.DevotionInventoryController.GenerateEliteBuff += DevotionInventoryController_GenerateEliteBuff;

            // compat mode
            if (PluginConfig.enableCompatMode.Value)
            {
                On.RoR2.DevotionInventoryController.ActivateDevotedEvolution += DevotionInventoryController_ActivateDevotedEvolution;
                On.RoR2.DevotionInventoryController.UpdateMinionInventory += DevotionInventoryController_UpdateMinionInventory;
                IL.RoR2.DevotionInventoryController.UpdateMinionInventory += DevotionInventoryController_UpdateMinionInventory;
            }
        }

        public static void Init()
        {
            if (instance != null)
                return;

            instance = new DevotedInventoryTweaks();
        }

        #region Hooks
        private static void DevotionInventoryController_ActivateDevotedEvolution(On.RoR2.DevotionInventoryController.orig_ActivateDevotedEvolution orig)
        {
            orig();
            foreach (DevotionInventoryController devotionInventoryController in DevotionInventoryController.InstanceList)
            {
                devotionInventoryController.UpdateAllMinions(false);
            }
        }

        private static void DevotionInventoryController_UpdateMinionInventory(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.Before,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<DevotionInventoryController>(nameof(DevotionInventoryController._devotionMinionInventory)),
                    i => i.MatchLdarg(1),
                    i => i.MatchCallvirt<DevotedLemurianController>("get_DevotionItem"),
                    i => i.MatchLdcI4(out _),
                    i => i.MatchCallvirt<Inventory>(nameof(Inventory.GiveItem))
                ))
            {
                c.RemoveRange(6);
            }
            else
            {
                LemurFusionPlugin.LogError("IL Hook failed for DevotionInventoryController_UpdateMinionInventory #1");
            }

            if (c.TryGotoNext(MoveType.After,
                    i => i.MatchCall<DevotionInventoryController>(nameof(DevotionInventoryController.EvolveDevotedLumerian))) &&
                c.TryFindNext(out var next,
                    i => i.MatchCallvirt<Inventory>(nameof(Inventory.GiveItem)),
                    i => i.MatchCallvirt<Inventory>(nameof(Inventory.GiveItem))
                ))
            {
                // nuke all 40 something lines i do not care
                c.MoveAfterLabels();
                c.RemoveRange(next.Last().Index + 1 - c.Index);
            }
            else
            {
                LemurFusionPlugin.LogError("IL Hook failed for DevotionInventoryController_UpdateMinionInventory #2");
            }

            if (c.TryGotoNext(MoveType.Before,
                    i => i.MatchCallvirt<Inventory>(nameof(Inventory.AddItemsFrom))
                ))
            {
                if (ConfigExtended.Blacklist_Enable.Value && DevotionTweaks.EnableSharedInventory)
                {
                    c.Remove();
                    c.Emit<ConfigExtended>(OpCodes.Ldsfld, nameof(ConfigExtended.Blacklist_Filter));
                    c.Emit(OpCodes.Callvirt, typeof(Inventory).GetMethod(nameof(Inventory.AddItemsFrom), [typeof(Inventory), typeof(Func<ItemIndex, bool>)]));
                }
                if (!DevotionTweaks.EnableSharedInventory)
                {
                    c.Remove();
                    c.Emit(OpCodes.Pop);
                    c.Emit(OpCodes.Pop);
                }
            }
            else
            {
                LemurFusionPlugin.LogError("IL Hook failed for DevotionInventoryController_UpdateMinionInventory #3");
            }
        }

        private static void DevotionInventoryController_UpdateMinionInventory(On.RoR2.DevotionInventoryController.orig_UpdateMinionInventory orig,
            DevotionInventoryController self, DevotedLemurianController lem, bool shouldEvolve)
        {
            if (!NetworkServer.active || lem is not BetterLemurController lemCtrl)
            {
                orig(self, lem, shouldEvolve);
                return;
            }

            if (shouldEvolve && lemCtrl._devotedItemList.Any())
            {
                foreach (var item in lemCtrl._devotedItemList.Keys.ToList())
                {
                    if (DevotionTweaks.EnableSharedInventory)
                        self.GiveItem(item);

                    lemCtrl._devotedItemList[item]++;
                }
            }

            orig(self, lem, shouldEvolve);

            lemCtrl.ReturnItems();
        }

        private static void DevotionInventoryController_EvolveDevotedLumerian(ILContext il)
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
                    var list = PluginConfig.highTierElitesOnly.Value ? DevotionTweaks.gigaChadLvl.ToList() : DevotionTweaks.highLvl.ToList();

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

        private static void DevotionInventoryController_GenerateEliteBuff(ILContext ll)
        {
            var c = new ILCursor(ll);

            if (c.TryGotoNext(MoveType.Before,
                i => i.MatchBrtrue(out _),
                i => i.MatchLdsfld<DevotionInventoryController>(nameof(DevotionInventoryController.highLevelEliteBuffs)),
                i => i.MatchBr(out _),
                i => i.MatchLdsfld<DevotionInventoryController>(nameof(DevotionInventoryController.lowLevelEliteBuffs))
                ))
            {
                // fuck it just nuke it all
                c.RemoveRange(4);
                c.EmitDelegate<Func<bool, List<EquipmentIndex>>>((isLowLvl) =>
                {
                    return isLowLvl ? DevotionTweaks.lowLvl.ToList() : DevotionTweaks.highLvl.ToList();
                });
            }
            else
            {
                LemurFusionPlugin.LogError("Hook failed for DevotionInventoryController_GenerateEliteBuff");
            }
        }
        #endregion
    }
}
