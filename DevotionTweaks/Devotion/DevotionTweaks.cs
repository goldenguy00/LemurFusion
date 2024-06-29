using System;
using System.Collections.Generic;
using System.Linq;
using R2API;
using RoR2;
using RoR2.CharacterAI;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using UnityEngine;
using UnityEngine.Networking;
using LemurFusion.Config;
using UnityEngine.AddressableAssets;
using UE = UnityEngine;
using LemurFusion.Devotion.Components;

namespace LemurFusion.Devotion
{
    public class DevotionTweaks
    {
        public enum DeathItem
        {
            None,
            Scrap,
            Original,
            Custom
        }

        public static DevotionTweaks instance { get; private set; }

        public static GameObject betterLemInvCtrlPrefab;
        public static GameObject masterPrefab;
        public static GameObject bodyPrefab;
        public static GameObject bigBodyPrefab;

        public const string devotedPrefix = "Devoted";
        public const string cloneSuffix = "(Clone)";

        public const string lemBodyName = "LemurianBody";
        public const string bigLemBodyName = "LemurianBruiserBody";

        public const string devotedMasterName = "BetterDevotedLemurianMaster";
        public const string devotedLemBodyName = devotedPrefix + lemBodyName;
        public const string devotedBigLemBodyName = devotedPrefix + bigLemBodyName;

        public static bool EnableSharedInventory { get; private set; }

        public static void Init()
        {
            if (instance != null)
                return;

            instance = new DevotionTweaks();
        }

        private DevotionTweaks()
        {
            EnableSharedInventory = PluginConfig.enableSharedInventory.Value;

            LoadAssets();

            // external lem hooks
            IL.RoR2.CharacterAI.LemurianEggController.SummonLemurian += LemurianEggController_SummonLemurian;

            // egg interaction display
            if (ConfigExtended.Blacklist_Enable.Value)
                On.RoR2.PickupPickerController.SetOptionsFromInteractor += PickupPickerController_SetOptionsFromInteractor;
        }

        #region Artifact Setup
        private void LoadAssets()
        {
            //Fix up the tags on the Harness
            LemurFusionPlugin.LogInfo("Adding Tags ItemTag.BrotherBlacklist, ItemTag.CannotSteal, ItemTag.CannotCopy to Lemurian Harness.");
            ItemDef itemDef = Addressables.LoadAssetAsync<ItemDef>("RoR2/CU8/Harness/LemurianHarness.asset").WaitForCompletion();
            if (itemDef)
            {
                itemDef.tags = itemDef.tags.Concat([ItemTag.BrotherBlacklist, ItemTag.CannotSteal]).Distinct().ToArray();
            }

            // dupe body
            GameObject lemurianBodyPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Lemurian/LemurianBody.prefab").WaitForCompletion();
            bodyPrefab = lemurianBodyPrefab.InstantiateClone(devotedLemBodyName, true);
            var body = bodyPrefab.GetComponent<CharacterBody>();
            body.bodyFlags |= CharacterBody.BodyFlags.Devotion;
            body.baseMaxHealth = 360f;
            body.levelMaxHealth = 11f;
            body.baseMoveSpeed = 7f;

            // dupe body pt2
            GameObject lemurianBruiserBodyPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LemurianBruiser/LemurianBruiserBody.prefab").WaitForCompletion();
            bigBodyPrefab = lemurianBruiserBodyPrefab.InstantiateClone(devotedBigLemBodyName, true);
            var bigBody = bigBodyPrefab.GetComponent<CharacterBody>();
            bigBody.bodyFlags |= CharacterBody.BodyFlags.Devotion;
            bigBody.baseMaxHealth = 720f;
            bigBody.levelMaxHealth = 22f;
            bigBody.baseMoveSpeed = 10f;

            // fix original
            lemurianBodyPrefab.GetComponent<CharacterBody>().bodyFlags &= ~CharacterBody.BodyFlags.Devotion;
            lemurianBruiserBodyPrefab.GetComponent<CharacterBody>().bodyFlags &= ~CharacterBody.BodyFlags.Devotion;

            // better master
            masterPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/CU8/LemurianEgg/DevotedLemurianMaster.prefab")
                .WaitForCompletion().InstantiateClone(devotedMasterName, true);
            UE.Object.DestroyImmediate(masterPrefab.GetComponent<DevotedLemurianController>());
            masterPrefab.AddComponent<BetterLemurController>();
            masterPrefab.GetComponent<CharacterMaster>().bodyPrefab = bodyPrefab;

            // better inventory ctrl
            betterLemInvCtrlPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/DevotionMinionInventory");
            UE.Object.DestroyImmediate(betterLemInvCtrlPrefab.GetComponent<DevotionInventoryController>());
            betterLemInvCtrlPrefab.AddComponent<BetterInventoryController>();
        }
        #endregion

