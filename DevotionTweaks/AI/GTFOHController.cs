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
        private CharacterMaster master;

        private void Awake()
        {
            this.ai = base.gameObject.GetComponent<BaseAI>();
            this.master = base.gameObject.GetComponent<CharacterMaster>();
        }

        private void Start()
        {
            base.StartCoroutine(nameof(UpdateSkillDrivers));
        }

        private IEnumerator UpdateSkillDrivers()
        {
            while (master != null)
            {
                var body = master.GetBody();
                while (!body)
                {
                    yield return new WaitForSeconds(1);
                    body = master.GetBody();
                }

                var target = FindNearbyProjectiles(body.footPosition, body.radius, out var distance);
                if (target)
                {
                    ai.customTarget.gameObject = target;
                    ai.BeginSkillDriver(new BaseAI.SkillDriverEvaluation
                    {
                        target = ai.customTarget,
                        aimTarget = ai.customTarget,
                        dominantSkillDriver = ai.skillDrivers[ai.skillDrivers.Length - 1],
                        separationSqrMagnitude = distance,
                    });
                    yield return new WaitForSeconds(0.2f);
                }
                else
                {
                    yield return new WaitForSeconds(1);
                }
            }
            yield break;
        }

        private GameObject FindNearbyProjectiles(Vector3 footPosition, float bodyRadius, out float distance)
        {
            GameObject target = null;
            distance = float.PositiveInfinity;
            bodyRadius = Mathf.Max(bodyRadius, 5f);
            float searchRadius = bodyRadius * bodyRadius;
            List<ProjectileController> instancesList = InstanceTracker.GetInstancesList<ProjectileController>();

            foreach (var projectileController in instancesList)
            {
                if (projectileController && projectileController.transform)
                {
                    var projectileDistance = (projectileController.transform.position - footPosition).sqrMagnitude;

                    if (projectileDistance < searchRadius && projectileDistance < distance &&
                        AITweaks.projectileIds.Contains(projectileController.catalogIndex))
                    {
                        LemurFusionPlugin._logger.LogInfo("Potential danger " + projectileDistance);
                        target = projectileController.gameObject;
                        distance = projectileDistance;
                    }
                }
            }
            return target;
        }

    }
}