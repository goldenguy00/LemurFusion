using HG;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LemurFusion.Devotion.Components
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
        private Stopwatch sw;

        private void Start()
        {
            ai = gameObject.GetComponent<BaseAI>();
            master = gameObject.GetComponent<CharacterMaster>();
            sw = new Stopwatch();

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
            sw.Restart();
            escapeDistance = Mathf.Pow(AITweaks.detectionRadius.Value * (isMoon ? 0.75f : 0.5f), 2f);
            safeDistance = Mathf.Pow(AITweaks.detectionRadius.Value * (isMoon ? 1.5f : 1f), 2f);
            distance = safeDistance;
            shouldJump = false;
            GameObject target = null;
            Vector3? closestPoint = null;
            int count = 0;
            int q = 0;
            int w = 0;
            int e = 0;

            if (body)
            {
                var instancesList = InstanceTracker.GetInstancesList<ProjectileController>();
                var position = body.footPosition;
                count = instancesList.Count;
                for (int i = 0; i < count; i++)
                {
                    ProjectileController projectile = ListUtils.GetSafe(instancesList, i);
                    if (projectile && projectile.teamFilter.teamIndex != body.master.teamIndex)
                    {
                        Vector3 estimatedPoint;
                        var transform = projectile.transform;
                        var scale = transform.lossyScale.sqrMagnitude;
                        float other = (transform.position - position).sqrMagnitude;

                        if (projectile.gameObject.name.Contains("Sunder") && other < safeDistance * 4f)
                            shouldJump = true;

                        if (scale < Mathf.Pow(body.radius, 2f))
                        {
                            q++;
                            if (other < distance)
                            {
                                distance = other;
                                closestPoint = null;
                                target = projectile.gameObject;
                            }
                        }
                        else// if (other - scale < this.safeDistance * 4f)
                        {
                            var hitBoxGroup = projectile.GetComponent<HitBoxGroup>();
                            if (hitBoxGroup && hitBoxGroup.hitBoxes != null)
                            {
                                w++;
                                for (int j = 0; j < hitBoxGroup.hitBoxes.Length; j++)
                                {
                                    var hitBox = hitBoxGroup.hitBoxes[j];
                                    if (hitBox && hitBox.transform)
                                    {
                                        transform = hitBox.transform;
                                        estimatedPoint = Utils.EstimateClosestPoint(transform.position, transform.lossyScale, transform.rotation, position);
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
                            else
                            {
                                e++;
                                estimatedPoint = Utils.EstimateClosestPoint(transform.position, transform.lossyScale, transform.rotation, position);
                                other = (estimatedPoint - position).sqrMagnitude;
                                if (other < distance)
                                {
                                    distance = other;
                                    closestPoint = estimatedPoint;
                                    target = projectile.gameObject;
                                }
                            }
                        }
                    }
                }
            }

            ai.customTarget.Reset();

            sw.Stop();
            if (target)
            {
                ai.customTarget.gameObject = target;
                ai.customTarget.lastKnownBullseyePosition = closestPoint;
                LemurFusionPlugin.LogWarning($"Timer {sw.ElapsedMilliseconds}ms -> {sw.ElapsedTicks}");
                LemurFusionPlugin.LogWarning($"ListSize {count}:\tSimple: {q}\tHitbox: {w}\tAdv: {e}");
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