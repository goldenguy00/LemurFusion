using R2API;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;
using RoR2;
using LemurFusion.Config;
using System.Linq;
using static RoR2.FriendlyFireManager;

namespace LemurFusion.Devotion
{
    public static class StatTweaks
    {
        public static void InitHooks()
        {
            On.RoR2.Util.GetBestMasterName += Util_GetBestMasterName;
            On.RoR2.CharacterBody.GetDisplayName += CharacterBody_GetDisplayName;
            On.RoR2.UI.ScoreboardController.Rebuild += ScoreboardController_Rebuild;
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
            CharacterBody.onBodyStartGlobal += CharacterBody_onBodyStartGlobal;
        }

        public static void RemoveHooks()
        {
            On.RoR2.Util.GetBestMasterName -= Util_GetBestMasterName;
            On.RoR2.CharacterBody.GetDisplayName -= CharacterBody_GetDisplayName;
            On.RoR2.UI.ScoreboardController.Rebuild -= ScoreboardController_Rebuild;
            RecalculateStatsAPI.GetStatCoefficients -= RecalculateStatsAPI_GetStatCoefficients;
            CharacterBody.onBodyStartGlobal -= CharacterBody_onBodyStartGlobal;
        }

        #region Hooks
        #region UI
        private static string Util_GetBestMasterName(On.RoR2.Util.orig_GetBestMasterName orig, CharacterMaster characterMaster) => Utils.IsDevoted(characterMaster) ? characterMaster.GetBody()?.GetDisplayName() : orig(characterMaster);

        private static string CharacterBody_GetDisplayName(On.RoR2.CharacterBody.orig_GetDisplayName orig, CharacterBody self)
        {
            var baseName = orig(self);
            if (!string.IsNullOrEmpty(baseName) && Utils.IsDevoted(self))
            {
                var lemCtrl = self.master.GetComponent<BetterLemurController>();
                var fusionCount = lemCtrl.LemurianInventory.GetItemCount(CU8Content.Items.LemurianHarness);
                if (fusionCount > 0)
                {
                    return $"{baseName} <style=cStack>x{fusionCount}</style>";
                }
            }
            return baseName;
        }

        private static void ScoreboardController_Rebuild(On.RoR2.UI.ScoreboardController.orig_Rebuild orig, RoR2.UI.ScoreboardController self)
        {
            orig(self);
            if (!PluginConfig.enableMinionScoreboard.Value)
                return;

            List<BetterLemurController> lems = [];
            foreach (var instance in PlayerCharacterMasterController.instances.Where(m => m.hasEffectiveAuthority))
            {
                var minionGroup = MinionOwnership.MinionGroup.FindGroup(instance.netId);
                if (minionGroup != null)
                {
                    foreach (var minionOwnership in minionGroup.members)
                    {
                        if (minionOwnership && minionOwnership.TryGetComponent<BetterLemurController>(out var lemCtrl) && lemCtrl.LemurianInventory)
                        {
                            lems.Add(lemCtrl);
                        }
                    }
                }
            }

            if (lems.Any())
            {
                int start = self.stripAllocator.elements.Count;
                self.SetStripCount(start + lems.Count);
                for (int i = 0; i < lems.Count; i++)
                {
                    self.stripAllocator.elements[start + i].SetMaster(lems[i]._lemurianMaster);
                    if (PluginConfig.showPersonalInventory.Value)
                        self.stripAllocator.elements[start + i].itemInventoryDisplay.SetSubscribedInventory(lems[i].PersonalInventory);
                }
            }
        }
        #endregion

