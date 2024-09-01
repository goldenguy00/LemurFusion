using System.Collections.Generic;
using System.Runtime.CompilerServices;
using LemurFusion.Config;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace LemurFusion
{
    public static class Utils
    {
        #region List Utils
        public static void AddItem(SortedList<ItemIndex, int> target, ItemDef itemDef, int count = 1)
        {
            if (!itemDef) return;
            AddItem(target, itemDef.itemIndex, count);
        }

        public static void AddItem(SortedList<ItemIndex, int> target, ItemIndex itemIndex, int count = 1)
        {
            if (itemIndex == ItemIndex.None) return;

            target ??= [];
            if (target.ContainsKey(itemIndex))
                target[itemIndex] += count;
            else
                target.Add(itemIndex, count);
        }

        public static void SetItem(SortedList<ItemIndex, int> target, ItemDef itemDef, int count = 1)
        {
            if (!itemDef) return;
            SetItem(target, itemDef.itemIndex, count);
        }

        public static void SetItem(SortedList<ItemIndex, int> target, ItemIndex itemIndex, int count = 1)
        {
            if (itemIndex == ItemIndex.None) return;

            target ??= [];
            if (count <= 0)
                target.Remove(itemIndex);
            else
                target[itemIndex] = count;
        }

        public static void RemoveItem(SortedList<ItemIndex, int> target, ItemDef itemDef, int count = 1)
        {
            if (!itemDef) return;
            RemoveItem(target, itemDef.itemIndex, count);
        }

        public static void RemoveItem(SortedList<ItemIndex, int> target, ItemIndex itemIndex, int count = 1)
        {
            if (itemIndex == ItemIndex.None || target == null) return;

            if (target.TryGetValue(itemIndex, out var heldCount))
            {
                var newVal = System.Math.Max(0, heldCount - count);

                if (newVal == 0)
                    target.Remove(itemIndex);
                else
                    target[itemIndex] = newVal;
            }
        }
        #endregion

        #region Stat Modifiers
        public static Vector3 GetScaleFactor(int configValue, int meldCount)
        {
            if (meldCount <= 1) return Vector3.one;

            return Vector3.one * ((meldCount - 1) * (configValue * 0.01f) * 0.5f);
        }

        public static float GetVanillaStatModifier(int configValue, int meldCount, int evolutionCount)
        {
            var vanillaMult = evolutionCount switch
            {
                0 or 2 => 10,
                1 => 20,
                _ => 17 + evolutionCount,
            };
            return (meldCount * (configValue * 0.01f)) + (vanillaMult * 0.1f);
        }

        public static float GetFusionStatMultiplier(int configValue, int meldCount, int evolutionCount)
        {
            var evolutionModifier = Mathf.Clamp(evolutionCount, 0, 4);
            var configModifier = PluginConfig.statMultEvo.Value * 0.01f;
            return meldCount * (configValue * 0.01f) + (evolutionModifier * configModifier * 0.1f);
        }

        public static float GetLevelModifier(int evolutionCount)
        {
            if (!Run.instance) return 0;

            var stageModifier = Mathf.Clamp(Run.instance.stageClearCount + 1, 1, 4);
            var evolutionModifier = Mathf.Clamp(evolutionCount, 0, 4);
            var configModifier = PluginConfig.statMultEvo.Value * 0.01f;
            return (stageModifier * configModifier) + (evolutionModifier * configModifier);
        }
        #endregion

        #region Vector Math
        public static Vector3 EstimateClosestPoint(Vector3 pos, Vector3 lossyScale, Quaternion rotation, Vector3 point)
        {
            lossyScale *= 0.5f;

            // take the two opposite corners, rotate then find the closest point on that line
            var p1 = rotation * new Vector3(pos.x + lossyScale.x, pos.y, pos.z + lossyScale.z);
            var p2 = rotation * new Vector3(pos.x - lossyScale.x, pos.y, pos.z - lossyScale.z);
            var p3 = rotation * new Vector3(pos.x + lossyScale.x, pos.y, pos.z - lossyScale.z);
            var p4 = rotation * new Vector3(pos.x - lossyScale.x, pos.y, pos.z + lossyScale.z);

            var vect = NearestPointOnLine(point, p1, p2);
            var otherVect = NearestPointOnLine(point, p3, p4);

            if ((vect - point).sqrMagnitude < (otherVect - point).sqrMagnitude)
                return vect;
            return otherVect;
        }

        public static Vector3 NearestPointOnLine(Vector3 point, Vector3 start, Vector3 end)
        {
            start.y = point.y;
            end.y = point.y;
            var line = (end - start);
            var len = line.magnitude;
            line.Normalize();

            var d = Vector3.Dot(point - start, line);
            d = Mathf.Clamp(d, 0f, len);
            return start + line * d;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDevoted(CharacterBody body) => body && IsDevoted(body.master);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDevoted(CharacterMaster master) => master && master.teamIndex == TeamIndex.Player && master.hasBody && master.GetComponent<BetterLemurController>() != null;

        private const float zero = 0.00001f;

        public static Ray PredictAimray(Ray aimRay, CharacterBody body, GameObject projectilePrefab)
        {
            if (!IsDevoted(body) || !projectilePrefab)
                return aimRay;

            var projectileSpeed = 0f;
            if (projectilePrefab.TryGetComponent<ProjectileSimple>(out var ps))
                projectileSpeed = ps.desiredForwardSpeed;

            if (projectilePrefab.TryGetComponent<ProjectileCharacterController>(out var pcc))
                projectileSpeed = Mathf.Max(projectileSpeed, pcc.velocity);

            var targetBody = GetAimTargetBody(body);
            if (projectileSpeed > 0f && targetBody)
            {
                //Velocity shows up as 0 for clients due to not having authority over the CharacterMotor
                //Less accurate, but it works online.
                Vector3 vT, aT, pT = targetBody.transform.position;
                if (targetBody.characterMotor && targetBody.characterMotor.hasEffectiveAuthority)
                {
                    vT = targetBody.characterMotor.velocity;
                    aT = GetAccel(targetBody.characterMotor, ref vT);
                }
                else
                {
                    vT = (pT - targetBody.previousPosition) / Time.fixedDeltaTime;
                    aT = Vector3.zero;
                }

                if (vT.sqrMagnitude > zero) //Dont bother predicting stationary targets
                {
                    return GetRay(aimRay, projectileSpeed, pT, vT, aT);
                }
            }

            return aimRay;
        }
        private static Vector3 GetAccel(CharacterMotor motor, ref Vector3 velocity)
        {
            float num = motor.acceleration;
            if (motor.isAirControlForced || !motor.isGrounded)
            {
                num *= (motor.disableAirControlUntilCollision ? 0f : motor.airControl);
            }

            Vector3 vector = motor.moveDirection;
            if (!motor.isFlying)
            {
                vector.y = 0f;
            }

            if (motor.body.isSprinting)
            {
                float magnitude = vector.magnitude;
                if (magnitude < 1f && magnitude > 0f)
                {
                    float num2 = 1f / vector.magnitude;
                    vector *= num2;
                }
            }

            Vector3 target = vector * motor.walkSpeed;
            if (!motor.isFlying)
            {
                target.y = velocity.y;
            }

            velocity = Vector3.MoveTowards(velocity, target, num * Time.fixedDeltaTime);
            if (motor.useGravity)
            {
                ref float y = ref velocity.y;
                y += Physics.gravity.y * Time.fixedDeltaTime;
                if (motor.isGrounded)
                {
                    y = Mathf.Max(y, 0f);
                }
            }
            return velocity;
        }
        //All in world space! Gets point you have to aim to
        //NOTE: this will break with infinite speed projectiles!
        //https://gamedev.stackexchange.com/questions/149327/projectile-aim-prediction-with-acceleration
        public static Ray GetRay(Ray aimRay, float sP, Vector3 pT, Vector3 vT, Vector3 aT)
        {
            //time to target guess
            var t = Vector3.Distance(aimRay.origin, pT) / sP;

            // target position relative to ray position
            pT -= aimRay.origin;

            var useAccel = aT.sqrMagnitude > zero;

            //quartic coefficients
            // a = t^4 * (aT·aT / 4.0)
            // b = t^3 * (aT·vT)
            // c = t^2 * (aT·pT + vT·vT - s^2)
            // d = t   * (2.0 * vT·pT)
            // e =       pT·pT
            var c = vT.sqrMagnitude - Pow2(sP);
            var d = 2f * Vector3.Dot(vT, pT);
            var e = pT.sqrMagnitude;

            if (useAccel)
            {
                var a = aT.sqrMagnitude * 0.25f;
                var b = Vector3.Dot(aT, vT);
                c += Vector3.Dot(aT, pT);

                //solve with newton
                t = SolveQuarticNewton(t, 6, a, b, c, d, e);
            }
            else
            {
                t = SolveQuadraticNewton(t, 6, c, d, e);
            }

            if (t > 0f)
            {
                //p(t) = pT + (vT * t) + ((aT/2.0) * t^2)
                var relativeDest = pT + (vT * t);
                if (useAccel)
                    relativeDest += 0.5f * aT * Pow2(t);

                return new Ray(aimRay.origin, relativeDest);
            }
            return aimRay;

        }

        private static float SolveQuarticNewton(float guess, int iterations, float a, float b, float c, float d, float e)
        {
            for (var i = 0; i < iterations; i++)
            {
                guess -= EvalQuartic(guess, a, b, c, d, e) / EvalQuarticDerivative(guess, a, b, c, d);
            }
            return guess;
        }

        private static float EvalQuartic(float t, float a, float b, float c, float d, float e)
        {
            return (a * Pow4(t)) + (b * Pow3(t)) + (c * Pow2(t)) + (d * t) + e;
        }

        private static float EvalQuarticDerivative(float t, float a, float b, float c, float d)
        {
            return (4f * a * Pow3(t)) + (3f * b * Pow2(t)) + (2f * c * t) + d;
        }

        private static float SolveQuadraticNewton(float guess, int iterations, float a, float b, float c)
        {
            for (var i = 0; i < iterations; i++)
            {
                guess -= EvalQuadratic(guess, a, b, c) / EvalQuadraticDerivative(guess, a, b);
            }
            return guess;
        }

        private static float EvalQuadratic(float t, float a, float b, float c)
        {
            return (a * Pow2(t)) + (b * t) + c;
        }

        private static float EvalQuadraticDerivative(float t, float a, float b)
        {
            return (2f * a * t) + b;
        }

        private static float Pow2(float n) => n * n;
        private static float Pow3(float n) => n * n * n;
        private static float Pow4(float n) => n * n * n * n;

        private static CharacterBody GetAimTargetBody(CharacterBody body)
        {
            var aiComponents = body.master.aiComponents;
            for (var i = 0; i < aiComponents.Length; i++)
            {
                var ai = aiComponents[i];
                if (ai && ai.hasAimTarget)
                {
                    var aimTarget = ai.skillDriverEvaluation.aimTarget;
                    if (aimTarget.characterBody && aimTarget.healthComponent && aimTarget.healthComponent.alive)
                    {
                        return aimTarget.characterBody;
                    }
                }
            }
            return null;
        }
        #endregion
    }
}
