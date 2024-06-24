using HG;
using LemurFusion.Devotion.Tweaks;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LemurFusion.Devotion
{
    public class MatrixDodgingController : MonoBehaviour
    {
        private int escapeSkillIndex;
        private int strafeSkillIndex;
        private float escapeDistance;
        private float safeDistance;
        private float jumpPower;
        private bool isMoon;

        private BaseAI ai;
        private CharacterMaster master;
        private CharacterBody body;
        private LineRenderer laserLineComponent;

        private void Start()
        {
            ai = gameObject.GetComponent<BaseAI>();
            master = gameObject.GetComponent<CharacterMaster>();

            isMoon = false;
            jumpPower = 1f;
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
                StartCoroutine(nameof(UpdateSkillDrivers));
            }
        }

        private IEnumerator UpdateSkillDrivers()
        {
            TryInitMoon();
            while (master)
            {
                while (!body || !AITweaks.enableProjectileTracking.Value)
                {
                    yield return new WaitForSeconds(2);

                    if (!master) break;
                    body = master.GetBody();
                    TryInitMoon();
                }

                while (FindProjectiles(out var distance, out var shouldJump))
                {
                    AimLaser();

                    AISkillDriver driver;
                    if (distance < escapeDistance)
                    {
                        driver = ai.skillDrivers[escapeSkillIndex];
                        driver.maxDistance = escapeDistance;
                    }
                    else
                    {
                        driver = ai.skillDrivers[strafeSkillIndex];
                        driver.maxDistance = safeDistance;
                        driver.minDistance = escapeDistance;
                    }

                    driver.driverUpdateTimerOverride = AITweaks.updateFrequency.Value * 1.5f;
                    ai.BeginSkillDriver(new BaseAI.SkillDriverEvaluation
                    {
                        target = ai.customTarget,
                        aimTarget = ai.currentEnemy,
                        dominantSkillDriver = driver,
                        separationSqrMagnitude = distance,
                    });

                    if (body.characterMotor.isGrounded && shouldJump)
                    {
                        body.characterMotor.airControl = 1f;
                        body.characterMotor.Jump(jumpPower * 0.5f, jumpPower);
                    }

                    yield return new WaitForSeconds(AITweaks.updateFrequency.Value);
                    yield return new WaitForFixedUpdate();
                }
                DisableLaser();

                yield return new WaitForSeconds(AITweaks.updateFrequency.Value);
            }
            yield break;
        }

        private bool FindProjectiles(out float distance, out bool shouldJump)
        {
            escapeDistance = Mathf.Pow(AITweaks.detectionRadius.Value * (isMoon ? 0.75f : 0.5f), 2f);
            safeDistance = Mathf.Pow(AITweaks.detectionRadius.Value * (isMoon ? 1.5f : 1f), 2f);
            distance = safeDistance;
            shouldJump = false;
            GameObject target = null;
            Vector3? closestPoint = null;

            if (body)
            {
                var instancesList = InstanceTracker.GetInstancesList<ProjectileController>();
                var position = body.footPosition;
                var count = instancesList.Count;
                for (int i = 0; i < count; i++)
                {
                    ProjectileController projectile = ListUtils.GetSafe(instancesList, i);
                    if (projectile && projectile.teamFilter.teamIndex != TeamIndex.Player)
                    {
                        float other;
                        var valid = AITweaks.projectileIds.Contains(projectile.catalogIndex);
                        var special = AITweaks.overlapIds.Contains(projectile.catalogIndex);
                        if (valid || special)
                        {
                            other = (projectile.transform.position - position).sqrMagnitude;
                            if (other < distance)
                            {
                                distance = other;
                                closestPoint = null;
                                target = projectile.gameObject;
                            }
                            if (special)
                            {
                                var hitBoxGroup = projectile.GetComponent<HitBoxGroup>();
                                if (hitBoxGroup && hitBoxGroup.hitBoxes != null)
                                {
                                    for (int j = 0; j < hitBoxGroup.hitBoxes.Length; j++)
                                    {
                                        var hitBox = hitBoxGroup.hitBoxes[j];
                                        if (hitBox && hitBox.transform)
                                        {
                                            var transform = hitBox.transform;
                                            var estimatedPoint = Utils.EstimateClosestPoint(transform.position, transform.lossyScale, transform.rotation, position);
                                            other = (estimatedPoint - position).sqrMagnitude;
                                            if (other < distance)
                                            {
                                                distance = other;
                                                closestPoint = estimatedPoint;
                                                target = hitBox.gameObject;
                                            }
                                        }
                                    }
                                }
                            }
                            if (projectile.gameObject.name.Contains("Sunder") && other < safeDistance * 4f)
                                shouldJump = true;
                        }
                    }
                }
            }

            ai.customTarget.Reset();
            if (target)
            {
                ai.customTarget.gameObject = target;
                ai.customTarget.lastKnownBullseyePosition = closestPoint;
            }
            return !ai.customTarget.unset;
        }

        private void TryInitMoon()
        {
            if (SceneManager.GetActiveScene().name == "moon2" && !isMoon)
            {
                jumpPower = 0.4f;
                isMoon = true;
            }
        }

        private void AimLaser()
        {
            if (AITweaks.visualizeProjectileTracking.Value && body)
            {
                if (!laserLineComponent)
                    laserLineComponent = body.gameObject.AddComponent<LineRenderer>();

                laserLineComponent.enabled = true;
                laserLineComponent.startWidth = 0.25f;
                laserLineComponent.endWidth = 0.25f;
                ai.customTarget.GetBullseyePosition(out var pos);
                laserLineComponent.SetPositions([body.footPosition, pos]);
            }
            else
            {
                DisableLaser();
            }
        }

        private void DisableLaser()
        {
            if (laserLineComponent)
            {
                laserLineComponent.startWidth = 0f;
                laserLineComponent.endWidth = 0f;
                laserLineComponent.enabled = false;
            }
        }
    }
}