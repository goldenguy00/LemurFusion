using HG;
using Newtonsoft.Json.Utilities;
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
        private float jumpPower = 1f;
        private bool isMoon;

        private BaseAI ai;
        private CharacterMaster master;
        private CharacterBody body;

        private void Start()
        {
            ai = gameObject.GetComponent<BaseAI>();
            master = gameObject.GetComponent<CharacterMaster>();

            escapeSkillIndex = ai.skillDrivers.IndexOf(s => s.customName == AITweaks.SKILL_ESCAPE_NAME);
            strafeSkillIndex = ai.skillDrivers.IndexOf(s => s.customName == AITweaks.SKILL_STRAFE_NAME);

            if (escapeSkillIndex != -1 && strafeSkillIndex != -1)
            {
                StartCoroutine(UpdateSkillDrivers());
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

                    if (!master)
                        break;

                    body = master.GetBody();
                    TryInitMoon();
                }

                while (FindProjectiles(out var distance, out var shouldJump))
                {
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
                        body.characterMotor.Jump(jumpPower, jumpPower);
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
            distance = safeDistance;
            shouldJump = false;
            if (!body || !master)
                return false;

            this.escapeDistance = Mathf.Pow(AITweaks.detectionRadius.Value * (isMoon ? 1f : 0.5f), 2f);
            this.safeDistance = Mathf.Pow(AITweaks.detectionRadius.Value * (isMoon ? 2f : 1f), 2f);

            GameObject target = null;
            Vector3? closestPoint = null;

            var position = body.footPosition;
            var enemyTeams = TeamMask.GetEnemyTeams(body.master.teamIndex);

            foreach (var projectile in InstanceTracker.GetInstancesList<ProjectileController>())
            {
                if (projectile && enemyTeams.HasTeam(projectile.teamFilter.teamIndex))
                {
                    var transform = projectile.transform;
                    if (projectile.gameObject.name.Contains("Sunder") && (transform.position - position).sqrMagnitude < this.safeDistance * 10f)
                    {
                        var other = 
                        shouldJump = true;
                    }

                    if (projectile.TryGetComponent<HitBoxGroup>(out var hitBoxGroup) && hitBoxGroup.hitBoxes != null)
                    {
                        for (var j = 0; j < hitBoxGroup.hitBoxes.Length; j++)
                        {
                            var hitBox = hitBoxGroup.hitBoxes[j];
                            if (hitBox)
                            {
                                transform = hitBox.transform;
                                if (transform)
                                {
                                    CompareAndUpdateTargets(transform, position, ref distance, ref closestPoint, ref target);
                                }
                            }
                        }
                    }
                    else
                    {
                        CompareAndUpdateTargets(transform, position, ref distance, ref closestPoint, ref target);
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

        private void CompareAndUpdateTargets(Transform transform, Vector3 position, ref float distance, ref Vector3? closestPoint, ref GameObject target)
        {
            var estimatedPoint = Utils.EstimateClosestPoint(transform.position, transform.lossyScale, transform.rotation, position);
            var other = (estimatedPoint - position).sqrMagnitude;
            if (other < distance)
            {
                distance = other;
                closestPoint = estimatedPoint;
                target = transform.gameObject;
            }
        }

        private void TryInitMoon()
        {
            if (SceneManager.GetActiveScene().name == "moon2" && !isMoon)
            {
                jumpPower = 0.75f;
                isMoon = true;
            }
        }
    }
}