using R2API;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;
using RoR2;
using System.Linq;

namespace DevotionTweaks
{
    internal class StatHooks
    {
        public static StatHooks instance;

        public static Vector3 baseSize = default;

        public StatHooks() 
        {
            instance = this;
        }

        public void InitHooks()
        {
            On.RoR2.UI.ScoreboardController.Rebuild += AddLemurianInventory;
            On.RoR2.CharacterBody.GetDisplayName += CharacterBody_GetDisplayName;
            On.RoR2.CharacterMaster.OnBodyStart += CharacterMaster_OnBodyStart;
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        public void RemoveHooks()
        {
            On.RoR2.UI.ScoreboardController.Rebuild -= AddLemurianInventory;
            On.RoR2.CharacterBody.GetDisplayName -= CharacterBody_GetDisplayName;
            On.RoR2.CharacterMaster.OnBodyStart -= CharacterMaster_OnBodyStart;
            RecalculateStatsAPI.GetStatCoefficients -= RecalculateStatsAPI_GetStatCoefficients;
        }

        #region Hooks
        private static void AddLemurianInventory(On.RoR2.UI.ScoreboardController.orig_Rebuild orig, RoR2.UI.ScoreboardController self)
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
            if (master)
            {
                foreach (MinionOwnership minionOwnership in MinionOwnership.MinionGroup.FindGroup(master.netId)?.members ?? [])
                {
                    if (minionOwnership && minionOwnership.gameObject.TryGetComponent<BetterLemurController>(out var lemCtrl))
                    {
                        masters.Add(lemCtrl._lemurianMaster);
                    }
                }
            }

            self.SetStripCount(masters.Count);
            for (int i = 0; i < masters.Count; i++)
            {
                self.stripAllocator.elements[i].SetMaster(masters[i]);
            }
        }

        private static string CharacterBody_GetDisplayName(On.RoR2.CharacterBody.orig_GetDisplayName orig, CharacterBody self)
        {
            var retv = orig(self);
            var meldCount = self?.inventory?.GetItemCount(CU8Content.Items.LemurianHarness);
            if (meldCount.HasValue && meldCount.Value > 0)
            {
                return $"{retv} <style=cStack>x{meldCount}</style>";
            }
            return retv;
        }

        private static void CharacterMaster_OnBodyStart(On.RoR2.CharacterMaster.orig_OnBodyStart orig, CharacterMaster self, CharacterBody body)
        {
            orig(self, body);
            if (NetworkClient.active)
            {
                var count = body?.inventory?.GetItemCount(CU8Content.Items.LemurianHarness);

                if (count.HasValue && count.Value > 0)
                    ResizeBody(count.Value, body);
            }
        }

        private static void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            var meldCount = sender?.inventory?.GetItemCount(CU8Content.Items.LemurianHarness);
            if (meldCount.HasValue && meldCount.Value > 0 && sender.masterObject.TryGetComponent<BetterLemurController>(out var lem))
            {
                args.baseHealthAdd += (sender.baseMaxHealth + sender.levelMaxHealth * sender.level) *
                    GetStatModifier(PluginConfig.statMultHealth.Value, meldCount.Value, lem.MultiplyStatsCount);

                args.baseDamageAdd += (sender.baseDamage + sender.levelDamage * sender.level) *
                    GetStatModifier(PluginConfig.statMultDamage.Value, meldCount.Value, lem.MultiplyStatsCount);

                args.baseAttackSpeedAdd += (sender.baseAttackSpeed + sender.levelAttackSpeed * sender.level) *
                    GetStatModifier(PluginConfig.statMultAttackSpeed.Value, meldCount.Value, lem.MultiplyStatsCount);
            }
        }
        #endregion

        #region Utils
        private static Vector3 GetScaleFactor(int configValue, int meldCount)
        {
            if (meldCount <= 1) return Vector3.one;

            return Vector3.one * ((meldCount - 1) * (configValue * 0.001f));
        }

        private static float GetStatModifier(int configValue, int meldCount, int multiplyStatsCount)
        {
            return (meldCount - 1) * (configValue * 0.01f) + (multiplyStatsCount * 0.1f);
        }

        internal static void ResizeBody(int meldCount, CharacterBody body)
        {
            if (PluginConfig.statMultSize.Value > 0 && meldCount > 1)
            {
                var transform = body?.modelLocator?.modelTransform; 
                if (transform)
                {
                    if (baseSize == default)
                        baseSize = transform.localScale;
                    var scaleFactor = Vector3.Scale(baseSize, GetScaleFactor(PluginConfig.statMultSize.Value, meldCount));

                    LemurFusionPlugin._logger.LogWarning(baseSize.ToString());
                    LemurFusionPlugin._logger.LogWarning(scaleFactor.ToString());

                    transform.localScale = baseSize + scaleFactor;

                    LemurFusionPlugin._logger.LogWarning(transform.localScale.ToString());
                }
            }
        }
        #endregion
    }
}
