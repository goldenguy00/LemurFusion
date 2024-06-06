using RoR2;
using System.Collections.Generic;

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
                target[itemIndex] = count;
            else
                target.Add(itemIndex, count);
        }
        #endregion
    }
}
