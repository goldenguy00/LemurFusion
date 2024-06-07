using LemurFusion.Config;
using RoR2.CharacterAI;
using UnityEngine;

namespace LemurFusion
{
    public class AITweaks
    {
        public static AITweaks instance;

        public static void Init()
        {
            if (instance != null) return;

            instance = new AITweaks();
        }

        private AITweaks()
        {
            if (PluginConfig.improveAI.Value)
            {
                foreach (var driver in DevotionTweaks.masterPrefab.GetComponentsInChildren<AISkillDriver>())
                {
                    switch (driver.customName)
                    {
                        case "ReturnToLeaderDefault":
                            driver.driverUpdateTimerOverride = 0.2f;
                            driver.resetCurrentEnemyOnNextDriverSelection = true;
                            break;
                        case "WaitNearLeader":
                            driver.driverUpdateTimerOverride = 0.2f;
                            driver.resetCurrentEnemyOnNextDriverSelection = true;
                            break;
                    }
                }
            }
        }
    }
}