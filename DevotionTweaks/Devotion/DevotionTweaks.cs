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

            if (PluginConfig.disableTeamCollision.Value)
            {
                On.RoR2.BulletAttack.DefaultFilterCallbackImplementation += BulletAttack_DefaultFilterCallbackImplementation;
                On.RoR2.Projectile.ProjectileController.IgnoreCollisionsWithOwner += ProjectileController_IgnoreCollisionsWithOwner;
            }
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
            if (!shouldIgnore || self.teamFilter.teamIndex != TeamIndex.Player || self.myColliders.Length == 0 || !self.owner || !self.owner.TryGetComponent<CharacterBody>(out var ownerBody))
            {
                orig(self, shouldIgnore);
                return;
            }

            foreach (var tc in TeamComponent.GetTeamMembers(TeamIndex.Player))
            {
                var body = tc.body;
                if (body && body.hurtBoxGroup && (body == ownerBody || FriendlyFireManager.friendlyFireMode == FriendlyFireMode.Off))
                {
                    var hurtBoxes = body.hurtBoxGroup.hurtBoxes;
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
            bodyPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/CU8/DevotedLemurianBody.prefab").WaitForCompletion();
            var body = bodyPrefab.GetComponent<CharacterBody>();
            body.bodyFlags |= CharacterBody.BodyFlags.Devotion;
            body.baseMaxHealth = 360f;
            body.levelMaxHealth = 11f;
            body.baseMoveSpeed = 7f;

            bigBodyPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/CU8/DevotedLemurianBruiserBody.prefab").WaitForCompletion();
            var bigBody = bigBodyPrefab.GetComponent<CharacterBody>();
            bigBody.bodyFlags |= CharacterBody.BodyFlags.Devotion;
            bigBody.baseMaxHealth = 720f;
            bigBody.levelMaxHealth = 22f;
            bigBody.baseMoveSpeed = 10f;

            var betterLemInvCtrlPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/DevotionMinionInventory");
            UE.Object.DestroyImmediate(betterLemInvCtrlPrefab.GetComponent<DevotionInventoryController>());
            betterLemInvCtrlPrefab.AddComponent<BetterInventoryController>().sfxLocator = betterLemInvCtrlPrefab.GetComponent<SfxLocator>();
            DevotionInventoryController.s_effectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OrbEffects/ItemTakenOrbEffect");

            masterPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/CU8/LemurianEgg/DevotedLemurianMaster.prefab").WaitForCompletion();
            UE.Object.DestroyImmediate(masterPrefab.GetComponent<DevotedLemurianController>());
            masterPrefab.AddComponent<BetterLemurController>();
            masterPrefab.AddComponent<Inventory>();
            masterPrefab.GetComponent<CharacterMaster>().bodyPrefab = bodyPrefab;
            bruiserMasterPrefab = masterPrefab.InstantiateClone(devotedBruiserMasterName, true);
            bruiserMasterPrefab.GetComponent<CharacterMaster>().bodyPrefab = bigBodyPrefab;

            var itemDef = Addressables.LoadAssetAsync<ItemDef>("RoR2/CU8/Harness/LemurianHarness.asset").WaitForCompletion();
            itemDef.tags = [.. itemDef.tags.Concat([ItemTag.BrotherBlacklist, ItemTag.CannotSteal]).Distinct()];
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
                            var fusionCount = lemCtrl.LemurianInventory.GetItemCount(CU8Content.Items.LemurianHarness);
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

            if (c.TryGotoNext(MoveType.Before,
                    i => i.MatchCall<RunArtifactManager>("get_instance"),
                    i => i.MatchLdsfld("RoR2.CU8Content/Artifacts", "Devotion"),
                    i => i.MatchCallvirt<RunArtifactManager>(nameof(RunArtifactManager.IsArtifactEnabled))
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
        #endregion
    }
}
