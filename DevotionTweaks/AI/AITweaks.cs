﻿using AlliesAvoidImplosions;
using BepInEx.Configuration;
using LemurFusion.Config;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using System.Collections.Generic;
using System.Linq;
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
        public static ConfigEntry<float> evasionDistance;
        public static ConfigEntry<bool> improveAI;

        public static HashSet<int> projectileIds { get; private set; } = [];

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
                CharacterMaster.onStartGlobal += CharacterMaster_onStartGlobal;

                foreach (var driver in DevotionTweaks.masterPrefab.GetComponentsInChildren<AISkillDriver>())
                {
                    switch (driver.customName)
                    {
                        case "ReturnToLeaderDefault":
                            driver.driverUpdateTimerOverride = 0.2f;
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
                component.maxDistance = 10f;
                component.moveTargetType = AISkillDriver.TargetType.Custom;
                component.aimType = AISkillDriver.AimType.AtMoveTarget;
                component.movementType = AISkillDriver.MovementType.FleeMoveTarget;
                component.resetCurrentEnemyOnNextDriverSelection = true;
                component.ignoreNodeGraph = true;
                component.driverUpdateTimerOverride = 0.5f;

                DevotionTweaks.masterPrefab.AddComponent<GTFOHController>();
            }
        }

        public void PostLoad()
        {
            string[] list = ["BeetleQueenAcid", "DotZone", "DeathBomb"];
            foreach (var projectile in ProjectileCatalog.projectilePrefabProjectileControllerComponents)
            {
                var prefab = ProjectileCatalog.GetProjectilePrefab(projectile.catalogIndex);
                if (prefab && list.Any(prefab.name.Contains))
                {
                    projectileIds.Add(projectile.catalogIndex);
                }
                else if (projectile.TryGetComponent<ProjectileDamage>(out var damage) &&
                    ((damage.damageType & DamageType.VoidDeath) == DamageType.VoidDeath))
                {
                    projectileIds.Add(projectile.catalogIndex);
                }
                else if (projectile.TryGetComponent<ProjectileDamageTrail>(out var trail))
                {
                    projectileIds.Add(projectile.catalogIndex);
                }
            }
        }

        private void CharacterMaster_onStartGlobal(CharacterMaster self)
        {
            if (self.name == DevotionTweaks.masterCloneName)
            {
                AISkillDriver[] arr = self.gameObject.GetComponent<BaseAI>().skillDrivers;
                if (arr.Length > 0 && arr[0].customName != SKILL_DRIVER_NAME)
                {
                    var list = new AISkillDriver[arr.Length];
                    for (int i = 0; i < arr.Length - 1; i++)
                    {
                        if (i == arr.Length - 1)
                            list[0] = arr[i];
                        else
                            list[i + 1] = arr[i];
                    }
                    self.gameObject.GetComponent<BaseAI>().skillDrivers = list;
                }
            }
        }
    }
}