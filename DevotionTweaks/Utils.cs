using LemurFusion.Config;
using LemurFusion.Devotion;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using System.Collections.Generic;
using System.Linq;
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

            Vector3 vect = NearestPointOnLine(point, p1, p2);
            Vector3 otherVect = NearestPointOnLine(point, p3, p4);

            if (Vector3.Distance(vect, point) < Vector3.Distance(otherVect, point))
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

        public static bool AllowPrediction(CharacterBody body)
        {
            return body && body.master && body.master.name.Contains(DevotionTweaks.devotedMasterName) && body.teamComponent.teamIndex == TeamIndex.Player;
        }

        public static Ray PredictAimrayPS(Ray aimRay, GameObject projectilePrefab, HurtBox targetHurtBox)
        {
            float speed = -1f;
            if (projectilePrefab)
            {
                ProjectileSimple ps = projectilePrefab.GetComponent<ProjectileSimple>();
                if (ps)
                {
                    speed = ps.desiredForwardSpeed;
                }
            }

            if (speed <= 0f)
            {
                LemurFusionPlugin.LogError("Could not get speed of ProjectileSimple.");
                return aimRay;
            }

            return PredictAimray(aimRay, speed, targetHurtBox);
        }

        public static Ray PredictAimray(Ray aimRay, float projectileSpeed, HurtBox targetHurtBox)
        {
            if (targetHurtBox == null)
            {
                targetHurtBox = AcquireTarget(aimRay);
            }

            bool hasHurtbox = targetHurtBox && targetHurtBox.healthComponent && targetHurtBox.healthComponent.body && targetHurtBox.healthComponent.body.characterMotor;
            if (hasHurtbox && projectileSpeed > 0f)
            {
                CharacterBody targetBody = targetHurtBox.healthComponent.body;
                Vector3 targetPosition = targetHurtBox.transform.position;

                //Velocity shows up as 0 for clients due to not having authority over the CharacterMotor
                Vector3 targetVelocity = targetBody.characterMotor.velocity;
                if (!targetBody.hasAuthority)
                {
                    //Less accurate, but it works online.
                    targetVelocity = (targetBody.transform.position - targetBody.previousPosition) / Time.fixedDeltaTime;
                }

                if (targetVelocity.sqrMagnitude > 0f && !(targetBody && targetBody.hasCloakBuff))   //Dont bother predicting stationary targets
                {
                    //A very simplified way of estimating, won't be 100% accurate.
                    Vector3 currentDistance = targetPosition - aimRay.origin;
                    float timeToImpact = currentDistance.magnitude / projectileSpeed;
                    //Vertical movenent isn't predicted well by this, so just use the target's current Y
                    Vector3 lateralVelocity = new Vector3(targetVelocity.x, 0f, targetVelocity.z);
                    Vector3 futurePosition = targetPosition + lateralVelocity * timeToImpact;

                    //Only attempt prediction if player is jumping upwards.
                    //Predicting downwards movement leads to groundshots.
                    if (targetBody.characterMotor && !targetBody.characterMotor.isFlying && !targetBody.characterMotor.isGrounded && targetVelocity.y > 0f)
                    {
                        //point + vt + 0.5at^2
                        float futureY = targetPosition.y + targetVelocity.y * timeToImpact;
                        futureY += 0.5f * Physics.gravity.y * timeToImpact * timeToImpact;
                        futurePosition.y = futureY;
                    }

                    Ray newAimray = new()
                    {
                        origin = aimRay.origin,
                        direction = (futurePosition - aimRay.origin).normalized
                    };

                    float angleBetweenVectors = Vector3.Angle(aimRay.direction, newAimray.direction);
                    if (angleBetweenVectors <= AITweaks.basePredictionAngle)
                    {
                        return newAimray;
                    }
                }
            }

            return aimRay;
        }

        public static HurtBox AcquireTarget(Ray aimRay)
        {
            BullseyeSearch search = new()
            {
                teamMaskFilter = TeamMask.GetEnemyTeams(TeamIndex.Player),
                filterByLoS = true,
                searchOrigin = aimRay.origin,
                sortMode = BullseyeSearch.SortMode.Angle,
                maxDistanceFilter = 200f,
                maxAngleFilter = AITweaks.basePredictionAngle,
                searchDirection = aimRay.direction
            };
            search.RefreshCandidates();

            return search.GetResults().FirstOrDefault();
        }

        public static HurtBox GetMasterAITargetHurtbox(CharacterMaster cm)
        {
            if (cm && cm.aiComponents.Length > 0)
            {
                foreach (BaseAI ai in cm.aiComponents)
                {
                    if (ai.currentEnemy != null && ai.currentEnemy.bestHurtBox != null)
                    {
                        return ai.currentEnemy.bestHurtBox;
                    }
                }
            }
            return null;
        }
        #endregion
    }
}
