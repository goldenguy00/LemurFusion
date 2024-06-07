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
        }

        private void ModifyAI(GameObject masterPrefab)
        {
            foreach (var driver in masterPrefab.GetComponentsInChildren<AISkillDriver>())
            {
                switch (driver.customName)
                {
                    case "ReturnToLeaderDefault":
                        driver.driverUpdateTimerOverride = 1f;
                        driver.resetCurrentEnemyOnNextDriverSelection = true;
                        break;
                    case "WaitNearLeader":
                        driver.driverUpdateTimerOverride = 1f;
                        driver.resetCurrentEnemyOnNextDriverSelection = true;
                        break;
                    case "DevotedSecondarySkill":
                        driver.shouldSprint = true;
                        break;
                }
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