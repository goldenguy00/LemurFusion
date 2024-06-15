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
        private int skillDriverIndex;
        private SphereSearch sphereSearch;

        private void Awake()
        {
            this.ai = base.gameObject.GetComponent<BaseAI>();
            this.master = base.gameObject.GetComponent<CharacterMaster>();
            this.sphereSearch = new SphereSearch
            {
                mask = LayerIndex.entityPrecise.mask,
                queryTriggerInteraction = QueryTriggerInteraction.UseGlobal
            };
        }

        private void Start()
        {
            for (int i = 0; i < ai.skillDrivers.Length; i++)
            {
                if (ai.skillDrivers[i].customName == AITweaks.SKILL_DRIVER_NAME)
                {
                    skillDriverIndex = i;
                    break;
                }
            }
            base.StartCoroutine(nameof(UpdateSkillDrivers));
        }

        private IEnumerator UpdateSkillDrivers()
        {
            LemurFusionPlugin._logger.LogInfo("Begin coroutine of running the fuck away");
            while (master != null)
            {
                var body = master.GetBody();
                while (!body || !AITweaks.improveAI.Value)
                {
                    yield return new WaitForSeconds(1);
                    body = master.GetBody();
                }

                while (FindNearbyProjectiles(body.footPosition, body.radius, out var distance))
                {
                    LemurFusionPlugin._logger.LogInfo("Running the fuck away");
                    ai.BeginSkillDriver(new BaseAI.SkillDriverEvaluation
                    {
                        target = ai.customTarget,
                        aimTarget = ai.customTarget,
                        dominantSkillDriver = ai.skillDrivers[skillDriverIndex],
                        separationSqrMagnitude = distance,
                    });

                    yield return new WaitForSeconds(0.2f);
                }

                if (ai.customTarget.gameObject)
                {
                    LemurFusionPlugin._logger.LogInfo("i am now safe :)");
                    ai.customTarget.Reset();
                }

                yield return new WaitForSeconds(2);
            }
            LemurFusionPlugin._logger.LogInfo("End coroutine of running the fuck away");
            yield break;
        }

        private bool FindNearbyProjectiles(Vector3 footPosition, float bodyRadius, out float distance)
        {
            distance = float.PositiveInfinity;
            List<ProjectileController> instanceList = CollectionPool<ProjectileController, List<ProjectileController>>.RentCollection();

            this.sphereSearch.origin = footPosition;
            this.sphereSearch.radius = bodyRadius * 2f;
            this.sphereSearch.ClearCandidates().RefreshCandidates().FilterCandidatesByProjectileControllers().OrderCandidatesByDistance();
            this.sphereSearch.GetProjectileControllers(instanceList);

            foreach (var projectileController in instanceList)
            {
                if (projectileController.teamFilter.teamIndex != TeamIndex.Player && AITweaks.projectileIds.Contains(projectileController.catalogIndex))
                {
                    distance = (projectileController.transform.position - footPosition).sqrMagnitude;
                    ai.customTarget.gameObject = projectileController.gameObject;
                    break;
                }
            }

            CollectionPool<ProjectileController, List<ProjectileController>>.ReturnCollection(instanceList);
            return distance != float.PositiveInfinity;
        }
    }
}