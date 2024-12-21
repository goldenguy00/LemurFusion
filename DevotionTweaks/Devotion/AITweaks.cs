using BepInEx.Configuration;
using EntityStates.AI.Walker;
using LemurFusion.Devotion.Components;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.CharacterAI;
using System;
using UnityEngine;

namespace LemurFusion.Devotion
{
    public class AITweaks
    {
        public static AITweaks instance { get; private set; }

        public static ConfigEntry<bool> disableFallDamage;
        public static ConfigEntry<bool> immuneToVoidDeath;

        public static ConfigEntry<bool> improveAI;
        public static ConfigEntry<bool> enableProjectileTracking;
        public static ConfigEntry<bool> visualizeProjectileTracking;
        public static ConfigEntry<float> updateFrequency;
        public static ConfigEntry<int> detectionRadius;

        public const string SKILL_STRAFE_NAME = "StrafeAroundExplosion";
        public const string SKILL_ESCAPE_NAME = "BackUpFromExplosion";

        public static void Init() => instance ??= new AITweaks();

        private AITweaks()
        {
            if (improveAI.Value)
            {
                IL.EntityStates.AI.Walker.Combat.UpdateAI += Combat_UpdateAI;
                LemHitboxGroupRevealer.Init();

                var masterPrefab = DevotionTweaks.instance.masterPrefab;
                masterPrefab.AddComponent<MatrixDodgingController>();
                masterPrefab.AddComponent<LineRenderer>();
                var baseAI = masterPrefab.GetComponent<BaseAI>();
                baseAI.fullVision = true;
                baseAI.aimVectorDampTime = 0.05f;
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

                var skillDrivers = masterPrefab.GetComponents<AISkillDriver>();
                for (var i = 0; i < skillDrivers.Length; i++)
                {
                    var driver = skillDrivers[i];

                    switch (driver.customName)
                    {
                        case "DevotedSecondarySkill":
                            driver.minUserHealthFraction = 0.6f;
                            driver.shouldSprint = false;
                            driver.activationRequiresAimConfirmation = true;
                            driver.maxDistance = 10f;
                            break;
                        case "StrafeAndShoot":
                            driver.maxDistance = 100f;
                            driver.activationRequiresAimTargetLoS = true;
                            driver.shouldSprint = false;
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
                        return position - (fleeDirection * 0.5f);
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