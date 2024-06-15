using AlliesAvoidImplosions;
using BepInEx.Configuration;
using EntityStates.LemurianBruiserMonster;
using EntityStates.LemurianMonster;
using LemurFusion.Config;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace LemurFusion.AI
{
    public class AITweaks
    {
        public static AITweaks instance;

        public static ConfigEntry<bool> disableFallDamage;
        public static ConfigEntry<bool> immuneToVoidDeath;
        public static ConfigEntry<bool> improveAI;

        public static HashSet<int> projectileIds = [];

        public static float basePredictionAngle = 45f;

        public const string SKILL_DRIVER_NAME = "BackUpFromImplosion";

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
                component.customName = SKILL_DRIVER_NAME;
                component.skillSlot = SkillSlot.None;
                component.maxDistance = 25f;
                component.moveTargetType = AISkillDriver.TargetType.Custom;
                component.aimType = AISkillDriver.AimType.AtMoveTarget;
                component.movementType = AISkillDriver.MovementType.FleeMoveTarget;
                component.shouldSprint = true;
                component.ignoreNodeGraph = true;
                component.driverUpdateTimerOverride = 0.5f;

                DevotionTweaks.masterPrefab.AddComponent<GTFOHController>();

                EnablePrediction();
            }
        }

        public static void PostLoad()
        {
            string[] list = ["Acid", "DotZone", "DeathBomb"];
            foreach (var projectile in ProjectileCatalog.projectilePrefabProjectileControllerComponents)
            {
                var prefab = ProjectileCatalog.GetProjectilePrefab(projectile.catalogIndex);
                if (prefab && list.Any(prefab.name.Contains))
                {
                    projectileIds.Add(projectile.catalogIndex);
                    LemurFusionPlugin._logger.LogInfo($"Adding projectile by name {prefab.name}");
                }
                else if (projectile.TryGetComponent<ProjectileDamage>(out var damage) &&
                    ((damage.damageType & DamageType.VoidDeath) == DamageType.VoidDeath))
                {
                    projectileIds.Add(projectile.catalogIndex);
                    LemurFusionPlugin._logger.LogInfo($"Adding void death projectile {prefab.name}");
                }
                else if (projectile.GetComponent<ProjectileDamageTrail>() || projectile.GetComponent<ProjectileDotZone>() || projectile.GetComponent<ProjectileFuse>())
                {
                    projectileIds.Add(projectile.catalogIndex);
                    LemurFusionPlugin._logger.LogInfo($"Adding projectile by component {prefab.name}");
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
                    LemurFusionPlugin._logger.LogError("EntityStates.LemurianMonster.FireFireball.OnEnter IL Hook failed");
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
                    LemurFusionPlugin._logger.LogError("EntityStates.LemurianBruiserMonster.FireMegaFireball.FixedUpdate IL Hook failed");
                }
            };
        }

        private static bool AllowPrediction(CharacterBody body)
        {
            return body && body.master && body.master.name == DevotionTweaks.masterCloneName;
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
                LemurFusionPlugin._logger.LogError("Could not get speed of ProjectileSimple.");
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

                    //Vertical movenent isn't predicted well by this, so just use the target's current Y
                    Vector3 lateralVelocity = new(targetVelocity.x, 0f, targetVelocity.z);
                    Vector3 futurePosition = targetPosition + lateralVelocity * timeToImpact;

                    //Only attempt prediction if player is jumping upwards.
                    //Predicting downwards movement leads to groundshots.
                    if (targetBody.characterMotor && !targetBody.characterMotor.isGrounded && targetVelocity.y > 0f)
                    {
                        //point + vt + 0.5at^2
                        float futureY = targetPosition.y + targetVelocity.y * timeToImpact;
                        futureY += 0.5f * Physics.gravity.y * timeToImpact * timeToImpact;
                        futurePosition.y = futureY;
                    }

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