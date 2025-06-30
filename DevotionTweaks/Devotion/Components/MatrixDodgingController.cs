using MiscFixes.Modules;
using Newtonsoft.Json.Utilities;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace LemurFusion.Devotion.Components
{
    public class MatrixDodgingController : MonoBehaviour
    {
        private int escapeSkillIndex, strafeSkillIndex;
        private float escapeDistance, safeDistance;

        private BaseAI ai;
        private CharacterMaster master;
        private CharacterBody body;
        private LineRenderer laserLineComponent;

        private void OnEnable()
        {
            ai = GetComponent<BaseAI>();
            master = GetComponent<CharacterMaster>();
            laserLineComponent = this.GetComponent<LineRenderer>() ?? this.gameObject.AddComponent<LineRenderer>();
            DisableLaser();

            escapeSkillIndex = ai.skillDrivers.IndexOf(s => s.customName == AITweaks.SKILL_ESCAPE_NAME);
            strafeSkillIndex = ai.skillDrivers.IndexOf(s => s.customName == AITweaks.SKILL_STRAFE_NAME);

            StartCoroutine(UpdateSkillDrivers());
        }

        private IEnumerator UpdateSkillDrivers()
        {
            while (master)
            {
                while (!body || !AITweaks.enableProjectileTracking.Value)
                {
                    yield return new WaitForSeconds(2);

                    if (!master)
                        break;

                    body = master.GetBody();
                }

                while (FindProjectiles(out var distance, out var shouldJump))
                {
                    AISkillDriver driver = distance < this.escapeDistance ? this.ai.skillDrivers[escapeSkillIndex] : this.ai.skillDrivers[strafeSkillIndex];

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
                        body.characterMotor.Jump(0.75f, 0.75f);
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
            this.escapeDistance = (AITweaks.detectionRadius.Value * 0.5f) * (AITweaks.detectionRadius.Value * 0.5f);
            this.safeDistance = AITweaks.detectionRadius.Value * AITweaks.detectionRadius.Value;

            distance = safeDistance;
            shouldJump = false;
            if (!body || !master)
                return false;

            GameObject target = null;
            Vector3 closestPoint = Vector3.zero;

            var position = body.footPosition;
            var enemyTeams = TeamMask.GetEnemyTeams(body.master.teamIndex);

            foreach (var projectile in InstanceTracker.GetInstancesList<ProjectileController>())
            {
                if (projectile && enemyTeams.HasTeam(projectile.teamFilter.teamIndex))
                {
                    if (projectile.gameObject.name.Contains("Sunder") && (projectile.transform.position - position).sqrMagnitude < this.safeDistance * 9f)
                        shouldJump = true;

                    if (projectile.TryGetComponent<HitBoxGroup>(out var hitboxGroup))
                    {
                        for (int i = 0; i < hitboxGroup.hitBoxes.Length; i++)
                        {
                            var estimatedPoint = Utils.ClosestPointOnTransform(hitboxGroup.hitBoxes[i].transform, position, out var other);
                            if (other < distance)
                            {
                                distance = other;
                                closestPoint = estimatedPoint;
                                target = projectile.gameObject;
                            }
                        }
                    }
                    else if (projectile.myColliders.Length > 0)
                    {
                        for (int i = 0; i < projectile.myColliders.Length; i++)
                        {
                            var estimatedPoint = Utils.ClosestPointOnCollider(projectile.myColliders[i], position, out var other);
                            if (other < distance)
                            {
                                distance = other;
                                closestPoint = estimatedPoint;
                                target = projectile.gameObject;
                            }
                        }
                    }
                    else if (this.TryGetComponent<ProjectileExplosion>(out var impact))
                    {
                        var estimatedPoint = Utils.ClosestPointOnBounds(projectile.transform.position, Vector3.one * impact.blastRadius, position, out var other);
                        if (other < distance)
                        {
                            distance = other;
                            closestPoint = estimatedPoint;
                            target = projectile.gameObject;
                        }
                    }
                    else
                    {
                        var estimatedPoint = Utils.ClosestPointOnTransform(projectile.transform, position, out var other);
                        if (other < distance)
                        {
                            distance = other;
                            closestPoint = estimatedPoint;
                            target = projectile.gameObject;
                        }
                    }
                }
            }

            return SetAITarget(target, closestPoint);
        }

        private bool SetAITarget(GameObject target, Vector3 closestPoint)
        {
            if (ai.customTarget.gameObject && ai.customTarget.gameObject != target && ai.customTarget.gameObject.TryGetComponent<LemHitboxGroupRevealer>(out var revealer))
                revealer.Reveal(false);

            if (target)
            {
                ai.customTarget._gameObject = target;
                ai.customTarget.lastKnownBullseyePosition = closestPoint;
                ai.customTarget.lastKnownBullseyePositionTime = Run.FixedTimeStamp.now;
                ai.customTarget.unset = false;

                EnableLaser();
            }
            else
            {
                ai.customTarget._gameObject = null;
                ai.customTarget.lastKnownBullseyePosition = null;
                ai.customTarget.lastKnownBullseyePositionTime = Run.FixedTimeStamp.negativeInfinity;
                ai.customTarget.unset = true;

                DisableLaser();
            }

            return ai.customTarget.gameObject;
        }
        private void EnableLaser()
        {
            if (AITweaks.visualizeProjectileTracking.Value)
            {
                ai.customTarget.gameObject.GetOrAddComponent<LemHitboxGroupRevealer>().Reveal(true);

                laserLineComponent.enabled = true;
                laserLineComponent.startWidth = 0.1f;
                laserLineComponent.endWidth = 0.1f;

                ai.customTarget.GetBullseyePosition(out var pos);

                laserLineComponent.SetPositions([body.footPosition, pos]);
            }
        }

        private void DisableLaser()
        {
            laserLineComponent.startWidth = 0f;
            laserLineComponent.endWidth = 0f;
            laserLineComponent.enabled = false;
        }
    }
}