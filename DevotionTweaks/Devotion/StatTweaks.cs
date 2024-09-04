using R2API;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;
using RoR2;
using LemurFusion.Config;

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
        private static string Util_GetBestMasterName(On.RoR2.Util.orig_GetBestMasterName orig, CharacterMaster characterMaster)
        {
            if (Utils.IsDevoted(characterMaster))
            {
                return characterMaster.GetBody()?.GetDisplayName();
            }
            return orig(characterMaster);
        }

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
            {
                return;
            }

            List<CharacterMaster> masters = [];
            foreach (var instance in PlayerCharacterMasterController.instances)
            {
                masters.Add(instance.master);
                var minionGroup = MinionOwnership.MinionGroup.FindGroup(instance.master.netId);
                if (minionGroup != null)
                {
                    foreach (var minionOwnership in minionGroup.members)
                    {
                        if (minionOwnership && minionOwnership.TryGetComponent<BetterLemurController>(out var lemCtrl) && lemCtrl.LemurianInventory)
                        {
                            masters.Add(lemCtrl._lemurianMaster);
                        }
                    }
                }
            }

            self.SetStripCount(masters.Count);
            for (var i = 0; i < masters.Count; i++)
            {
                self.stripAllocator.elements[i].SetMaster(masters[i]);
            }
            /*
            self.SetStripCount(masters.Count + lemCtrlList.Count);
            for (var i = 0; i < masters.Count; i++)
            {
                self.stripAllocator.elements[i].SetMaster(masters[i]);
            }
            for (var i = 0; i < lemCtrlList.Count; i++)
            {
                var lemCtrl = lemCtrlList[i];
                var strip = self.stripAllocator.elements[i + masters.Count];
                strip.SetMaster(lemCtrl._lemurianMaster);

                //lazy
                if (!PluginConfig.showPersonalInventory.Value)
                    break;

                lemCtrl.LemurianInventory.onInventoryChanged -= strip.itemInventoryDisplay.OnInventoryChanged;

                strip.itemInventoryDisplay.itemOrderCount = lemCtrl._devotedItemList.Count;
                Array.Copy(lemCtrl.LemurianInventory.itemStacks, strip.itemInventoryDisplay.itemStacks, strip.itemInventoryDisplay.itemStacks.Length);
                Array.Copy(lemCtrl._devotedItemList.Keys.ToArray(), strip.itemInventoryDisplay.itemOrder, strip.itemInventoryDisplay.itemOrderCount);
                foreach (var item in lemCtrl._devotedItemList)
                {
                    strip.itemInventoryDisplay.itemStacks[(int)item.Key] = item.Value;
                }
                strip.itemInventoryDisplay.RequestUpdateDisplay();
            }*/
        }
        #endregion

        #region Stats
        private static void CharacterBody_onBodyStartGlobal(CharacterBody body)
        {
            if (Utils.IsDevoted(body))
            {
                if (AITweaks.disableFallDamage.Value)
                    body.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
                if (AITweaks.immuneToVoidDeath.Value)
                    body.bodyFlags |= CharacterBody.BodyFlags.ImmuneToVoidDeath | CharacterBody.BodyFlags.ResistantToAOE;

                if (body.modelLocator)
                {
                    var transform = body.modelLocator.modelTransform;
                    if (transform)
                    {
                        if (PluginConfig.miniElders.Value && body.bodyIndex == BodyCatalog.FindBodyIndex(DevotionTweaks.devotedBigLemBodyName) && NetworkClient.active)
                        {
                            var init = PluginConfig.initScaleValue.Value * 0.01f;
                            var stack = PluginConfig.scaleValue.Value * 0.01f;
                            var count = body.master.inventory.GetItemCount(CU8Content.Items.LemurianHarness);

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
            if (Utils.IsDevoted(sender) && sender.master.inventory && sender.master.TryGetComponent<BetterLemurController>(out var lem) && lem.LemurianInventory)
            {
                var fusionCount = lem.LemurianInventory.GetItemCount(CU8Content.Items.LemurianHarness);
                if (PluginConfig.rebalanceHealthScaling.Value)
                {
                    args.baseHealthAdd += sender.level * sender.levelMaxHealth * Utils.GetLevelModifier(lem.DevotedEvolutionLevel);
                    args.baseRegenAdd += sender.level * (sender.outOfCombat ? Utils.GetLevelModifier(lem.DevotedEvolutionLevel) : Utils.GetLevelModifier(0));
                    args.armorAdd += 20f + (5f * Utils.GetLevelModifier(lem.DevotedEvolutionLevel));

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
