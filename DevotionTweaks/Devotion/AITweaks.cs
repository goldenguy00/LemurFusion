using BepInEx.Configuration;
using EntityStates.AI.Walker;
using EntityStates.LemurianBruiserMonster;
using EntityStates.LemurianMonster;
using LemurFusion.Devotion.Components;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace LemurFusion.Devotion
{
    public class AITweaks
    {
        public static AITweaks instance { get; private set; }

        public static ConfigEntry<bool> disableFallDamage;
        public static ConfigEntry<bool> immuneToVoidDeath;

        public static ConfigEntry<bool> improveAI;
        public static ConfigEntry<bool> enablePredictiveAiming;
        public static ConfigEntry<bool> enableProjectileTracking;
        public static ConfigEntry<bool> visualizeProjectileTracking;
        public static ConfigEntry<float> updateFrequency;
        public static ConfigEntry<int> detectionRadius;

        public static float basePredictionAngle = 45f;

        public const string SKILL_STRAFE_NAME = "StrafeAroundExplosion";
        public const string SKILL_ESCAPE_NAME = "BackUpFromExplosion";

        public static void Init()
        {
            if (instance != null)
                return;

            instance = new AITweaks();
        }

        private AITweaks()
        {
            if (improveAI.Value)
            {
                RoR2Application.onLoad += PostLoad;

                On.RoR2.CharacterAI.BaseAI.UpdateTargets += BaseAI_UpdateTargets;
                IL.EntityStates.AI.Walker.Combat.UpdateAI += Combat_UpdateAI;
                IL.EntityStates.LemurianMonster.FireFireball.OnEnter += FireFireball_OnEnter;
                IL.EntityStates.LemurianBruiserMonster.FireMegaFireball.FixedUpdate += FireMegaFireball_FixedUpdate;

                var baseAI = DevotionTweaks.masterPrefab.GetComponent<BaseAI>();
                baseAI.fullVision = true;
                baseAI.aimVectorDampTime = 0.02f;
                baseAI.aimVectorMaxSpeed = 200f;

                var skillDrivers = DevotionTweaks.masterPrefab.GetComponents<AISkillDriver>();
                for (int i = 0; i < skillDrivers.Length; i++)
                {
                    var driver = skillDrivers[i];

                    switch (driver.customName)
                    {
                        case "DevotedSecondarySkill":
                            driver.minUserHealthFraction = 0.5f;
                            driver.noRepeat = true;
                            driver.nextHighPriorityOverride = skillDrivers[i + 1];
                            break;
                        case "StrafeAndShoot":
                            driver.maxDistance = 100f;
                            break;
                        case "ReturnToLeaderDefault":
                            driver.driverUpdateTimerOverride = 0.2f;
                            driver.shouldSprint = true;
                            driver.resetCurrentEnemyOnNextDriverSelection = true;
                            break;
                        case "WaitNearLeaderDefault":
                            driver.driverUpdateTimerOverride = 0.2f;
                            driver.resetCurrentEnemyOnNextDriverSelection = true;
                            break;
                        case "StrafeNearbyEnemies":
                        case "ChaseFarEnemies":
                        case "ReturnToOwnerLeash":
                        case "ChaseOffNodegraph":
                        case "StopAndShoot":
                            break;
                    }
                }

                var component = DevotionTweaks.masterPrefab.AddComponent<AISkillDriver>();
                component.customName = SKILL_ESCAPE_NAME;
                component.skillSlot = SkillSlot.None;
                component.maxDistance = detectionRadius.Value * 0.5f;
                component.minDistance = 0f;
                component.aimType = AISkillDriver.AimType.AtCurrentEnemy;
                component.moveTargetType = AISkillDriver.TargetType.Custom;
                component.movementType = AISkillDriver.MovementType.FleeMoveTarget;
                component.ignoreNodeGraph = true;
                component.shouldSprint = true;
                component.driverUpdateTimerOverride = updateFrequency.Value * 1.5f;
                component.resetCurrentEnemyOnNextDriverSelection = true;

                var component2 = DevotionTweaks.masterPrefab.AddComponent<AISkillDriver>();
                component2.customName = SKILL_STRAFE_NAME;
                component2.skillSlot = SkillSlot.Primary;
                component2.maxDistance = detectionRadius.Value;
                component2.minDistance = detectionRadius.Value * 0.5f;
                component2.aimType = AISkillDriver.AimType.AtCurrentEnemy;
                component2.moveTargetType = AISkillDriver.TargetType.Custom;
                component2.movementType = AISkillDriver.MovementType.StrafeMovetarget;
                component2.ignoreNodeGraph = true;
                component2.shouldSprint = true;
                component2.driverUpdateTimerOverride = updateFrequency.Value * 1.5f;
                component2.activationRequiresAimTargetLoS = true;
                component2.activationRequiresAimConfirmation = true;
                component2.resetCurrentEnemyOnNextDriverSelection = true;

                DevotionTweaks.masterPrefab.AddComponent<MatrixDodgingController>();
            }
        }

        private static void BaseAI_UpdateTargets(On.RoR2.CharacterAI.BaseAI.orig_UpdateTargets orig, BaseAI self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                if (self.master && self.master.minionOwnership && self.body.bodyFlags.HasFlag(CharacterBody.BodyFlags.Devotion))
                {
                    if (FuckingNullCheck(self.master.minionOwnership.ownerMaster, out var target))
                    {
                        var targetBody = target.GetComponent<CharacterBody>();
                        if (targetBody && targetBody.teamComponent.teamIndex != self.master.teamIndex)
                        {
                            self.currentEnemy.gameObject = target;
                        }
                    }
                }
            }
        }

        private static bool FuckingNullCheck(CharacterMaster master, out GameObject target)
        {
            // this is how you correctly nullcheck in unity.
            // fucking kill me in the face man.
            target = null;

            if (!master)
                return false;

            var minion = master.minionOwnership;
            if (!minion)
                return false;

            var ownerMaster = minion.ownerMaster;
            if (!ownerMaster)
                return false;

            var pCMC = ownerMaster.playerCharacterMasterController;
            if (!pCMC)
                return false;

            var pCtrl = pCMC.pingerController;
            if (!pCtrl)
                return false;

            target = pCtrl.currentPing.targetGameObject;
            return pCtrl.currentPing.active && target;
        }

        private static void PostLoad()
        {
            DevotionInventoryController.s_effectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OrbEffects/ItemTakenOrbEffect");
        }

        private static void FireMegaFireball_FixedUpdate(ILContext il)
        {
            ILCursor c = new(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchStloc(2)
                ))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc_2);
                c.EmitDelegate<Func<FireMegaFireball, Ray, Ray>>((self, aimRay) =>
                {
                    if (Utils.AllowPrediction(self.characterBody))
                    {
                        HurtBox targetHurtbox = Utils.GetMasterAITargetHurtbox(self.characterBody.master);

                        float projectileSpeed = FireMegaFireball.projectileSpeed;
                        if (projectileSpeed > 0f)
                        {
                            aimRay = Utils.PredictAimray(aimRay, projectileSpeed, targetHurtbox);
                        }
                        else
                        {
                            aimRay = Utils.PredictAimrayPS(aimRay, FireMegaFireball.projectilePrefab, targetHurtbox);
                        }
                    }
                    return aimRay;
                });
                c.Emit(OpCodes.Stloc_2);
            }
            else
            {
                LemurFusionPlugin.LogError("EntityStates.LemurianBruiserMonster.FireMegaFireball.FixedUpdate IL Hook failed");
            }
        }

        private static void FireFireball_OnEnter(ILContext il)
        {
            ILCursor c = new(il);
            if (c.TryGotoNext(MoveType.After,
                 i => i.MatchStloc(0)
                ))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc_0);
                c.EmitDelegate<Func<FireFireball, Ray, Ray>>((self, aimRay) =>
                {
                    if (Utils.AllowPrediction(self.characterBody))
                    {
                        HurtBox targetHurtbox = Utils.GetMasterAITargetHurtbox(self.characterBody.master);
                        Ray newAimRay = Utils.PredictAimrayPS(aimRay, FireFireball.projectilePrefab, targetHurtbox);
                        return newAimRay;
                    }
                    return aimRay;
                });
                c.Emit(OpCodes.Stloc_0);
            }
            else
            {
                LemurFusionPlugin.LogError("EntityStates.LemurianMonster.FireFireball.OnEnter IL Hook failed");
            }
        }

        private static void Combat_UpdateAI(ILContext il)
        {
            ILCursor c = new(il);

            if (c.TryGotoNext(MoveType.Before,
                    i => i.MatchLdcI4(0),
                    i => i.MatchStloc(13)
                ))
            {
                c.Emit(OpCodes.Ldloc, 12);
                c.Emit(OpCodes.Ldloc, 15);
                c.Emit(OpCodes.Ldarg_0);
                c.Emit<Combat>(OpCodes.Ldfld, nameof(Combat.dominantSkillDriver));
                c.EmitDelegate<Func<Vector3, Vector3, AISkillDriver, Vector3>>((position, fleeDirection, skillDriver) =>
                {
                    if (skillDriver && skillDriver.customName == SKILL_STRAFE_NAME)
                    {
                        return position - fleeDirection * 0.5f;
                    }
                    return position;
                });
                c.Emit(OpCodes.Stloc, 12);
            }
            else
            {
                LemurFusionPlugin.LogError("EntityStates.AI.Walker.Combat.UpdateAI IL Hook failed");
            }
        }

    }
}