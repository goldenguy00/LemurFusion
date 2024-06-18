using HG;
using LemurFusion;
using LemurFusion.AI;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlliesAvoidImplosions
{
    public class GTFOHController : MonoBehaviour
    {
        private BaseAI ai;
        private CharacterMaster master;
        private int escapeSkillIndex;
        private int strafeSkillIndex;
        private float escapeDistance;
        private float safeDistance;

        private void Awake()
        {
            this.ai = base.gameObject.GetComponent<BaseAI>();
            this.master = base.gameObject.GetComponent<CharacterMaster>();
        }

        private void Start()
        {
            escapeSkillIndex = -1;
            strafeSkillIndex = -1;
            for (int i = 0; i < ai.skillDrivers.Length; i++)
            {
                if (ai.skillDrivers[i].customName == AITweaks.SKILL_ESCAPE_NAME)
                {
                    escapeSkillIndex = i;
                }
                else if (ai.skillDrivers[i].customName == AITweaks.SKILL_STRAFE_NAME)
                {
                    strafeSkillIndex = i;
                }
            }

            if (escapeSkillIndex != -1 && strafeSkillIndex != -1)
            {
                escapeDistance = ai.skillDrivers[escapeSkillIndex].maxDistanceSqr;
                safeDistance = ai.skillDrivers[strafeSkillIndex].maxDistanceSqr;
                base.StartCoroutine(nameof(UpdateSkillDrivers));
            }
        }

        private IEnumerator UpdateSkillDrivers()
        {
            LemurFusionPlugin._logger.LogInfo("Begin coroutine of running the fuck away");
            while (master)
            {
                var body = master.GetBody();
                while (!AITweaks.improveAI.Value || !body)
                {
                    yield return new WaitForSeconds(2);
                    body = master.GetBody();
                }

                ai.customTarget.Reset();
                var target = FindProjectiles(body.footPosition, out var distance);
                while (target)
                {
                    AISkillDriver driver = null;
                    if (distance < escapeDistance)
                        driver = ai.skillDrivers[escapeSkillIndex];
                    else if (distance < safeDistance)
                        driver = ai.skillDrivers[strafeSkillIndex];

                    if (driver != null)
                    {
                        if (ai.selectedSkilldriverName != driver.customName || target != ai.customTarget.gameObject)
                        {
                            ai.customTarget.gameObject = target;
                            ai.BeginSkillDriver(new BaseAI.SkillDriverEvaluation
                            {
                                target = ai.customTarget,
                                aimTarget = ai.customTarget,
                                dominantSkillDriver = driver,
                                separationSqrMagnitude = distance,
                            });
                        }
                        else
                        {
                            ai.skillDriverUpdateTimer = driver.driverUpdateTimerOverride;
                            ai.skillDriverEvaluation.separationSqrMagnitude = distance;
                            ai.customTarget.Update();
                        }
                    }
                    yield return new WaitForSeconds(0.1f);

                    if (!body) break;

                    target = FindProjectiles(body.footPosition, out distance);
                }

                yield return new WaitForSeconds(1f);
            }
            yield break;
        }

        private GameObject FindProjectiles(Vector3 position, out float distance)
        {
            GameObject target = null;
            distance = safeDistance;
            List<ProjectileController> instancesList = InstanceTracker.GetInstancesList<ProjectileController>();
            var count = instancesList.Count;
            for (int i = 0; i < count; i++)
            {
                ProjectileController projectileController = ListUtils.GetSafe(instancesList, i);
                if (projectileController  && projectileController.teamFilter.teamIndex != TeamIndex.Player && AITweaks.projectileIds.Contains(projectileController.catalogIndex))
                {
                    var other = (projectileController.transform.position - position).sqrMagnitude;
                    if (other < distance)
                    {
                        distance = other;
                        target = projectileController.gameObject;
                    }
                }
            }
            return target;
        }
    }
}