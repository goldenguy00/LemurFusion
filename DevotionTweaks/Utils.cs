using System.Runtime.CompilerServices;
using System.Text;
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

        public static Vector3 ClosestPointOnCollider(Collider collider, Vector3 worldPoint, out float distance)
        {
            var closestPoint = collider.ClosestPoint(worldPoint);

            if ((closestPoint - worldPoint).sqrMagnitude < 0.001f)
                return ClosestPointOnTransform(collider.transform, worldPoint, out distance);

            distance = (closestPoint - worldPoint).sqrMagnitude;
            return closestPoint;
        }

        public static Vector3 ClosestPointOnTransform(Transform transform, Vector3 worldPoint, out float distance)
        {
            var localPoint = transform.InverseTransformPoint(worldPoint);
            var closestPoint = transform.TransformPoint(ClosestPointOnBounds(Vector3.zero, Vector3.one, localPoint, out distance));

            distance = Mathf.Sign(distance) * (closestPoint - worldPoint).sqrMagnitude;
            return closestPoint;
        }

        public static Vector3 ClosestPointOnBounds(Vector3 position, Vector3 size, Vector3 otherPoint, out float distance)
        {
            var bounds = new Bounds(position, size);

            Vector3 closestPoint;
            if (bounds.Contains(otherPoint))
            {
                closestPoint = bounds.ClosestPointInternal(otherPoint);
                distance = -(closestPoint - otherPoint).sqrMagnitude;
            }
            else
            {
                closestPoint = bounds.ClosestPoint(otherPoint);
                distance = (closestPoint - otherPoint).sqrMagnitude;
            }

            return closestPoint;
        }

        public static Vector3 ClosestPointInternal(this ref Bounds bounds, Vector3 localPoint)
        {
            var max = bounds.max;
            var min = bounds.min;

            // right
            var plane = new Plane(Vector3.right, max.x);
            var closestPoint = plane.ClosestPointOnPlane(localPoint);
            var distance = (closestPoint - localPoint).sqrMagnitude;

            // left
            plane.distance = min.x;
            var planePoint = plane.ClosestPointOnPlane(localPoint);
            var other = (planePoint - localPoint).sqrMagnitude;
            if (other < distance)
            {
                distance = other;
                closestPoint = planePoint;
            }

            // up
            plane.distance = max.y;
            plane.normal = Vector3.up;
            planePoint = plane.ClosestPointOnPlane(localPoint);
            other = (planePoint - localPoint).sqrMagnitude;
            if (other < distance)
            {
                distance = other;
                closestPoint = planePoint;
            }

            // down
            plane.distance = min.y;
            planePoint = plane.ClosestPointOnPlane(localPoint);
            other = (planePoint - localPoint).sqrMagnitude;
            if (other < distance)
            {
                distance = other;
                closestPoint = planePoint;
            }

            // forward
            plane.distance = max.z;
            plane.normal = Vector3.forward;
            planePoint = plane.ClosestPointOnPlane(localPoint);
            other = (planePoint - localPoint).sqrMagnitude;
            if (other < distance)
            {
                distance = other;
                closestPoint = planePoint;
            }

            // back
            plane.distance = min.z;
            planePoint = plane.ClosestPointOnPlane(localPoint);
            other = (planePoint - localPoint).sqrMagnitude;
            if (other < distance)
            {
                distance = other;
                closestPoint = planePoint;
            }

            return Vector3.MoveTowards(localPoint, closestPoint, -distance);
        }

        #endregion
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            if (!component)
                return go.AddComponent<T>();
            return component;
        }
        public static T GetOrAddComponent<T>(this Component c) where T : Component
        {
            var component = c.GetComponent<T>();
            if (!component)
                return c.gameObject.AddComponent<T>();
            return component;
        }
    }
}
