using HG;
using LemurFusion.Config;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LemurFusion
{
    internal static class Utils
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
            if (target.ContainsKey(itemIndex))
            {
                if (count <= 0)
                    target.Remove(itemIndex);
                else
                    target[itemIndex] = count;
            }
            else
                target.Add(itemIndex, count);
        }

        public static void RemoveItem(SortedList<ItemIndex, int> target, ItemDef itemDef, int count = 1)
        {
            if (!itemDef) return;
            RemoveItem(target, itemDef.itemIndex, count);
        }

        public static void RemoveItem(SortedList<ItemIndex, int> target, ItemIndex itemIndex, int count = 1)
        {
            if (itemIndex == ItemIndex.None || target == null) return;

            target ??= [];
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

        public static float GetStatModifier(int configValue, int meldCount, int evolutionCount)
        {
            var evolutionModifier = Mathf.Clamp(evolutionCount, 0, 4);
            var configModifier = PluginConfig.statMultEvo.Value * 0.01f;
            return (meldCount - 1) * (configValue * 0.01f) + (evolutionModifier * configModifier * 0.1f);
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

        public static Vector3 EstimateClosestPoint(Transform transform, Vector3 point)
        {
            Vector3 pos = transform.position;
            Vector3 bounds = transform.lossyScale * 0.5f;
            Quaternion rotation = transform.rotation;

            // take the two opposite corners, rotate then find the closest point on that line
            var p1 = new Vector3(pos.x + bounds.x, pos.y, pos.z + bounds.z);
            var p2 = new Vector3(pos.x - bounds.x, pos.y, pos.z - bounds.z);
            var p3 = new Vector3(pos.x + bounds.x, pos.y, pos.z - bounds.z);
            var p4 = new Vector3(pos.x - bounds.x, pos.y, pos.z + bounds.z);
            p1 = rotation * p1;
            p2 = rotation * p2;
            p3 = rotation * p3;
            p4 = rotation * p4;
            Vector3 vect = NearestPointOnLine(point, p1, p2);
            Vector3 otherVect = NearestPointOnLine(point, p3, p4);

            if (Vector3.Distance(vect, point) < Vector3.Distance(otherVect, point))
                return vect;
            return otherVect;
        }

        public static Vector3 NearestPointOnLine(Vector3 point, Vector3 start, Vector3 end)
        {
            var line = (end - start);
            var len = line.magnitude;
            line.Normalize();

            var v = point - start;
            var d = Vector3.Dot(v, line);
            d = Mathf.Clamp(d, 0f, len);
            return start + line * d;
        }
    }
}
