using HG;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Facepunch.Steamworks.LobbyList.Filter;
using static Facepunch.Steamworks.Workshop;
using UnityEngine.UIElements;

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
                DisableLaser();

                yield return new WaitForSeconds(AITweaks.updateFrequency.Value);
            }
            yield break;
        }

        private bool FindProjectiles(out float distance, out bool shouldJump)
        {
            this.escapeDistance = Mathf.Pow(AITweaks.detectionRadius.Value * (isMoon ? 1f : 0.5f), 2f);
            this.safeDistance = Mathf.Pow(AITweaks.detectionRadius.Value * (isMoon ? 2f : 1f), 2f);
            distance = safeDistance;
            shouldJump = false;
            GameObject target = null;
            Vector3? closestPoint = null;

            if (body)
            {
                var position = body.footPosition;
                var enemyTeams = TeamMask.GetEnemyTeams(body.master.teamIndex);

                var instancesList = InstanceTracker.GetInstancesList<ProjectileController>();
                var count = instancesList.Count;
                for (int i = 0; i < count; i++)
                {
                    ProjectileController projectile = ListUtils.GetSafe(instancesList, i);
                    if (projectile && enemyTeams.HasTeam(projectile.teamFilter.teamIndex))
                    {
                        var transform = projectile.transform;
                        float other = (transform.position - position).sqrMagnitude;

                        if (other < safeDistance * 4f && projectile.gameObject.name.Contains("Sunder"))
                            shouldJump = true;

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