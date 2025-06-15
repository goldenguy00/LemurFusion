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
using RoR2.Projectile;
using static RoR2.FriendlyFireManager;
using HarmonyLib;
using RoR2.Skills;
using RoR2BepInExPack.GameAssetPaths;

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
        public GameObject bruiserMasterPrefab;
        public GameObject bodyPrefab;
        public GameObject bigBodyPrefab;

        public const string devotedPrefix = "Devoted";
        public const string devotedMasterName = "DevotedLemurianMaster";
        public const string devotedBruiserMasterName = "DevotedLemurianBruiserMaster";

        public const string devotedLemBodyName = devotedPrefix + "LemurianBody";
        public const string devotedBigLemBodyName = devotedPrefix + "LemurianBruiserBody";

        public readonly bool EnableSharedInventory;

        public static void Init() => instance ??= new DevotionTweaks();

        private DevotionTweaks()
        {
            EnableSharedInventory = PluginConfig.enableSharedInventory.Value;

            LoadAssets();

            // egg interaction display
            if (ConfigExtended.Blacklist_Enable.Value)
                On.RoR2.PickupPickerController.SetOptionsFromInteractor += PickupPickerController_SetOptionsFromInteractor;

            IL.RoR2.SceneDirector.PopulateScene += PopulateScene;

            On.RoR2.AffixAurelioniteBehavior.OnServerDamageDealt += AffixAurelioniteBehavior_OnServerDamageDealt;
            On.EntityStates.LemurianMonster.Bite.OnEnter += Bite_OnEnter;
            On.EntityStates.LemurianBruiserMonster.Flamebreath.FireFlame += Flamebreath_FireFlame;

            if (PluginConfig.disableTeamCollision.Value)
            {
                On.RoR2.BulletAttack.DefaultFilterCallbackImplementation += BulletAttack_DefaultFilterCallbackImplementation;
                On.RoR2.Projectile.ProjectileController.IgnoreCollisionsWithOwner += ProjectileController_IgnoreCollisionsWithOwner;
            }
        }

        private static void Flamebreath_FireFlame(On.EntityStates.LemurianBruiserMonster.Flamebreath.orig_FireFlame orig, EntityStates.LemurianBruiserMonster.Flamebreath self, string muzzleString)
        {
            if (self.bulletAttack != null)
                self.bulletAttack.damageType.damageSource = DamageSource.Secondary;

            orig(self, muzzleString);
        }

        private static void Bite_OnEnter(On.EntityStates.LemurianMonster.Bite.orig_OnEnter orig, EntityStates.LemurianMonster.Bite self)
        {
            orig(self);

            self.attack.damageType.damageSource = DamageSource.Secondary;
        }

        private static void AffixAurelioniteBehavior_OnServerDamageDealt(On.RoR2.AffixAurelioniteBehavior.orig_OnServerDamageDealt orig, AffixAurelioniteBehavior self, DamageReport damageReport)
        {
            if (Utils.IsDevoted(damageReport.attackerBody, out _) && !damageReport.damageInfo.damageType.IsDamageSourceSkillBased)
                return;

            orig(self, damageReport);
        }

        #region Misc
        private static bool BulletAttack_DefaultFilterCallbackImplementation(On.RoR2.BulletAttack.orig_DefaultFilterCallbackImplementation orig, BulletAttack bulletAttack, ref BulletAttack.BulletHit hitInfo)
        {
            return orig(bulletAttack, ref hitInfo)
                && !(hitInfo.hitHurtBox
                && hitInfo.hitHurtBox.healthComponent
                && bulletAttack.owner
                && bulletAttack.owner.TryGetComponent<TeamComponent>(out var attackerTeamComponent)
                && attackerTeamComponent.teamIndex == TeamIndex.Player
                && !FriendlyFireManager.ShouldDirectHitProceed(hitInfo.hitHurtBox.healthComponent, attackerTeamComponent.teamIndex));
        }

        private static void ProjectileController_IgnoreCollisionsWithOwner(On.RoR2.Projectile.ProjectileController.orig_IgnoreCollisionsWithOwner orig, ProjectileController self, bool shouldIgnore)
        {
            orig(self, shouldIgnore);

            if (!shouldIgnore || FriendlyFireManager.friendlyFireMode != FriendlyFireMode.Off || self.teamFilter.teamIndex != TeamIndex.Player || self.myColliders.Length == 0 || !self.owner)
            {
                return;
            }

            foreach (var tc in TeamComponent.GetTeamMembers(TeamIndex.Player))
            {
                if (Utils.IsDevoted(tc.body, out _) && tc.body.hurtBoxGroup && tc.gameObject != self.owner)
                {
                    var hurtBoxes = tc.body.hurtBoxGroup.hurtBoxes;
                    for (int i = 0; i < hurtBoxes.Length; i++)
                    {
                        for (int j = 0; j < self.myColliders.Length; j++)
                        {
                            Physics.IgnoreCollision(hurtBoxes[i].collider, self.myColliders[j], shouldIgnore);
                        } // end for
                    } // end for
                }
            } // endforeach
        }

        private void LoadAssets()
        {
            bodyPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2_CU8.DevotedLemurianBody_prefab).WaitForCompletion();
            var body = bodyPrefab.GetComponent<CharacterBody>();
            body.bodyFlags |= CharacterBody.BodyFlags.Devotion | CharacterBody.BodyFlags.ImmuneToLava | CharacterBody.BodyFlags.Mechanical;
            body.baseMaxHealth = 360f;
            body.levelMaxHealth = 11f;
            body.baseMoveSpeed = 7f;

            bigBodyPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2_CU8.DevotedLemurianBruiserBody_prefab).WaitForCompletion();
            var bigBody = bigBodyPrefab.GetComponent<CharacterBody>();
            bigBody.bodyFlags |= CharacterBody.BodyFlags.Devotion | CharacterBody.BodyFlags.ImmuneToLava | CharacterBody.BodyFlags.Mechanical;
            bigBody.baseMaxHealth = 720f;
            bigBody.levelMaxHealth = 11f;
            bigBody.baseMoveSpeed = 10f;
            bigBody.baseArmor = 0;
            bigBody.levelArmor = 0.5f;

            var betterLemInvCtrlPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/DevotionMinionInventory");
            UE.Object.DestroyImmediate(betterLemInvCtrlPrefab.GetComponent<DevotionInventoryController>());
            betterLemInvCtrlPrefab.AddComponent<BetterInventoryController>().sfxLocator = betterLemInvCtrlPrefab.GetComponent<SfxLocator>();
            BetterInventoryController.s_effectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OrbEffects/ItemTakenOrbEffect");
            LegacyResourcesAPI.LoadAsyncCallback("NetworkSoundEventDefs/nseNullifiedBuffApplied", delegate (NetworkSoundEventDef asset)
            {
                BetterInventoryController.activationSoundEventDef = asset;
            });

            masterPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2_CU8_LemurianEgg.DevotedLemurianMaster_prefab).WaitForCompletion();
            UE.Object.DestroyImmediate(masterPrefab.GetComponent<DevotedLemurianController>());
            masterPrefab.AddComponent<BetterLemurController>();
            masterPrefab.AddComponent<Inventory>();
            masterPrefab.GetComponent<CharacterMaster>().bodyPrefab = bodyPrefab;

            bruiserMasterPrefab = masterPrefab.InstantiateClone(devotedBruiserMasterName, true);
            bruiserMasterPrefab.GetComponent<CharacterMaster>().bodyPrefab = bigBodyPrefab;

            Addressables.LoadAssetAsync<SkillDef>(RoR2_Base_LemurianBruiser.LemurianBruiserBodyPrimary_asset).Completed +=
                (task) => task.Result.interruptPriority = EntityStates.InterruptPriority.Any;

            Addressables.LoadAssetAsync<SkillDef>(RoR2_Base_LemurianBruiser.LemurianBruiserBodySecondary_asset).Completed +=
                (task) => task.Result.interruptPriority = EntityStates.InterruptPriority.Any;

            var itemDef = Addressables.LoadAssetAsync<ItemDef>(RoR2_CU8_Harness.LemurianHarness_asset).WaitForCompletion();
            itemDef.tags = [.. itemDef.tags.Concat([ItemTag.BrotherBlacklist, ItemTag.CannotSteal]).Distinct()];

            var fireball = Addressables.LoadAssetAsync<GameObject>(RoR2_Base_Lemurian.Fireball_prefab).WaitForCompletion();
            fireball.GetComponent<ProjectileDamage>().damageType.damageSource = DamageSource.Primary;
            var fireball2 = Addressables.LoadAssetAsync<GameObject>(RoR2_Base_LemurianBruiser.LemurianBigFireball_prefab).WaitForCompletion();
            fireball.GetComponent<ProjectileDamage>().damageType.damageSource = DamageSource.Primary;
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
                    var pickupIndex = PickupCatalog.FindPickupIndex(itemIndex);
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
            if (self.masterPrefab && self.masterPrefab == this.masterPrefab && self.summonerBodyObject && self.summonerBodyObject.TryGetComponent<CharacterBody>(out var summonerBody) && summonerBody.isPlayerControlled)
            {
                CharacterMaster lemMaster = null;
                var lowestCount = int.MaxValue;
                var lemCount = 0;

                var minionGroup = MinionOwnership.MinionGroup.FindGroup(summonerBody.masterObjectId);
                if (minionGroup != null)
                {
                    foreach (var minionOwnership in minionGroup.members)
                    {
                        if (minionOwnership && minionOwnership.TryGetComponent<BetterLemurController>(out var lemCtrl) && lemCtrl.LemurianInventory)
                        {
                            lemCount++;
                            var fusionCount = lemCtrl.FusionCount;
                            if (fusionCount < lowestCount)
                            {
                                lowestCount = fusionCount;
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

        private static void PopulateScene(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.After,
                    i => i.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(RunArtifactManager), nameof(RunArtifactManager.instance))),
                    i => i.MatchLdsfld("RoR2.CU8Content/Artifacts", "Devotion"),
                    i => i.MatchCallOrCallvirt<RunArtifactManager>(nameof(RunArtifactManager.IsArtifactEnabled))
                ))
            {
                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Ldc_I4_0);
            }
            else
            {
                LemurFusionPlugin.LogError("IL Hook failed for SceneDirector_PopulateScene 1");
            }

            if (c.TryGotoNext(i => i.MatchNewobj(typeof(DirectorPlacementRule))) &&
                c.TryGotoNext(i => i.MatchNewobj(typeof(DirectorSpawnRequest))) &&
                c.TryGotoPrev(MoveType.After,
                    x => x.MatchCallOrCallvirt<DirectorCard>(nameof(DirectorCard.GetSpawnCard))
                ))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<SpawnCard, SceneDirector, SpawnCard>>((originalCard, self) =>
                {
                    if (!DevotionInventoryController.isDevotionEnable)
                        return originalCard;

                    if (!(originalCard is InteractableSpawnCard originalIsc && originalIsc.skipSpawnWhenDevotionArtifactEnabled))
                        return originalCard;

                    if (self.rng.RangeInt(0, 100) < Math.Clamp(PluginConfig.eggSpawnChance.Value, 0, 100))
                    {
                        return Addressables.LoadAssetAsync<SpawnCard>(RoR2_CU8_LemurianEgg.iscLemurianEgg_asset).WaitForCompletion();
                    }

                    return originalCard;
                });
            }
            else
            {
                LemurFusionPlugin.LogError("IL Hook failed for SceneDirector_PopulateScene 2");
            }
        }
        #endregion
    }
}
