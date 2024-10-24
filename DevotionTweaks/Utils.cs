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
            var pos = hitbox.position;
            var rotation = hitbox.rotation;
            var lossyScale = hitbox.lossyScale * 0.5f;
            var posY = Mathf.Clamp(footPosition.y, pos.y - lossyScale.y, pos.y + lossyScale.y);

            var fr = rotation * new Vector3(pos.x + lossyScale.x, posY, pos.z + lossyScale.z);
            var bl = rotation * new Vector3(pos.x - lossyScale.x, posY, pos.z - lossyScale.z);
            var fl = rotation * new Vector3(pos.x + lossyScale.x, posY, pos.z - lossyScale.z);
            var br = rotation * new Vector3(pos.x - lossyScale.x, posY, pos.z + lossyScale.z);

            // compare diagonals
            var v1 = Util.ClosestPointOnLine(fl, br, footPosition);
            var v2 = Util.ClosestPointOnLine(fr, bl, footPosition);

            distance = (v1 - footPosition).sqrMagnitude;
            var distance2 = (v2 - footPosition).sqrMagnitude;
            if (distance < distance2)
                return v1;

            distance = distance2;
            return v2;
        }
        #endregion
    }
}
