using R2API;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;
using RoR2;
using System.Linq;
using LemurFusion.Config;
using System;
using LemurFusion.AI;

namespace LemurFusion
{
    internal class StatHooks
    {
        public static StatHooks instance;

        private StatHooks() { }

        public static void Init() 
        {
            if (instance != null) return;

            instance = new StatHooks();
        }

        public void InitHooks()
        {
            On.RoR2.Util.GetBestMasterName += Util_GetBestMasterName;
            On.RoR2.CharacterBody.GetDisplayName += CharacterBody_GetDisplayName;
            On.RoR2.UI.ScoreboardController.Rebuild += ScoreboardController_Rebuild;
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
            CharacterBody.onBodyStartGlobal += CharacterBody_onBodyStartGlobal;
        }

        public void RemoveHooks()
        {
            On.RoR2.Util.GetBestMasterName -= Util_GetBestMasterName;
            On.RoR2.CharacterBody.GetDisplayName -= CharacterBody_GetDisplayName;
            On.RoR2.UI.ScoreboardController.Rebuild -= ScoreboardController_Rebuild;
            RecalculateStatsAPI.GetStatCoefficients -= RecalculateStatsAPI_GetStatCoefficients;
            CharacterBody.onBodyStartGlobal -= CharacterBody_onBodyStartGlobal;
        }

        #region Hooks
        #region UI
        private string Util_GetBestMasterName(On.RoR2.Util.orig_GetBestMasterName orig, CharacterMaster characterMaster)
        {
            if (characterMaster && characterMaster.name == DevotionTweaks.masterCloneName && characterMaster.hasBody)
            {
                return characterMaster.GetBody().GetDisplayName();
            }
            return orig(characterMaster);
        }

        private string CharacterBody_GetDisplayName(On.RoR2.CharacterBody.orig_GetDisplayName orig, CharacterBody self)
        {
            var baseName = orig(self);
            if (!string.IsNullOrEmpty(baseName))
            {
                var meldCount = self?.inventory?.GetItemCount(CU8Content.Items.LemurianHarness);
                if (meldCount.HasValue && meldCount.Value > 0)
                {
                    return $"{baseName} <style=cStack>x{meldCount}</style>";
                }
            }
            return baseName;
        }

        private void ScoreboardController_Rebuild(On.RoR2.UI.ScoreboardController.orig_Rebuild orig, RoR2.UI.ScoreboardController self)
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
            }

            // fuck splitscreen players amirite
            var master = LocalUserManager.readOnlyLocalUsersList.First().cachedMasterController.master;
            List<BetterLemurController> lemCtrlList = [];
            if (master)
            {
                foreach (MinionOwnership minionOwnership in MinionOwnership.MinionGroup.FindGroup(master.netId)?.members ?? [])
                {
                    if (minionOwnership && minionOwnership.gameObject.TryGetComponent<BetterLemurController>(out var lemCtrl))
                    {
                        lemCtrlList.Add(lemCtrl);
                        if (!PluginConfig.showPersonalInventory.Value)
                            break;
                    }
                }
            }

            self.SetStripCount(masters.Count + lemCtrlList.Count);
            for (int i = 0; i < masters.Count; i++)
            {
                self.stripAllocator.elements[i].SetMaster(masters[i]);
            }
            for (int i = 0; i < lemCtrlList.Count; i++)
            {
                var lemCtrl = lemCtrlList[i];
                var strip = self.stripAllocator.elements[i + masters.Count];
                
                strip.SetMaster(lemCtrl._lemurianMaster);
                //lazy
                if (!PluginConfig.showPersonalInventory.Value) break;

                lemCtrl.LemurianInventory.onInventoryChanged -= strip.itemInventoryDisplay.OnInventoryChanged;

                strip.itemInventoryDisplay.itemOrderCount = lemCtrl._devotedItemList.Count;
                Array.Copy(lemCtrl.LemurianInventory.itemStacks, strip.itemInventoryDisplay.itemStacks, strip.itemInventoryDisplay.itemStacks.Length);
                Array.Copy(lemCtrl._devotedItemList.Keys.ToArray(), strip.itemInventoryDisplay.itemOrder, strip.itemInventoryDisplay.itemOrderCount);
                foreach (var item in lemCtrl._devotedItemList)
                {
                    strip.itemInventoryDisplay.itemStacks[(int)item.Key] = item.Value;
                }
                strip.itemInventoryDisplay.RequestUpdateDisplay();
            }
        }
        #endregion

        #region Stats
        private void CharacterBody_onBodyStartGlobal(CharacterBody body)
        {
            var count = body?.inventory?.GetItemCount(CU8Content.Items.LemurianHarness);

            if (count.HasValue && count.Value > 0)
            {
                if (AITweaks.disableFallDamage.Value)
                    body.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
                if (AITweaks.immuneToVoidDeath.Value)
                    body.bodyFlags |= CharacterBody.BodyFlags.ImmuneToVoidDeath;

                ResizeBody(count.Value, body);
            }
        }

        private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            var meldCount = sender?.inventory?.GetItemCount(CU8Content.Items.LemurianHarness);
            if (meldCount.HasValue && meldCount.Value > 0 && sender.masterObject.TryGetComponent<BetterLemurController>(out var lem))
            {
                if (PluginConfig.rebalanceHealthScaling.Value)
                {
                    args.levelRegenAdd += Utils.GetLevelModifier(lem.DevotedEvolutionLevel);
                    args.levelArmorAdd += Utils.GetLevelModifier(lem.DevotedEvolutionLevel);
                    args.levelHealthAdd = 43 * Utils.GetLevelModifier(lem.DevotedEvolutionLevel);

                    args.healthMultAdd += Utils.GetStatModifier(PluginConfig.statMultHealth.Value, meldCount.Value, lem.DevotedEvolutionLevel);
                    args.damageMultAdd += Utils.GetStatModifier(PluginConfig.statMultDamage.Value, meldCount.Value, lem.DevotedEvolutionLevel);
                    args.attackSpeedMultAdd += Utils.GetStatModifier(PluginConfig.statMultAttackSpeed.Value, meldCount.Value, lem.DevotedEvolutionLevel);
                }
                else
                {
                    args.healthMultAdd += Utils.GetVanillaStatModifier(PluginConfig.statMultHealth.Value, meldCount.Value, lem.DevotedEvolutionLevel);
                    args.damageMultAdd += Utils.GetVanillaStatModifier(PluginConfig.statMultHealth.Value, meldCount.Value, lem.DevotedEvolutionLevel);
                    args.attackSpeedMultAdd += Utils.GetVanillaStatModifier(PluginConfig.statMultHealth.Value, meldCount.Value, 0);
                }
            }
        }

        private void ResizeBody(int meldCount, CharacterBody body)
        {
            // todo: fix this shit.
            if (NetworkClient.active && PluginConfig.miniElders.Value)
            {
                var transform = body?.modelLocator?.modelTransform;
                if (transform)
                {
                    transform.localScale = Vector3.one * 0.2f;
                }
            }
            /*
            if (PluginConfig.miniElders.Value)
            {
                var transform = body?.modelLocator?.modelTransform; 
                if (transform)
                {
                    var scaleFactor = Vector3.Scale(baseSize, GetScaleFactor(PluginConfig.statMultSize.Value, meldCount));
                    transform.localScale = baseSize + scaleFactor;
                }
            }*/
        }
        #endregion
        #endregion
    }
}
