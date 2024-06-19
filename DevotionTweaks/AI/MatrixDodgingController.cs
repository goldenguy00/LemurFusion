using HG;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LemurFusion.AI
{
    public class MatrixDodgingController : MonoBehaviour
    {
        private int escapeSkillIndex;
        private int strafeSkillIndex;
        private float escapeDistance;
        private float safeDistance;

        private BaseAI ai;
        private CharacterMaster master;
        private CharacterBody body;
        private LineRenderer laserLineComponent;

        private void Awake()
        {
            this.ai = base.gameObject.GetComponent<BaseAI>();
            this.master = base.gameObject.GetComponent<CharacterMaster>();
        }

        private void Start()
        {
            this.escapeSkillIndex = -1;
            this.strafeSkillIndex = -1;
            for (int i = 0; i < this.ai.skillDrivers.Length; i++)
            {
                if (this.ai.skillDrivers[i].customName == AITweaks.SKILL_ESCAPE_NAME)
                {
                    this.escapeSkillIndex = i;
                }
                else if (this.ai.skillDrivers[i].customName == AITweaks.SKILL_STRAFE_NAME)
                {
                    this.strafeSkillIndex = i;
                }
            }

            if (this.escapeSkillIndex != -1 && this.strafeSkillIndex != -1)
            {
                this.escapeDistance = this.ai.skillDrivers[escapeSkillIndex].maxDistanceSqr;
                this.safeDistance = this.ai.skillDrivers[strafeSkillIndex].maxDistanceSqr;
                base.StartCoroutine(nameof(UpdateSkillDrivers));
            }
        }

        private void CreateLaser()
        {
            if (!this.laserLineComponent && AITweaks.visualizeProjectileTracking.Value)
            {
                this.laserLineComponent = this.body.gameObject.AddComponent<LineRenderer>();
                this.laserLineComponent.startWidth = 0f;
                this.laserLineComponent.endWidth = 0f;
                this.laserLineComponent.enabled = false;
            }
        }

        private void DisableLaser()
        {
            if (this.laserLineComponent)
            {
                this.laserLineComponent.startWidth = 0f;
                this.laserLineComponent.endWidth = 0f;
                this.laserLineComponent.enabled = false;
            }
        }

        private void AimLaser()
        {
            if (this.laserLineComponent && this.ai.customTarget.gameObject && AITweaks.visualizeProjectileTracking.Value)
            {
                this.laserLineComponent.enabled = true;
                this.laserLineComponent.startWidth = 0.25f;
                this.laserLineComponent.endWidth = 0.25f;
                this.laserLineComponent.SetPositions([body.footPosition, this.ai.customTarget.gameObject.transform.position]);
            }
        }

        private IEnumerator UpdateSkillDrivers()
        {
            while (this.master)
            {
                this.body = this.master.GetBody();
                while (!AITweaks.enableProjectileTracking.Value || !this.body)
                {
                    yield return new WaitForSeconds(2);
                    this.body = master.GetBody();
                }
                CreateLaser();

                this.ai.customTarget.Reset();
                var target = FindProjectiles(this.body.footPosition, out var distance);
                while (target)
                {
                    AISkillDriver driver;
                    if (distance < escapeDistance) driver = this.ai.skillDrivers[escapeSkillIndex];
                    else driver = this.ai.skillDrivers[strafeSkillIndex];

                    this.ai.customTarget.gameObject = target;
                    if (driver.customName == AITweaks.SKILL_ESCAPE_NAME || driver.customName != ai.selectedSkilldriverName)
                    {
                        this.ai.BeginSkillDriver(new BaseAI.SkillDriverEvaluation
                        {
                            target = ai.customTarget,
                            aimTarget = ai.customTarget,
                            dominantSkillDriver = driver,
                            separationSqrMagnitude = distance,
                        });
                    }
                    AimLaser();

                    yield return new WaitForSeconds(0.1f);

                    if (!this.body) break;

                    target = FindProjectiles(this.body.footPosition, out distance);
                }
                DisableLaser();

                yield return new WaitForSeconds(0.2f);
            }
            yield break;
        }

        private GameObject FindProjectiles(Vector3 position, out float distance)
        {
            GameObject target = null;
            distance = this.safeDistance;
            List<ProjectileController> instancesList = InstanceTracker.GetInstancesList<ProjectileController>();
            var count = instancesList.Count;
            for (int i = 0; i < count; i++)
            {
                ProjectileController projectileController = ListUtils.GetSafe(instancesList, i);
                if (projectileController && projectileController.teamFilter.teamIndex != TeamIndex.Player && AITweaks.projectileIds.Contains(projectileController.catalogIndex))
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