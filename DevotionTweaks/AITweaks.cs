using EntityStates;
using EntityStates.BrotherMonster;
using EntityStates.BrotherMonster.Weapon;
using EntityStates.Destructible;
using HG;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Networking;
using RoR2.Projectile;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace LemurFusion
{
    public class AITweaks
    {
        public AITweaks()
        {
            On.RoR2.CharacterAI.BaseAI.FindEnemyHurtBox += FullVision;
            ModifyAI(DevotionTweaks.masterPrefab);
            //IL.RoR2.CharacterAI.BaseAI.FindEnemyHurtBox += TargetOnlyPlayers;
        }
        internal static void ModifyAI(GameObject masterPrefab)
        {
            LemurFusionPlugin._logger.LogWarning("Ai Skill Drivers begin");
            foreach (var driver in masterPrefab.GetComponentsInChildren<AISkillDriver>())
            {
                switch (driver.customName)
                {
                    case "ReturnToLeaderDefault":
                        driver.shouldSprint = true;
                        driver.resetCurrentEnemyOnNextDriverSelection = true;
                        break;
                    case "ReturnToOwnerLeash":
                        driver.shouldSprint = true;
                        driver.driverUpdateTimerOverride = -1f;
                        driver.resetCurrentEnemyOnNextDriverSelection = true;
                        break;
                    case "WaitNearLeader":
                        driver.shouldSprint = true;
                        driver.resetCurrentEnemyOnNextDriverSelection = true;
                        break;
                }

                /*switch (driver.customName)
                {
                    case "Slash":
                        driver.minDistance = 15f;
                        driver.maxDistance = 45f;
                        break;
                    case "LeaveNodegraph":
                        driver.minDistance = 0f;
                        driver.maxDistance = 15f;
                        driver.shouldSprint = true;
                        driver.movementType = AISkillDriver.MovementType.FleeMoveTarget;
                        break;
                    case "StrafeBecausePrimaryIsntReady":
                        driver.minDistance = 15f;
                        break;
                    case "BlinkBecauseClose":
                        driver.minDistance = 25f;
                        driver.maxDistance = 45f;
                        break;
                    case "PathToTarget":
                        driver.minDistance = 15f;
                        break;*/
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
                    if (instance && instance.master.name == DevotionTweaks.masterPrefabName && PhaseCounter.instance && PhaseCounter.instance.phase == 3)
                    {
                        // Filter results to only target players (don't target player allies like drones)
                        IEnumerable<HurtBox> playerControlledTargets = results.Where(hurtBox =>
                        {
                            GameObject entityObject = HurtBox.FindEntityObject(hurtBox);
                            return entityObject && entityObject.TryGetComponent(out CharacterBody characterBody) && characterBody.isPlayerControlled;
                        });

                        // If there are no players, use the default target so that the AI doesn't end up doing nothing
                        return playerControlledTargets.Any() ? playerControlledTargets : results;
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
                maxDistance = float.PositiveInfinity;
                full360Vision = true;
                filterByLoS = false;
            }

            return orig(self, maxDistance, full360Vision, filterByLoS);
        }
    }
}