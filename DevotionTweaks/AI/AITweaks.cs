using BepInEx.Configuration;
using EntityStates.AI.Walker;
using EntityStates.LemurianBruiserMonster;
using EntityStates.LemurianMonster;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
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
        public static ConfigEntry<bool> excludeSurvivorProjectiles;

        public static HashSet<int> projectileIds = [];
        public static HashSet<int> overlapIds = [];

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
                RoR2Application.onLoad += AITweaks.PostLoad;

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
                component.maxDistance = 10f;
                component.aimType = AISkillDriver.AimType.AtCurrentEnemy;
                component.moveTargetType = AISkillDriver.TargetType.Custom;
                component.movementType = AISkillDriver.MovementType.FleeMoveTarget;
                component.ignoreNodeGraph = true;
                component.shouldSprint = true;
                component.driverUpdateTimerOverride = 0.2f;

                var component2 = DevotionTweaks.masterPrefab.AddComponent<AISkillDriver>();
                component2.customName = SKILL_STRAFE_NAME;
                component2.skillSlot = SkillSlot.Primary;
                component2.maxDistance = 25f;
                component2.aimType = AISkillDriver.AimType.AtCurrentEnemy;
                component2.moveTargetType = AISkillDriver.TargetType.Custom;
                component2.movementType = AISkillDriver.MovementType.StrafeMovetarget;
                component2.ignoreNodeGraph = true;
                component2.shouldSprint = true;
                component2.driverUpdateTimerOverride = 1f;
                component2.moveInputScale = 0.8f;
                component2.activationRequiresAimTargetLoS = true;
                component2.activationRequiresAimConfirmation = true;

                DevotionTweaks.masterPrefab.AddComponent<MatrixDodgingController>();

                ILHooks();
            }
        }

        private static void PostLoad()
        {
            List<string> survivors = ["Captain", "Bandit", "Commando", "Croco", "Driver", "Engi", "Evis", "Heal",
               "Firework", "Hunk", "Huntress", "Loader", "Mage", "Paladin", "Railgunner", "SS2", "Toolbot", "Treebot", "VoidSurvivor", "Mauling",
            "Needle", "Scissor", "Scout"];
            foreach (var projectile in ProjectileCatalog.projectilePrefabs)
            {
                if (excludeSurvivorProjectiles.Value && survivors.Any(projectile.gameObject.name.Contains))
                    continue;

                if (projectile.TryGetComponent<ProjectileController>(out var controller))
                {
                    if (projectile.GetComponent<ProjectileFuse>())
                    {
                        projectileIds.Add(controller.catalogIndex);
                        LemurFusionPlugin.LogInfo($"Adding ProjectileFuse {projectile.gameObject.name}");
                    }
                    else if (projectile.GetComponent<ProjectileImpactExplosion>())
                    {
                        projectileIds.Add(controller.catalogIndex);
                        LemurFusionPlugin.LogInfo($"Adding ProjectileImpactExplosion {projectile.gameObject.name}");
                    }
                    else if (projectile.GetComponent<HitBoxGroup>())
                    {
                        projectileIds.Add(controller.catalogIndex);
                        overlapIds.Add(controller.catalogIndex);
                        LemurFusionPlugin.LogInfo($"Adding HitboxGroup {projectile.gameObject.name}");
                    }
                }
            }
        }

        private static void ILHooks()
        {
            IL.EntityStates.AI.Walker.Combat.UpdateAI += (il) =>
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
            };

            IL.EntityStates.LemurianMonster.FireFireball.OnEnter += (il) =>
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
                        if (AITweaks.AllowPrediction(self.characterBody))
                        {
                            HurtBox targetHurtbox = AITweaks.GetMasterAITargetHurtbox(self.characterBody.master);
                            Ray newAimRay = AITweaks.PredictAimrayPS(aimRay, FireFireball.projectilePrefab, targetHurtbox);
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
            };

            IL.EntityStates.LemurianBruiserMonster.FireMegaFireball.FixedUpdate += (il) =>
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
                        if (AITweaks.AllowPrediction(self.characterBody))
                        {
                            HurtBox targetHurtbox = AITweaks.GetMasterAITargetHurtbox(self.characterBody.master);

                            float projectileSpeed = FireMegaFireball.projectileSpeed;
                            if (projectileSpeed > 0f)
                            {
                                aimRay = AITweaks.PredictAimray(aimRay, projectileSpeed, targetHurtbox);
                            }
                            else
                            {
                                aimRay = AITweaks.PredictAimrayPS(aimRay, FireMegaFireball.projectilePrefab, targetHurtbox);
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