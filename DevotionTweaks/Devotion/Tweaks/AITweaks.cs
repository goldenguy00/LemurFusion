using BepInEx.Configuration;
using EntityStates.AI.Walker;
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

namespace LemurFusion.Devotion.Tweaks
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
        public static ConfigEntry<bool> excludeSurvivorProjectiles;
        public static ConfigEntry<float> updateFrequency;
        public static ConfigEntry<int> detectionRadius;

        public static HashSet<int> projectileIds = [];
        public static HashSet<int> overlapIds = [];

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

                IL.EntityStates.AI.Walker.Combat.UpdateAI += Combat_UpdateAI;
                IL.EntityStates.LemurianMonster.FireFireball.OnEnter += FireFireball_OnEnter;
                IL.EntityStates.LemurianBruiserMonster.FireMegaFireball.FixedUpdate += FireMegaFireball_FixedUpdate;

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
                        case "DevotedSecondarySkill":
                            driver.minUserHealthFraction = 0.5f;
                            driver.noRepeat = true;
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

        private void PostLoad()
        {
            // no i wont make this configurable fuck you its good enough
            List<string> survivors = ["Captain", "Bandit", "Commando", "Croco", "Driver", "Engi", "Evis", "Heal",
               "Firework", "Hunk", "Huntress", "Loader", "Mage", "Paladin", "Railgunner", "SS2", "Toolbot", "Treebot",
                "VoidSurvivor", "Mauling", "Needle", "Scissor", "Scout"];

            foreach (var projectile in ProjectileCatalog.projectilePrefabs)
            {
                if (excludeSurvivorProjectiles.Value && survivors.Any(projectile.gameObject.name.Contains))
                    continue;

                if (projectile.TryGetComponent<ProjectileController>(out var controller))
                {
                    if (projectile.GetComponentInChildren<HitBoxGroup>(true))
                    {
                        overlapIds.Add(controller.catalogIndex);
                        LemurFusionPlugin.LogInfo($"Adding HitboxGroup {projectile.gameObject.name}");
                    }
                    else if (projectile.GetComponent<ProjectileFuse>())
                    {
                        projectileIds.Add(controller.catalogIndex);
                        LemurFusionPlugin.LogInfo($"Adding ProjectileFuse {projectile.gameObject.name}");
                    }
                    else if (projectile.GetComponent<ProjectileImpactExplosion>())
                    {
                        projectileIds.Add(controller.catalogIndex);
                        LemurFusionPlugin.LogInfo($"Adding ProjectileImpactExplosion {projectile.gameObject.name}");
                    }
                    else
                    {
                        projectileIds.Add(controller.catalogIndex);
                        LemurFusionPlugin.LogInfo($"Adding everything else {projectile.gameObject.name}");
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