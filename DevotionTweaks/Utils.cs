using LemurFusion.Config;
using RoR2;
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
            return (meldCount * (configValue * 0.01f)) + (GetLevelModifier(evolutionCount) * 0.1f);
        }

        public static float GetLevelModifier(int evolutionCount)
        {
            if (!Run.instance) return 0;

            // big bonuses of 100%->300% for early stage and evolutions then continue with 10% increases later on
            var stageModifier = Mathf.Clamp(Mathf.Max(Run.instance.stageClearCount, evolutionCount), 1, 3);
            return  stageModifier + ((evolutionCount * 0.1f) * (PluginConfig.statMultEvo.Value * 0.01f));
        }
        #endregion
    }
}