        #region Stats
        private static void CharacterBody_onBodyStartGlobal(CharacterBody lemBody)
        {
            if (Utils.IsDevoted(lemBody))
            {
                if (AITweaks.disableFallDamage.Value)
                    lemBody.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
                if (AITweaks.immuneToVoidDeath.Value)
                    lemBody.bodyFlags |= CharacterBody.BodyFlags.ImmuneToVoidDeath | CharacterBody.BodyFlags.ResistantToAOE;
                
                if (NetworkServer.active && lemBody.hurtBoxGroup && lemBody.hurtBoxGroup.hurtBoxes.Length > 0)
                {
                    var hurtBoxes = lemBody.hurtBoxGroup.hurtBoxes;
                    foreach (var tc in TeamComponent.GetTeamMembers(TeamIndex.Player).Where(tc => tc && tc.body && tc.body.isPlayerControlled))
                    {
                        var teamBoxes = tc.body.hurtBoxGroup ? tc.body.hurtBoxGroup.hurtBoxes : null;
                        if (teamBoxes?.Any() == true && FriendlyFireManager.friendlyFireMode == FriendlyFireMode.Off)
                        {
                            for (int i = 0; i < teamBoxes.Length; i++)
                            {
                                for (int j = 0; j < hurtBoxes.Length; j++)
                                {
                                    Physics.IgnoreCollision(teamBoxes[i].collider, hurtBoxes[j].collider, true);
                                } // end for
                            } // end for
                        }
                    }
                }

                if (NetworkClient.active && lemBody.modelLocator)
                {
                    var transform = lemBody.modelLocator.modelTransform;
                    if (transform)
                    {
                        if (PluginConfig.miniElders.Value && lemBody.bodyIndex == BodyCatalog.FindBodyIndex(DevotionTweaks.devotedBigLemBodyName))
                        {
                            var init = PluginConfig.initScaleValue.Value * 0.01f;
                            var stack = PluginConfig.scaleValue.Value * 0.01f;
                            var count = lemBody.master.inventory.GetItemCount(CU8Content.Items.LemurianHarness);

                            transform.localScale = Vector3.one * (init + (stack * count));
                        }

                        if (transform.TryGetComponent<FootstepHandler>(out var footstep))
                            footstep.baseFootstepString = "";
                    }
                }
            }
        }

        private static void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (Utils.IsDevoted(sender) && sender.master.TryGetComponent<BetterLemurController>(out var lem) && lem.LemurianInventory)
            {
                var fusionCount = lem.LemurianInventory.GetItemCount(CU8Content.Items.LemurianHarness);
                if (PluginConfig.rebalanceHealthScaling.Value)
                {
                    args.baseHealthAdd += sender.level * sender.levelMaxHealth * Utils.GetLevelModifier(lem.DevotedEvolutionLevel);
                    args.baseRegenAdd += sender.level * (sender.outOfCombat ? Utils.GetLevelModifier(lem.DevotedEvolutionLevel) : Utils.GetLevelModifier(0));
                    args.armorAdd += 20f + Utils.GetLevelModifier(lem.DevotedEvolutionLevel);

                    args.healthMultAdd += Utils.GetFusionStatMultiplier(PluginConfig.statMultHealth.Value, fusionCount, lem.DevotedEvolutionLevel);
                    args.damageMultAdd += Utils.GetFusionStatMultiplier(PluginConfig.statMultDamage.Value, fusionCount, lem.DevotedEvolutionLevel);
                    args.attackSpeedMultAdd += Utils.GetFusionStatMultiplier(PluginConfig.statMultAttackSpeed.Value, fusionCount, lem.DevotedEvolutionLevel);
                }
                else
                {
                    args.healthMultAdd += Utils.GetVanillaStatModifier(PluginConfig.statMultHealth.Value, fusionCount, lem.DevotedEvolutionLevel);
                    args.damageMultAdd += Utils.GetVanillaStatModifier(PluginConfig.statMultHealth.Value, fusionCount, lem.DevotedEvolutionLevel);
                    args.attackSpeedMultAdd += Utils.GetVanillaStatModifier(PluginConfig.statMultHealth.Value, fusionCount, 0);
                }
            }
        }
        #endregion
        #endregion
    }
}
