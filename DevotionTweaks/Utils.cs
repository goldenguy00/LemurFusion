using System.Runtime.CompilerServices;
using LemurFusion.Config;
using RoR2;
using UnityEngine;

namespace LemurFusion
{
    public static class Utils
    {
        #region Devotion Utils

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDevoted(CharacterBody body) => body && IsDevoted(body.master);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDevoted(CharacterMaster master) => master && master.teamIndex == TeamIndex.Player && master.hasBody && master.TryGetComponent<BetterLemurController>(out var lemCtrl) && lemCtrl.LemurianInventory;

        public static void ResetItem(this Inventory self, ItemIndex itemIndex, int count)
        {
            if (count == 0)
            {
                self.itemAcquisitionOrder.Remove(itemIndex);
                self.ResetItem(itemIndex);
            }
            else if ((uint)itemIndex < self.itemStacks.Length)
            {
                ref int reference = ref self.itemStacks[(int)itemIndex];
                if (reference != count)
                {
                    if (reference == 0)
                    {
                        self.itemAcquisitionOrder.Add(itemIndex);
                        self.SetDirtyBit(8u);
                    }

                    reference = count;
                    self.SetDirtyBit(1u);
                    self.HandleInventoryChanged();
                }
            }
        }
        #endregion

        #region Stat Modifiers
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
        public static Vector3 NearestPointOnTransform(Transform hitbox, Vector3 footPosition, out float distance)
        {
            // take slice
            var bounds = new Bounds(Vector3.zero, hitbox.lossyScale);
            var localFootPos = hitbox.InverseTransformPoint(footPosition);
            var localClosest = bounds.ClosestPoint(localFootPos);
            if (localClosest != localFootPos || (Mathf.Abs(bounds.size.x / bounds.size.y) < 2f))
            {
                distance = (localClosest - localFootPos).sqrMagnitude;
                return hitbox.TransformPoint(localClosest);
            }

            return hitbox.TransformPoint(ClosetPointOnBounds(localFootPos, bounds, out distance));
        }

        public static Vector3 ClosetPointOnBounds(Vector3 point, Bounds bounds, out float distance)
        {
            var points = HG.ListPool<Vector3>.RentCollection();

            var plane = new Plane(Vector3.up, bounds.max);
            points.Add(plane.ClosestPointOnPlane(point));

            plane.SetNormalAndPosition(Vector3.down, bounds.min);
            points.Add(plane.ClosestPointOnPlane(point));

            plane.SetNormalAndPosition(Vector3.forward, bounds.max);
            points.Add(plane.ClosestPointOnPlane(point));

            plane.SetNormalAndPosition(Vector3.back, bounds.min);
            points.Add(plane.ClosestPointOnPlane(point));

            plane.SetNormalAndPosition(Vector3.right, bounds.max);
            points.Add(plane.ClosestPointOnPlane(point));

            plane.SetNormalAndPosition(Vector3.left, bounds.min);
            points.Add(plane.ClosestPointOnPlane(point));

            Vector3 closest = point;
            distance = float.MaxValue;
            foreach (var p in  points)
            {
                float dist = (p - point).sqrMagnitude;
                if (dist < distance)
                {
                    distance = dist;
                    closest = p;
                }
            }

            return closest;
        }
        #endregion
    }
}
