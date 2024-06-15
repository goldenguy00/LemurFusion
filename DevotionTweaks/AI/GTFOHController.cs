using HG;
using LemurFusion;
using LemurFusion.AI;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AlliesAvoidImplosions
{
    public class GTFOHController : MonoBehaviour
    {
        private BaseAI ai;
        private TeamIndex teamIndex;
        private float searchRadius;

        private void Awake()
        {
            this.ai = base.GetComponent<BaseAI>();
            teamIndex = this.ai.body.teamComponent.teamIndex;
            searchRadius = this.ai.body.radius * this.ai.body.radius;
            LemurFusionPlugin._logger.LogInfo("awake with body radius " + this.ai.body.radius);
        }

        private void Start()
        {
            base.StartCoroutine(nameof(UpdateSkillDrivers));
        }

        private IEnumerator UpdateSkillDrivers()
        {
            while (ai.master != null)
            {
                while (!ai.body)
                {
                    yield return new WaitForSeconds(1);
                }

                var target = FindNearbyProjectiles(out var distance);
                if (target)
                {
                    ai.customTarget.gameObject = target;
                    ai.BeginSkillDriver(new BaseAI.SkillDriverEvaluation
                    {
                        target = ai.customTarget,
                        aimTarget = ai.customTarget,
                        dominantSkillDriver = ai.skillDrivers[0],
                        separationSqrMagnitude = distance,
                    });

                    yield return new WaitForSeconds(0.2f);
                }
                else
                {
                    ai.customTarget.Reset();
                    yield return new WaitForSeconds(1);
                }
            }
            yield break;
        }

        private GameObject FindNearbyProjectiles(out float distance)
        {
            GameObject target = null;
            Vector3 vector = this.ai.body.footPosition;
            distance = float.PositiveInfinity;
            List<ProjectileController> instancesList = InstanceTracker.GetInstancesList<ProjectileController>();

            foreach (var projectileController in instancesList)
            {
                var projectileDistance = (projectileController.transform.position - vector).sqrMagnitude;

                if (projectileController.teamFilter.teamIndex != teamIndex && 
                    projectileDistance < searchRadius && projectileDistance < distance &&
                    AITweaks.projectileIds.Contains(projectileController.catalogIndex))
                {
                    LemurFusionPlugin._logger.LogInfo("Potential danger " + projectileDistance);
                    target = projectileController.gameObject;
                    distance = projectileDistance;
                }
            }
            return target;
        }

    }
}