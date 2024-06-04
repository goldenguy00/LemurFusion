using MonoMod.Cil;
using Mono.Cecil.Cil;
using RoR2;
using RoR2.CharacterAI;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace LemurFusion
{
    public class AITweaks
    {
        public AITweaks()
        {
            ModifyAI(DevotionTweaks.masterPrefab);
            On.RoR2.CharacterAI.BaseAI.FindEnemyHurtBox += FullVision;
            //IL.RoR2.CharacterAI.BaseAI.FindEnemyHurtBox += TargetOnlyPlayers;
        }
        private void ModifyAI(GameObject masterPrefab)
        {
            foreach (var driver in masterPrefab.GetComponentsInChildren<AISkillDriver>())
            {
                switch (driver.customName)
                {
                    case "ReturnToLeaderDefault":
                        driver.driverUpdateTimerOverride = -1f;
                        driver.resetCurrentEnemyOnNextDriverSelection = true;
                        break;
                    case "WaitNearLeader":
                        driver.driverUpdateTimerOverride = -1f;
                        driver.resetCurrentEnemyOnNextDriverSelection = true;
                        break;
                    case "DevotedSecondarySkill":
                        driver.shouldSprint = true;
                        break;
                }
            }
        }

        private void TargetOnlyPlayers(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt<BullseyeSearch>(nameof(BullseyeSearch.GetResults))))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((IEnumerable<HurtBox> results, BaseAI instance) =>
                {
                    if (instance && instance.master.name == DevotionTweaks.masterCloneName)
                    {
                        // Filter results to only target players (don't target player allies like drones)
                        IEnumerable<HurtBox> bigPriorityTargets = results.Where(hurtBox =>
                        {
                            GameObject entityObject = HurtBox.FindEntityObject(hurtBox);
                            return entityObject && entityObject.TryGetComponent(out CharacterBody characterBody) && (characterBody.isBoss || characterBody.isChampion);
                        });

                        // If there are no players, use the default target so that the AI doesn't end up doing nothing
                        return bigPriorityTargets.Any() ? bigPriorityTargets : results;
                    }
                    else
                        return results;
                });
            }
        }

        private HurtBox FullVision(On.RoR2.CharacterAI.BaseAI.orig_FindEnemyHurtBox orig, BaseAI self, float maxDistance, bool full360Vision, bool filterByLoS)
        {
            if (self && self.master.name == DevotionTweaks.masterCloneName)
            {
                maxDistance = 100;
                full360Vision = true;
                filterByLoS = false;
            }

            return orig(self, maxDistance, full360Vision, filterByLoS);
        }
    }
}