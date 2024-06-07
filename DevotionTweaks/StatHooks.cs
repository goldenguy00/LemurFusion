using R2API;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;
using RoR2;
using System.Linq;
using LemurFusion.Config;
using System;

namespace LemurFusion
{
    internal class StatHooks
    {
        public static StatHooks instance;

        public static Vector3 baseSize = default;

        private StatHooks() { }

        public static void Init() 
        {
            if (instance != null) return;

            instance = new StatHooks();

            if (PluginConfig.miniElders.Value)
                On.RoR2.CharacterMaster.OnBodyStart += instance.CharacterMaster_OnBodyStart;
        }

        public void InitHooks()
        {
            On.RoR2.Util.GetBestMasterName += Util_GetBestMasterName;
            On.RoR2.CharacterBody.GetDisplayName += CharacterBody_GetDisplayName;
            On.RoR2.UI.ScoreboardController.Rebuild += ScoreboardController_Rebuild;
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        public void RemoveHooks()
        {
            On.RoR2.Util.GetBestMasterName -= Util_GetBestMasterName;
            On.RoR2.CharacterBody.GetDisplayName -= CharacterBody_GetDisplayName;
            On.RoR2.UI.ScoreboardController.Rebuild -= ScoreboardController_Rebuild;
            RecalculateStatsAPI.GetStatCoefficients -= RecalculateStatsAPI_GetStatCoefficients;
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
                        if (!PluginConfig.personalInventory.Value)
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
                if (!PluginConfig.personalInventory.Value) break;

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
        private void CharacterMaster_OnBodyStart(On.RoR2.CharacterMaster.orig_OnBodyStart orig, CharacterMaster self, CharacterBody body)
        {
            orig(self, body);
            if (NetworkClient.active)
            {
                var count = body?.inventory?.GetItemCount(CU8Content.Items.LemurianHarness);

                if (count.HasValue && count.Value > 0)
                    ResizeBody(count.Value, body);
            }
        }

        private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            var meldCount = sender?.inventory?.GetItemCount(CU8Content.Items.LemurianHarness);
            if (meldCount.HasValue && meldCount.Value > 0 && sender.masterObject.TryGetComponent<BetterLemurController>(out var lem))
            {
                args.baseHealthAdd += (sender.baseMaxHealth + sender.levelMaxHealth * sender.level) *
                    Utils.GetStatModifier(PluginConfig.statMultHealth.Value, meldCount.Value, lem.MultiplyStatsCount);

                args.baseDamageAdd += (sender.baseDamage + sender.levelDamage * sender.level) *
                    Utils.GetStatModifier(PluginConfig.statMultDamage.Value, meldCount.Value, lem.MultiplyStatsCount);

                args.baseAttackSpeedAdd += (sender.baseAttackSpeed + sender.levelAttackSpeed * sender.level) *
                    Utils.GetStatModifier(PluginConfig.statMultAttackSpeed.Value, meldCount.Value);

                // nerf later stage regen a bit
                // no clue what kinda curve this is, i just made some shit up.
                // I think regen to full starts at 50seconds and increases pretty quickly but idk i failed math
                args.baseRegenAdd += args.baseHealthAdd / (50f * Mathf.Pow(meldCount.Value, PluginConfig.statMultHealth.Value * 0.01f)); 
            }
        }

        private void ResizeBody(int meldCount, CharacterBody body)
        {
            // todo: fix this shit.
            if (PluginConfig.miniElders.Value)
            {
                var transform = body?.modelLocator?.modelTransform;
                if (transform)
                {
                    if (baseSize == default)
                        baseSize = transform.localScale;
                    transform.localScale = baseSize;
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
