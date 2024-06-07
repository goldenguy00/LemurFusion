using R2API;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;
using RoR2;
using System.Linq;
using LemurFusion.Config;

namespace LemurFusion
{
    internal class StatHooks
    {
        public static StatHooks instance;

        public static Vector3 baseSize = default;

        public StatHooks() 
        {
            instance = this;

            On.RoR2.CharacterBody.GetDisplayName += (orig, self) => { return Utils.ModifyName(orig(self), self); };
            //On.RoR2.CharacterBody.GetColoredUserName += (orig, self) => { return ModifyName(orig(self), self); };
            //On.RoR2.CharacterBody.GetUserName += (orig, self) => { return ModifyName(orig(self), self); };
            if (PluginConfig.miniElders.Value)
                On.RoR2.CharacterMaster.OnBodyStart += CharacterMaster_OnBodyStart;
        }

        public void InitHooks()
        {
            On.RoR2.UI.ScoreboardController.Rebuild += AddLemurianInventory;
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        public void RemoveHooks()
        {
            On.RoR2.UI.ScoreboardController.Rebuild -= AddLemurianInventory;
            //On.RoR2.CharacterMaster.OnBodyStart -= CharacterMaster_OnBodyStart;
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
        #endregion

        #region Utils
        internal static void ResizeBody(int meldCount, CharacterBody body)
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
    }
}
