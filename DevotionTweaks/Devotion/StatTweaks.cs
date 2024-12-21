using R2API;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;
using RoR2;
using LemurFusion.Config;
using System.Linq;

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
                    lemBody.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage | CharacterBody.BodyFlags.ResistantToAOE;
                if (AITweaks.immuneToVoidDeath.Value)
                    lemBody.bodyFlags |= CharacterBody.BodyFlags.ImmuneToVoidDeath | CharacterBody.BodyFlags.OverheatImmune;

                if (PluginConfig.disableTeamCollision.Value)
                {
                    lemBody.gameObject.layer = LayerIndex.fakeActor.intVal;
                    if (lemBody.characterMotor)
                        lemBody.characterMotor.Motor.RebuildCollidableLayers();
                }

                if (NetworkClient.active)
                {
                    var lemModel = lemBody.modelLocator ? lemBody.modelLocator.modelTransform : null;
                    if (lemModel)
                    {
                        if (PluginConfig.miniElders.Value && lemBody.bodyIndex == BodyCatalog.FindBodyIndex(DevotionTweaks.devotedBigLemBodyName))
                        {
                            var init = PluginConfig.initScaleValue.Value * 0.01f;
                            var stack = PluginConfig.scaleValue.Value * 0.01f;
                            var count = lemBody.master.inventory.GetItemCount(CU8Content.Items.LemurianHarness);

                            lemModel.localScale = Vector3.one * (init + (stack * count));
                        }

                        if (lemModel.TryGetComponent<FootstepHandler>(out var footstep))
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
                    var scaledHealth = sender.level * sender.levelMaxHealth * Utils.GetLevelModifier(lem.DevotedEvolutionLevel);
                    args.baseHealthAdd += scaledHealth;
                    args.baseRegenAdd += scaledHealth * 0.01f * Utils.GetLevelModifier(lem.DevotedEvolutionLevel);
                    args.armorAdd += Utils.GetLevelModifier(lem.DevotedEvolutionLevel);

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
