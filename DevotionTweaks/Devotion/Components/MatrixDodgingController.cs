using HG;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using System.Collections;
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
                    this.ai.customTarget.GetBullseyePosition(out var pos);
                    var driver = distance < this.escapeDistance ? this.ai.skillDrivers[escapeSkillIndex] : this.ai.skillDrivers[strafeSkillIndex];

                    if (driver.customName != this.ai.selectedSkilldriverName || this.ai.skillDriverUpdateTimer > 0.2f)
                    {
                        driver.driverUpdateTimerOverride = Mathf.Clamp(AITweaks.updateFrequency.Value * 1.5f, 0.3f, 1f);
                        ai.BeginSkillDriver(new BaseAI.SkillDriverEvaluation
                        {
                            target = ai.customTarget,
                            aimTarget = ai.currentEnemy,
                            dominantSkillDriver = driver,
                            separationSqrMagnitude = distance,
                        });
                    }
                    if (body.characterMotor.isGrounded && shouldJump)
                    {
                        body.characterMotor.airControl = 1f;
                        body.characterMotor.Jump(jumpPower * 0.5f, jumpPower);
                    }

                    yield return new WaitForSeconds(AITweaks.updateFrequency.Value);
                    yield return new WaitForFixedUpdate();
                }

                yield return new WaitForSeconds(AITweaks.updateFrequency.Value);
            }
            yield break;
        }

        private bool FindProjectiles(out float distance, out bool shouldJump)
        {
            this.escapeDistance = Mathf.Pow(AITweaks.detectionRadius.Value * (isMoon ? 0.75f : 0.5f), 2f);
            this.safeDistance = Mathf.Pow(AITweaks.detectionRadius.Value * (isMoon ? 1.5f : 1f), 2f);
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
                    if (projectile && projectile.teamFilter.teamIndex != body.master.teamIndex)
                    {
                        Vector3 estimatedPoint;
                        var transform = projectile.transform;
                        float other = (transform.position - position).sqrMagnitude;

                        if (other < safeDistance * 4f && projectile.gameObject.name.Contains("Sunder"))
                            shouldJump = true;

                        if (other < escapeDistance)
                        {
                            distance = other;
                            closestPoint = null;
                            target = projectile.gameObject;
                        }

                        if (projectile.TryGetComponent<HitBoxGroup>(out var hitBoxGroup) && hitBoxGroup.hitBoxes != null)
                        {
                            for (int j = 0; j < hitBoxGroup.hitBoxes.Length; j++)
                            {
                                var hitBox = hitBoxGroup.hitBoxes[j];
                                if (hitBox)
                                {
                                    transform = hitBox.transform;
                                    if (transform)
                                    {
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
                        }
                        else
                        {
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
    }
}