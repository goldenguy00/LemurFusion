using BepInEx.Configuration;
using EntityStates.LemurianBruiserMonster;
using EntityStates.LemurianMonster;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LemurFusion.AI
{
    public class AITweaks
    {
        public static AITweaks instance;

        public static ConfigEntry<bool> disableFallDamage;
        public static ConfigEntry<bool> immuneToVoidDeath;

        public static ConfigEntry<bool> improveAI;
        public static ConfigEntry<bool> enablePredictiveAiming;
        public static ConfigEntry<bool> enableProjectileTracking;
        public static ConfigEntry<bool> visualizeProjectileTracking;

        public static HashSet<int> projectileIds = [];
        public static HashSet<int> pain = [];

        public static float basePredictionAngle = 45f;

        public const string SKILL_STRAFE_NAME = "StrafeAroundExplosion";
        public const string SKILL_ESCAPE_NAME = "BackUpFromExplosion";

        public static void Init()
        {
            if (instance != null) return;

            instance = new AITweaks();
        }

        private AITweaks()
        {
            if (improveAI.Value)
            {
                RoR2Application.onLoad += PostLoad;

                foreach (var driver in DevotionTweaks.masterPrefab.GetComponentsInChildren<AISkillDriver>())
                {
                    switch (driver.customName)
                    {
                        case "ReturnToLeaderDefault":
                            driver.driverUpdateTimerOverride = 0.2f;
                            driver.shouldSprint = true;
                            driver.resetCurrentEnemyOnNextDriverSelection = true;
                            break;
                        case "WaitNearLeader":
                            driver.driverUpdateTimerOverride = 0.2f;
                            driver.resetCurrentEnemyOnNextDriverSelection = true;
                            break;
                    }
                }

                var component = DevotionTweaks.masterPrefab.AddComponent<AISkillDriver>();
                component.customName = SKILL_ESCAPE_NAME;
                component.skillSlot = SkillSlot.None;
                component.maxDistance = 20f;
                component.minDistance = 0f;
                component.aimType = AISkillDriver.AimType.AtCurrentEnemy;
                component.moveTargetType = AISkillDriver.TargetType.Custom;
                component.movementType = AISkillDriver.MovementType.FleeMoveTarget;
                component.ignoreNodeGraph = true;
                component.shouldSprint = true;
                component.driverUpdateTimerOverride = 0.2f;

                var component2 = DevotionTweaks.masterPrefab.AddComponent<AISkillDriver>();
                component2.customName = SKILL_STRAFE_NAME;
                component2.skillSlot = SkillSlot.None;
                component2.maxDistance = 30f;
                component2.minDistance = 20f;
                component2.aimType = AISkillDriver.AimType.AtCurrentEnemy;
                component2.moveTargetType = AISkillDriver.TargetType.Custom;
                component2.movementType = AISkillDriver.MovementType.StrafeMovetarget;
                component2.ignoreNodeGraph = false;
                component2.shouldSprint = true;
                component2.driverUpdateTimerOverride = 0.5f;
                component2.moveInputScale = 0.8f;
                component2.resetCurrentEnemyOnNextDriverSelection = true;

                component.nextHighPriorityOverride = component2;

                DevotionTweaks.masterPrefab.AddComponent<MatrixDodgingController>();

                EnablePrediction();
            }
        }

        public static void PostLoad()
        {
            foreach (var projectile in ProjectileCatalog.projectilePrefabProjectileControllerComponents)
            {
                var prefab = ProjectileCatalog.GetProjectilePrefab(projectile.catalogIndex);
                if (projectile.GetComponent<ProjectileDotZone>() || projectile.GetComponent<ProjectileFuse>() ||
                    projectile.GetComponent<DeathProjectile>() || projectile.GetComponent<ProjectileImpactExplosion>())
                {
                    if (projectile.GetComponentInChildren<Collider>())
                    {
                        projectileIds.Add(projectile.catalogIndex);
                        LemurFusionPlugin.LogInfo($"Adding projectile by component {prefab.name}");
                    }
                }
            }
        }

        private static void EnablePrediction()
        {
            IL.EntityStates.LemurianMonster.FireFireball.OnEnter += (il) =>
            {
                ILCursor c = new(il);
                if (c.TryGotoNext(MoveType.After,
                     x => x.MatchCall<EntityStates.BaseState>("GetAimRay")
                    ))
                {
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate<Func<Ray, FireFireball, Ray>>((aimRay, self) =>
                    {
                        if (AITweaks.AllowPrediction(self.characterBody))
                        {
                            HurtBox targetHurtbox = AITweaks.GetMasterAITargetHurtbox(self.characterBody.master);
                            Ray newAimRay = AITweaks.PredictAimrayPS(aimRay, FireFireball.projectilePrefab, targetHurtbox);
                            return newAimRay;
                        }
                        return aimRay;
                    });
                }
                else
                {
                    LemurFusionPlugin.LogError("EntityStates.LemurianMonster.FireFireball.OnEnter IL Hook failed");
                }
            };

            IL.EntityStates.LemurianBruiserMonster.FireMegaFireball.FixedUpdate += (il) =>
            {
                ILCursor c = new(il);
                if (c.TryGotoNext(MoveType.After,
                     x => x.MatchCall<EntityStates.BaseState>("GetAimRay")
                    ))
                {
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate<Func<Ray, FireMegaFireball, Ray>>((aimRay, self) =>
                    {
                        if (AITweaks.AllowPrediction(self.characterBody))
                        {
                            HurtBox targetHurtbox = AITweaks.GetMasterAITargetHurtbox(self.characterBody.master);
                            Ray newAimRay;

                            float projectileSpeed = FireMegaFireball.projectileSpeed;
                            if (projectileSpeed > 0f)
                            {
                                newAimRay = AITweaks.PredictAimray(aimRay, projectileSpeed, targetHurtbox);
                            }
                            else
                            {
                                newAimRay = AITweaks.PredictAimrayPS(aimRay, FireMegaFireball.projectilePrefab, targetHurtbox);
                            }

                            return newAimRay;
                        }
                        return aimRay;
                    });
                }
                else
                {
                    LemurFusionPlugin.LogError("EntityStates.LemurianBruiserMonster.FireMegaFireball.FixedUpdate IL Hook failed");
                }
            };
        }

        private static bool AllowPrediction(CharacterBody body)
        {
            return body && body.master && body.master.name.Contains(DevotionTweaks.masterPrefabName) && body.teamComponent.teamIndex == TeamIndex.Player;
        }

        private static Ray PredictAimrayPS(Ray aimRay, GameObject projectilePrefab, HurtBox targetHurtBox)
        {
            float speed = -1f;
            if (projectilePrefab)
            {
                ProjectileSimple ps = projectilePrefab.GetComponent<ProjectileSimple>();
                if (ps)
                {
                    speed = ps.desiredForwardSpeed;
                }
            }

            if (speed <= 0f)
            {
                LemurFusionPlugin.LogError("Could not get speed of ProjectileSimple.");
                return aimRay;
            }

            return PredictAimray(aimRay, speed, targetHurtBox);
        }

        private static Ray PredictAimray(Ray aimRay, float projectileSpeed, HurtBox targetHurtBox)
        {
            if (targetHurtBox == null)
            {
                targetHurtBox = AcquireTarget(aimRay);
            }

            bool hasHurtbox = targetHurtBox && targetHurtBox.healthComponent && targetHurtBox.healthComponent.body && targetHurtBox.healthComponent.body.characterMotor;
            if (hasHurtbox && projectileSpeed > 0f)
            {
                CharacterBody targetBody = targetHurtBox.healthComponent.body;
                Vector3 targetPosition = targetHurtBox.transform.position;

                //Velocity shows up as 0 for clients due to not having authority over the CharacterMotor
                Vector3 targetVelocity = targetBody.characterMotor.velocity;
                if (!targetBody.hasAuthority)
                {
                    //Less accurate, but it works online.
                    targetVelocity = (targetBody.transform.position - targetBody.previousPosition) / Time.fixedDeltaTime;
                }

                if (targetVelocity.sqrMagnitude > 0f && !(targetBody && targetBody.hasCloakBuff))   //Dont bother predicting stationary targets
                {
                    //A very simplified way of estimating, won't be 100% accurate.
                    Vector3 currentDistance = targetPosition - aimRay.origin;
                    float timeToImpact = currentDistance.magnitude / projectileSpeed;
                    Vector3 futurePosition = targetPosition + targetVelocity * timeToImpact;

                    Ray newAimray = new()
                    {
                        origin = aimRay.origin,
                        direction = (futurePosition - aimRay.origin).normalized
                    };

                    float angleBetweenVectors = Vector3.Angle(aimRay.direction, newAimray.direction);
                    if (angleBetweenVectors <= AITweaks.basePredictionAngle)
                    {
                        return newAimray;
                    }
                }
            }

            return aimRay;
        }

        private static HurtBox AcquireTarget(Ray aimRay)
        {
            BullseyeSearch search = new()
            {
                teamMaskFilter = TeamMask.GetEnemyTeams(TeamIndex.Player),
                filterByLoS = true,
                searchOrigin = aimRay.origin,
                sortMode = BullseyeSearch.SortMode.Angle,
                maxDistanceFilter = 200f,
                maxAngleFilter = AITweaks.basePredictionAngle,
                searchDirection = aimRay.direction
            };
            search.RefreshCandidates();

            return search.GetResults().FirstOrDefault();
        }

        private static HurtBox GetMasterAITargetHurtbox(CharacterMaster cm)
        {
            if (cm && cm.aiComponents.Length > 0)
            {
                foreach (BaseAI ai in cm.aiComponents)
                {
                    if (ai.currentEnemy != null && ai.currentEnemy.bestHurtBox != null)
                    {
                        return ai.currentEnemy.bestHurtBox;
                    }
                }
            }
            return null;
        }
    }
}