        #region Summon
        private static void PickupPickerController_SetOptionsFromInteractor(On.RoR2.PickupPickerController.orig_SetOptionsFromInteractor orig, PickupPickerController self, Interactor activator)
        {
            if (!self.GetComponent<LemurianEggController>() || !activator || !activator.TryGetComponent<CharacterBody>(out var body) || !body.inventory)
            {
                orig(self, activator);
                return;
            }

            List<PickupPickerController.Option> list = [];
            foreach (var itemIndex in body.inventory.itemAcquisitionOrder)
            {
                if (ConfigExtended.Blacklist_Filter(itemIndex))
                {
                    PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(itemIndex);
                    if (pickupIndex != PickupIndex.none)
                    {
                        list.Add(new PickupPickerController.Option
                        {
                            available = true,
                            pickupIndex = pickupIndex
                        });
                    }
                }
            }
            self.SetOptionsServer(list.ToArray());
        }

        public static CharacterMaster MasterSummon_Perform(On.RoR2.MasterSummon.orig_Perform orig, MasterSummon self)
        {
            if (self.masterPrefab == masterPrefab && self.summonerBodyObject && self.summonerBodyObject.TryGetComponent<CharacterBody>(out var body) && body)
            {
                // successful lemurmeld
                // hijacking this var for CreateTwin_ExtraLife. fuck your config, you get another friend.
                var targetSummon = self.ignoreTeamMemberLimit ? null : TrySummon(body.masterObjectId);
                self.ignoreTeamMemberLimit = true;
                if (targetSummon)
                {
                    return targetSummon;
                }
            }
            return orig(self);
        }

        private static CharacterMaster TrySummon(NetworkInstanceId summoner)
        {
            List<BetterLemurController> lemCtrlList = [];
            foreach (MinionOwnership minionOwnership in MinionOwnership.MinionGroup.FindGroup(summoner)?.members ?? [])
            {
                if (minionOwnership && minionOwnership.gameObject.TryGetComponent<BetterLemurController>(out var lemCtrl) && lemCtrl)
                {
                    lemCtrlList.Add(lemCtrl);
                }
            }

            // replace summon with an existing guy
            // hijacking ignoreMaxAllies for CreateTwin_ExtraLife
            if (lemCtrlList.Count >= PluginConfig.maxLemurs.Value && lemCtrlList.Any())
            {
                var meldTarget = lemCtrlList.OrderBy(l => l.FusionCount).FirstOrDefault();
                if (meldTarget && meldTarget._lemurianMaster)
                {
                    return meldTarget._lemurianMaster;
                }
            }

            // let orig do the summoning
            return null;
        }

        private static void LemurianEggController_SummonLemurian(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.Before,
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<LemurianEggController>(nameof(LemurianEggController.masterPrefab))
                ))
            {
                c.RemoveRange(2);
                c.Emit<DevotionTweaks>(OpCodes.Ldsfld, nameof(masterPrefab));
            }
            else
            {
                LemurFusionPlugin.LogError("Hook failed for LemurianEggController_SummonLemurian # 1");
            }

            if (c.TryGotoNext(MoveType.Before,
                i => i.MatchStfld<MasterSummon>(nameof(MasterSummon.ignoreTeamMemberLimit))
                ))
            {
                c.Prev.OpCode = OpCodes.Ldc_I4_0;
            }
            else
            {
                LemurFusionPlugin.LogError("Hook failed for LemurianEggController_SummonLemurian # 2");
            }
        }
        #endregion

    }
}
