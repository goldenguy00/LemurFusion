using System;
using System.Collections.Generic;
using System.Linq;
using R2API;
using RoR2;
using RoR2.CharacterAI;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using UnityEngine;
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

        public GameObject masterPrefab;
        public GameObject bodyPrefab;
        public GameObject bigBodyPrefab;

        public const string devotedPrefix = "Devoted";
        public const string lemBodyName = "LemurianBody";
        public const string bigLemBodyName = "LemurianBruiserBody";
        public const string devotedMasterName = "DevotedLemurianMaster";

        public const string devotedLemBodyName = devotedPrefix + lemBodyName;
        public const string devotedBigLemBodyName = devotedPrefix + bigLemBodyName;

        public bool EnableSharedInventory { get; private set; }

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

            // egg interaction display
            if (ConfigExtended.Blacklist_Enable.Value)
                On.RoR2.PickupPickerController.SetOptionsFromInteractor += PickupPickerController_SetOptionsFromInteractor;

            On.RoR2.BulletAttack.DefaultFilterCallbackImplementation += BulletAttack_DefaultFilterCallbackImplementation;
            On.RoR2.Projectile.ProjectileController.IgnoreCollisionsWithOwner += ProjectileController_IgnoreCollisionsWithOwner;
            IL.RoR2.SceneDirector.PopulateScene += PopulateScene;
        }

        private static void PopulateScene(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.Before,
                    i => i.MatchCall<RoR2.RunArtifactManager>("get_instance"),
                    i => i.MatchLdsfld("RoR2.CU8Content/Artifacts", "Devotion"),
                    i => i.MatchCallvirt<RoR2.RunArtifactManager>(nameof(RunArtifactManager.IsArtifactEnabled))
                ))
            {
                c.RemoveRange(3);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<SceneDirector, bool>>((self) =>
                {
                    return (PluginConfig.permaDevotion.Value || RunArtifactManager.instance.IsArtifactEnabled(CU8Content.Artifacts.Devotion)) &&
                            self.rng.RangeInt(0, 100) < PluginConfig.eggSpawnChance.Value;
                });
            }
            else
            {
                LemurFusionPlugin.LogError("IL Hook failed for SceneDirector_PopulateScene");
            }
        }

        private static bool BulletAttack_DefaultFilterCallbackImplementation(On.RoR2.BulletAttack.orig_DefaultFilterCallbackImplementation orig, BulletAttack bulletAttack, ref BulletAttack.BulletHit hitInfo)
        {
            return orig(bulletAttack, ref hitInfo) && !ShouldIgnoreAttackCollision(hitInfo.hitHurtBox, bulletAttack.owner);
        }

        private static bool ShouldIgnoreAttackCollision(HurtBox victimHurtBox, GameObject attacker)
        {
            return attacker && attacker.TryGetComponent(out TeamComponent attackerTeamComponent) &&
                attackerTeamComponent.teamIndex == TeamIndex.Player && victimHurtBox && victimHurtBox.healthComponent &&
                !FriendlyFireManager.ShouldDirectHitProceed(victimHurtBox.healthComponent, attackerTeamComponent.teamIndex);
        }

        private static void ProjectileController_IgnoreCollisionsWithOwner(On.RoR2.Projectile.ProjectileController.orig_IgnoreCollisionsWithOwner orig, RoR2.Projectile.ProjectileController self, bool shouldIgnore)
        {
            orig(self, shouldIgnore);

            if (!shouldIgnore || self.teamFilter.teamIndex != TeamIndex.Player || self.myColliders.Length == 0)
                return;

            foreach (var tc in TeamComponent.GetTeamMembers(TeamIndex.Player))
            {
                var body = tc.body;
                if (body && body.healthComponent && body.hurtBoxGroup && !FriendlyFireManager.ShouldSplashHitProceed(body.healthComponent, TeamIndex.Player))
                {
                    HurtBox[] hurtBoxes = body.hurtBoxGroup.hurtBoxes;
                    for (int i = 0; i < hurtBoxes.Length; i++)
                    {
                        List<Collider> gameObjectComponents = GetComponentsCache<Collider>.GetGameObjectComponents(hurtBoxes[i].gameObject);
                        int j = 0;
                        while (j < gameObjectComponents.Count)
                        {
                            Collider collider = gameObjectComponents[j];
                            for (int k = 0; k < self.myColliders.Length; k++)
                            {
                                Physics.IgnoreCollision(collider, self.myColliders[k]);
                            }
                            j++;
                        }
                        GetComponentsCache<Collider>.ReturnBuffer(gameObjectComponents);
                    }
                }
            }
        }

        #region Artifact Setup
        private void LoadAssets()
        {
            #region Clone
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
            #endregion

            #region Fix Vanilla
            lemurianBodyPrefab.GetComponent<CharacterBody>().bodyFlags &= ~CharacterBody.BodyFlags.Devotion;
            lemurianBruiserBodyPrefab.GetComponent<CharacterBody>().bodyFlags &= ~CharacterBody.BodyFlags.Devotion;

            var betterLemInvCtrlPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/DevotionMinionInventory");
            UE.Object.DestroyImmediate(betterLemInvCtrlPrefab.GetComponent<DevotionInventoryController>());
            betterLemInvCtrlPrefab.AddComponent<BetterInventoryController>().sfxLocator = betterLemInvCtrlPrefab.GetComponent<SfxLocator>();
            DevotionInventoryController.s_effectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OrbEffects/ItemTakenOrbEffect");

            masterPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/CU8/LemurianEgg/DevotedLemurianMaster.prefab").WaitForCompletion();
            UE.Object.DestroyImmediate(masterPrefab.GetComponent<DevotedLemurianController>());
            masterPrefab.AddComponent<BetterLemurController>();
            masterPrefab.GetComponent<CharacterMaster>().bodyPrefab = bodyPrefab;

            ItemDef itemDef = Addressables.LoadAssetAsync<ItemDef>("RoR2/CU8/Harness/LemurianHarness.asset").WaitForCompletion();
            if (itemDef && itemDef.ContainsTag(ItemTag.BrotherBlacklist))
                LemurFusionPlugin.LogError("YOURE A FUCKING IDIOT");
            if (itemDef && itemDef.ContainsTag(ItemTag.CannotSteal))
                LemurFusionPlugin.LogError("YOURE A FUCKING IDIOT again");
            if (itemDef) itemDef.tags = [.. itemDef.tags.Concat([ItemTag.BrotherBlacklist, ItemTag.CannotSteal]).Distinct()];
            #endregion
        }
        #endregion

        #region Summon
        private void PickupPickerController_SetOptionsFromInteractor(On.RoR2.PickupPickerController.orig_SetOptionsFromInteractor orig, PickupPickerController self, Interactor activator)
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
            self.SetOptionsServer([.. list]);
        }

        public CharacterMaster MasterSummon_Perform(On.RoR2.MasterSummon.orig_Perform orig, MasterSummon self)
        {
            if (self.masterPrefab == masterPrefab && self.summonerBodyObject && self.summonerBodyObject.TryGetComponent<CharacterBody>(out var body) && body.isPlayerControlled)
            {
                CharacterMaster lemMaster = null;
                int lowestCount = int.MaxValue;
                int lemCount = 0;

                var minionGroup = MinionOwnership.MinionGroup.FindGroup(body.masterObjectId);
                if (minionGroup != null)
                {
                    foreach (MinionOwnership minionOwnership in minionGroup.members)
                    {
                        if (minionOwnership && minionOwnership.GetComponent<CharacterMaster>().TryGetComponent<BetterLemurController>(out var lemCtrl))
                        {
                            lemCount++;
                            var fc = lemCtrl.FusionCount;
                            if (fc < lowestCount)
                            {
                                lowestCount = fc;
                                lemMaster = lemCtrl._lemurianMaster;
                            }
                        }
                    }
                }

                // successful lemurmeld
                if (lemMaster && lemCount >= PluginConfig.maxLemurs.Value)
                    return lemMaster;
            }
            return orig(self);
        }
        #endregion

    }
}
