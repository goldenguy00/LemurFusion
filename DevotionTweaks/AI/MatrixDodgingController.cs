using HG;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace LemurFusion.AI
{
    public class MatrixDodgingController : MonoBehaviour
    {
        private int escapeSkillIndex;
        private int strafeSkillIndex;
        private float escapeDistance;
        private float safeDistance;
        private float distance;
        private float delayTime;
        private float jumpPower;

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
                if (SceneManager.GetActiveScene().name == "moon2")
                {
                    delayTime = 0.2f;
                    jumpPower = 0.6f;
                    this.ai.skillDrivers[strafeSkillIndex].maxDistance = 50f;
                }
                else
                {
                    delayTime = 0.5f;
                    jumpPower = 1f;
                    this.ai.skillDrivers[strafeSkillIndex].maxDistance = 30f;
                }

                this.ai.fullVision = true;
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
                    this.body = this.master.GetBody();
                }
                CreateLaser();

                while (FindProjectiles())
                {
                    AISkillDriver driver;
                    if (distance < escapeDistance) driver = this.ai.skillDrivers[escapeSkillIndex];
                    else driver = this.ai.skillDrivers[strafeSkillIndex];
                    this.ai.BeginSkillDriver(new BaseAI.SkillDriverEvaluation
                    {
                        target = ai.customTarget,
                        aimTarget = ai.currentEnemy,
                        dominantSkillDriver = driver,
                        separationSqrMagnitude = this.distance,
                    });

                    if (this.body.characterMotor.isGrounded && AITweaks.pain.Contains(this.ai.customTarget.gameObject.name))
                    {
                        this.body.characterMotor.airControl = 1f;
                        this.body.characterMotor.Jump(this.jumpPower * 0.5f, this.jumpPower);
                    }

                    AimLaser();

                    yield return new WaitForSeconds(0.1f);
                }
                DisableLaser();

                yield return new WaitForSeconds(delayTime);
            }
            yield break;
        }

        private bool FindProjectiles()
        {
            GameObject target = null;
            this.distance = this.safeDistance;

            if (this.body)
            {
                var instancesList = InstanceTracker.GetInstancesList<ProjectileController>();
                var position = this.body.footPosition;
                var count = instancesList.Count;

                for (int i = 0; i < count; i++)
                {
                    ProjectileController projectileController = ListUtils.GetSafe(instancesList, i);
                    if (projectileController && projectileController.teamFilter.teamIndex != TeamIndex.Player && AITweaks.projectileIds.Contains(projectileController.catalogIndex))
                    {
                        var other = (projectileController.transform.position - position).sqrMagnitude;
                        if (other < this.distance)
                        {
                            this.distance = other;
                            target = projectileController.gameObject;
                        }
                    }
                }
            }

            this.ai.customTarget.gameObject = target;
            return !this.ai.customTarget.unset;
        }
    }
}