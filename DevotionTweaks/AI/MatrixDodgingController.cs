using HG;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LemurFusion.AI
{
    public class MatrixDodgingController : MonoBehaviour
    {
        private int escapeSkillIndex;
        private int strafeSkillIndex;
        private float escapeDistance;
        private float safeDistance;
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
                this.ai.fullVision = true;
                this.escapeDistance = this.ai.skillDrivers[escapeSkillIndex].maxDistanceSqr;
                this.safeDistance = this.ai.skillDrivers[strafeSkillIndex].maxDistanceSqr;
                base.StartCoroutine(nameof(UpdateSkillDrivers));
            }
        }

        private void OnEnabled()
        {
            if (SceneManager.GetActiveScene().name == "moon2")
            {
                delayTime = 0.2f;
                jumpPower = 0.4f;
                this.ai.skillDrivers[strafeSkillIndex].maxDistance = 40f;
            }
            else
            {
                delayTime = 0.5f;
                jumpPower = 1f;
                this.ai.skillDrivers[strafeSkillIndex].maxDistance = 25f;
            }

            this.safeDistance = this.ai.skillDrivers[strafeSkillIndex].maxDistanceSqr;
            CreateLaser();
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

                yield return new WaitForFixedUpdate();
                while (FindProjectiles(out var distance))
                {
                    AISkillDriver driver;
                    if (distance < this.escapeDistance)
                        driver = this.ai.skillDrivers[escapeSkillIndex];
                    else 
                        driver = this.ai.skillDrivers[strafeSkillIndex];

                    this.ai.BeginSkillDriver(new BaseAI.SkillDriverEvaluation
                    {
                        target = ai.customTarget,
                        aimTarget = ai.currentEnemy,
                        dominantSkillDriver = driver,
                        separationSqrMagnitude = distance,
                    });

                    if (this.body.characterMotor.isGrounded && this.ai.customTarget.gameObject &&
                        this.ai.customTarget.gameObject.name.Contains("Sunder"))
                    {
                        this.body.characterMotor.airControl = 1f;
                        this.body.characterMotor.Jump(this.jumpPower * 0.5f, this.jumpPower);
                    }

                    AimLaser();

                    yield return new WaitForSeconds(0.2f);
                    yield return new WaitForFixedUpdate();
                }
                DisableLaser();

                yield return new WaitForSeconds(delayTime);
            }
            yield break;
        }

        private bool FindProjectiles(out float distance)
        {
            this.ai.customTarget.Reset();
            distance = this.safeDistance;

            GameObject target = null;
            Vector3? closestPoint = null;

            if (this.body)
            {
                var instancesList = InstanceTracker.GetInstancesList<ProjectileController>();
                var position = this.body.footPosition;
                var count = instancesList.Count;

                for (int i = 0; i < count; i++)
                {
                    ProjectileController projectile = ListUtils.GetSafe(instancesList, i);
                    if (projectile && projectile.teamFilter.teamIndex != TeamIndex.Player && AITweaks.projectileIds.Contains(projectile.catalogIndex))
                    {
                        if (AITweaks.overlapIds.Contains(projectile.catalogIndex))
                        {
                            var hitBoxGroup = projectile.GetComponent<HitBoxGroup>();
                            if (hitBoxGroup && hitBoxGroup.hitBoxes.Length > 0)
                            {
                                for (int j = 0; j < hitBoxGroup.hitBoxes.Length; j++)
                                {
                                    var hitBox = hitBoxGroup.hitBoxes[j];
                                    if (hitBox)
                                    {
                                        var estimatedPoint = Utils.EstimateClosestPoint(hitBox.transform, position);
                                        var other = (estimatedPoint - position).sqrMagnitude;
                                        if (other < distance)
                                        {
                                            distance = other;
                                            closestPoint = estimatedPoint;
                                            target ??= projectile.gameObject;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            var other = (projectile.transform.position - position).sqrMagnitude;
                            if (other < distance)
                            {
                                distance = other;
                                closestPoint = null;
                                target = projectile.gameObject;
                            }
                        }
                    }
                }
            }

            this.ai.customTarget.gameObject = target;
            this.ai.customTarget.lastKnownBullseyePosition = closestPoint;

            return !this.ai.customTarget.unset;
        }

        private void CreateLaser()
        {
            if (this.body && !this.laserLineComponent && AITweaks.visualizeProjectileTracking.Value)
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
                this.ai.customTarget.GetBullseyePosition(out var pos);
                this.laserLineComponent.SetPositions([body.footPosition, pos]);
            }
        }
    }
}