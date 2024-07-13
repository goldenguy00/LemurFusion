using BepInEx.Configuration;
using EntityStates.AI.Walker;
using EntityStates.LemurianBruiserMonster;
using EntityStates.LemurianMonster;
using LemurFusion.Devotion.Components;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.CharacterAI;
using System;
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
                On.RoR2.CharacterAI.BaseAI.UpdateTargets += BaseAI_UpdateTargets;
                IL.EntityStates.AI.Walker.Combat.UpdateAI += Combat_UpdateAI;
                IL.EntityStates.LemurianMonster.FireFireball.OnEnter += FireFireball_OnEnter;
                IL.EntityStates.LemurianBruiserMonster.FireMegaFireball.FixedUpdate += FireMegaFireball_FixedUpdate;

                var masterPrefab = DevotionTweaks.instance.masterPrefab;
                masterPrefab.AddComponent<MatrixDodgingController>();
                var baseAI = masterPrefab.GetComponent<BaseAI>();
                baseAI.fullVision = true;
                baseAI.aimVectorDampTime = 0.1f;
                baseAI.aimVectorMaxSpeed = 200f;

                var component = masterPrefab.AddComponent<AISkillDriver>();
                component.customName = SKILL_ESCAPE_NAME;
                component.skillSlot = SkillSlot.Primary;
                component.maxDistance = detectionRadius.Value * 0.5f;
                component.minDistance = 0f;
                component.aimType = AISkillDriver.AimType.AtCurrentEnemy;
                component.moveTargetType = AISkillDriver.TargetType.Custom;
                component.movementType = AISkillDriver.MovementType.FleeMoveTarget;
                component.ignoreNodeGraph = true;
                component.shouldSprint = true;
                component.driverUpdateTimerOverride = Mathf.Clamp(updateFrequency.Value * 1.5f, 0.3f, 1f);

                var component2 = masterPrefab.AddComponent<AISkillDriver>();
                component2.customName = SKILL_STRAFE_NAME;
                component2.skillSlot = SkillSlot.Primary;
                component2.maxDistance = detectionRadius.Value;
                component2.minDistance = detectionRadius.Value * 0.5f;
                component2.aimType = AISkillDriver.AimType.AtCurrentEnemy;
                component2.moveTargetType = AISkillDriver.TargetType.Custom;
                component2.movementType = AISkillDriver.MovementType.StrafeMovetarget;
                component2.ignoreNodeGraph = true;
                component2.shouldSprint = true;
                component2.driverUpdateTimerOverride = Mathf.Clamp(updateFrequency.Value * 1.5f, 0.3f, 1f);
                component2.activationRequiresAimTargetLoS = true;
                component2.activationRequiresAimConfirmation = true;

                var skillDrivers = masterPrefab.GetComponents<AISkillDriver>();
                for (int i = 0; i < skillDrivers.Length; i++)
                {
                    var driver = skillDrivers[i];

                    switch (driver.customName)
                    {
                        case "DevotedSecondarySkill":
                            driver.minUserHealthFraction = 0.6f;
                            driver.shouldSprint = true;
                            driver.activationRequiresAimConfirmation = true;
                            driver.maxDistance = 10f;
                            break;
                        case "StrafeAndShoot":
                            driver.maxDistance = 100f;
                            driver.activationRequiresAimTargetLoS = true;
                            driver.shouldSprint = true;
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
                            driver.shouldSprint = true;
                            skillDrivers[0].nextHighPriorityOverride = driver;
                            break;
                        case "ChaseFarEnemies":
                        case "ReturnToOwnerLeash":
                        case "ChaseOffNodegraph":
                        case "StopAndShoot":
                        case SKILL_ESCAPE_NAME:
                        case SKILL_STRAFE_NAME:
                            break;
                    }
                }
            }
        }

        private static void BaseAI_UpdateTargets(On.RoR2.CharacterAI.BaseAI.orig_UpdateTargets orig, BaseAI self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                if (Utils.IsDevoted(self.master) && FuckMyAss.FuckingNullCheckPing(self.master.minionOwnership.ownerMaster, out var target))
                {
                    var targetBody = target.GetComponent<CharacterBody>();
                    if (targetBody && targetBody.master && TeamMask.GetEnemyTeams(self.master.teamIndex).HasTeam(targetBody.master.teamIndex))
                    {
                        LemurFusionPlugin.LogInfo("TARGET ACQUIRED");
                        self.currentEnemy.gameObject = target;
                    }
                }
            }
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
                    var body = self.characterBody;
                    if (Utils.IsDevoted(body))
                    {
                        return Utils.PredictAimray(body, aimRay, FireMegaFireball.projectilePrefab, FireMegaFireball.projectileSpeed);
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
                    var body = self.characterBody;
                    if (Utils.IsDevoted(body))
                    {
                        return Utils.PredictAimray(body, aimRay, FireFireball.projectilePrefab);
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
                    if (skillDriver && (skillDriver.customName == SKILL_STRAFE_NAME || skillDriver.customName == "StrafeNearbyEnemies"))
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