﻿using LemurFusion.Config;
using LemurFusion.Devotion;
using LemurFusion;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;

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

            Vector3 vect = NearestPointOnLine(point, p1, p2);
            Vector3 otherVect = NearestPointOnLine(point, p3, p4);

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

        public static bool IsDevoted(CharacterBody body) => body && IsDevoted(body.master);
        public static bool IsDevoted(CharacterMaster master) => master && master.gameObject.name == DevotionTweaks.clonedMasterName && master.teamIndex == TeamIndex.Player;

        public static Ray PredictAimray(CharacterBody body, Ray aimRay, GameObject projectilePrefab, float projectileSpeed = 0)
        {
            if (projectileSpeed == 0 && projectilePrefab && projectilePrefab.TryGetComponent<ProjectileSimple>(out var ps))
            {
                projectileSpeed = ps.desiredForwardSpeed;
            }

            if (projectileSpeed > 0f && GetTargetHurtbox(body, aimRay, out var targetBody, out var targetPosition))
            {
                //Velocity shows up as 0 for clients due to not having authority over the CharacterMotor
                //Less accurate, but it works online.
                Vector3 targetVelocity;
                if (!targetBody.hasAuthority)
                    targetVelocity = (targetBody.transform.position - targetBody.previousPosition) / Time.fixedDeltaTime;
                else
                    targetVelocity = targetBody.characterMotor.velocity;

                if (targetVelocity.sqrMagnitude > 0f) //Dont bother predicting stationary targets
                {
                    return GetRay(aimRay, projectileSpeed, targetPosition, targetVelocity);
                }
            }

            return aimRay;
        }

        // i hate math dont loook :(
        private static Ray GetRay(Ray aimRay, float v, Vector3 y, Vector3 dy)
        {
            /*
                origin x
                speed of the projectile v

                target's initial position y
                velocity of the target dy

                find unit vector direction d
                for some time to impact t

                and
                yx.x = y.x - x.x
                yx.y = y.y - x.y
                yx.z = y.z - x.z

                yx = y - x

                equations to solve:
                x.x + v*t*d.x = y.x + dy.x*t
                x.y + v*t*d.y = y.y + dy.y*t
                x.z + v*t*d.z = y.z + dy.z*t
                x   + v*t*d   = y   + dy  *t

                x + v*t*d = y + dy*t
                    v*t*d = y + dy*t - x
                    v*t*d = (y - x) + dy*t
                    dv*t  = yx + dy*t

                v*t*d.x = dy.x*t + (y.x-x.x)
                v*t*d.y = dy.y*t + (y.y-x.y)
                v*t*d.z = dy.z*t + (y.z-x.z)

                Noting that d is a unit vector we have
                d.x^2 + d.y^2 + d.z^2 == 1
                            Dot(d, d) == 1
            
                (v*t)^2 = (dy.x*t + (y.x-x.x))^2 
                        + (dy.y*t + (y.y-x.y))^2 
                        + (dy.z*t + (y.z-x.z))^2

                (v*t)^2 = dy.x^2*t^2 + 2*dy.x*(y.x-x.x)*t + (y.x-x.x)^2
                        + dy.y^2*t^2 + 2*dy.y*(y.y-x.y)*t + (y.y-x.y)^2
                        + dy.z^2*t^2 + 2*dy.z*(y.z-x.z)*t + (y.z-x.z)^2
                v^2*t^2 = dy^2*t^2 + 2dy*yx*t + yx^2
                
                so
                    0 = a*t^2 + b*t + c

                where
                a = v^2 - dy.x^2 - dy.y^2 - dy.z^2
                  = v^2 - (dy.x^2 + dy.y^2 + dy.z^2) 
                  = v^2 - Dot(dy, dy)

                b = -2*dy.x(y.x-x.x) - 2*dy.y(y.y-x.y) - 2*dy.z(y.z-x.z)
                  = -2 * (dy.x*yx.x  +   dy.y * yx.y   +   dy.z*yx.z)
                  = -2 * Dot(dy, yx)

                c = (y.x-x.x)^2 + (y.y-x.y)^2 + (y.z-x.z)^2
                  = -1 * ((y.x-x.x)^2 + (y.y-x.y)^2 + (y.z-x.z)^2)
                  = -1 * ((yx.x)^2    + (yx.y)^2    + (yx.z)^2)
                  = -1 * Dot(yx, yx)

                This is a quadratic equation in t and has solutions
                t = (-b +- sqrt(b^2 - 4*a*c)) / (2*a)
            
            finally d (direction normalized) can be solved
                x + dx*t = y + dy*t
                dx = (dy*t + yx) * (1/t)
                dx = dy + yx/t
                
             */
            var yx = y - aimRay.origin;

            var a = (v * v) - Vector3.Dot(dy, dy);
            var b = -2 * Vector3.Dot(dy, yx);
            var c = -1 * Vector3.Dot(yx, yx);

            var d = (b * b) - (4 * a * c);
            if (d > 0)
            {
                d = Mathf.Sqrt(d);
                var t1 = (-b + d) / (2 * a);
                var t2 = (-b - d) / (2 * a);
                var t = Mathf.Max(t1, t2);
                if (t > 0)
                {
                    aimRay.direction = (dy * t + yx) / t;
                }
            }
            return aimRay;
        }

        public static bool GetTargetHurtbox(CharacterBody body, Ray aimRay, out CharacterBody targetBody, out Vector3 targetPosition)
        {
            var aiComponents = body.master.aiComponents;
            for (int i = 0; i < aiComponents.Length; i++)
            {
                var enemy = aiComponents[i].currentEnemy;
                enemy.Update();
                if (enemy.gameObject && enemy.bestHurtBox && FuckMyAss.FuckingNullCheckHurtBox(enemy.bestHurtBox, out targetBody, out targetPosition))
                    return true;
            }

            var enemySearch = new BullseyeSearch
            {
                viewer = body,
                teamMaskFilter = TeamMask.GetEnemyTeams(body.teamComponent.teamIndex),
                sortMode = BullseyeSearch.SortMode.DistanceAndAngle,
                minDistanceFilter = 0f,
                maxDistanceFilter = 200f,
                searchOrigin = aimRay.origin,
                searchDirection = aimRay.direction,
                maxAngleFilter = 180f,
                filterByLoS = true
            };
            enemySearch.RefreshCandidates();
            return FuckMyAss.FuckingNullCheckHurtBox(enemySearch.GetResults().FirstOrDefault(), out targetBody, out targetPosition);
        }
        #endregion
    }
}
