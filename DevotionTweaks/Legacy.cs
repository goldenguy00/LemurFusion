using LemurFusion.Config;
using LemurFusion.Devotion.Tweaks;
using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Networking;

namespace LemurFusion
{
    internal class Legacy
    {

        public void CreateTwin(ItemIndex devotionItem)
        {
            var invCtrl = this.BetterInventoryController;
            if (invCtrl)
            {
                CharacterMaster ownerMaster = invCtrl._summonerMaster;
                if (ownerMaster)
                {
                    CharacterBody ownerBody = ownerMaster.GetBody();
                    if (ownerBody)
                    {
                        var transform = this.transform;
                        MasterSummon masterSummon = new()
                        {
                            masterPrefab = DevotionTweaks.masterPrefab,
                            position = transform.position,
                            rotation = transform.rotation,
                            summonerBodyObject = ownerBody.gameObject,
                            ignoreTeamMemberLimit = true,
                            useAmbientLevel = true
                        };
                        CharacterMaster twinMaster = masterSummon.Perform();

                        if (twinMaster && twinMaster.TryGetComponent<BetterLemurController>(out var lemCtrl))
                        {

                            lemCtrl.InitializeDevotedLemurian(devotionItem, invCtrl);

                            lemCtrl.DevotedEvolutionLevel = base.DevotedEvolutionLevel;
                            if (lemCtrl.DevotedEvolutionLevel > 1)
                                lemCtrl._lemurianMaster.TransformBody(DevotionTweaks.devotedBigLemBodyName);
                        }
                    }
                }
            }
        }
        /*if (PluginConfig.cloneReplacesRevive.Value)
        {
            if (itemIndex == RoR2Content.Items.ExtraLife.itemIndex)
            {
                lemCtrl.CreateTwin(RoR2Content.Items.Bear.itemIndex);
                itemIndex = RoR2Content.Items.ExtraLifeConsumed.itemIndex;
            }
            else if (itemIndex == DLC1Content.Items.ExtraLifeVoid.itemIndex)
            {
                lemCtrl.CreateTwin(DLC1Content.Items.BearVoid.itemIndex);
                itemIndex = DLC1Content.Items.ExtraLifeVoidConsumed.itemIndex;
            }
        }*/
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
                if (ConfigExtended.Blacklist_Enable.Value && DevotionTweaks.instance.EnableSharedInventory)
                {
                    c.Remove();
                    c.Emit<ConfigExtended>(OpCodes.Ldsfld, nameof(ConfigExtended.Blacklist_Filter));
                    c.Emit(OpCodes.Callvirt, typeof(Inventory).GetMethod(nameof(Inventory.AddItemsFrom), [typeof(Inventory), typeof(Func<ItemIndex, bool>)]));
                }
                if (!DevotionTweaks.instance.EnableSharedInventory)
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
                    if (DevotionTweaks.instance.EnableSharedInventory)
                        self.GiveItem(item);

                    lemCtrl._devotedItemList[item]++;
                }
            }

            orig(self, lem, shouldEvolve);

            lemCtrl.ReturnItems();
        }
    }
}
