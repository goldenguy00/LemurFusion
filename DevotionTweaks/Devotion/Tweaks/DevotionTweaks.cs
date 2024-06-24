using System;
using System.Collections.Generic;
using System.Linq;
using R2API;
using RoR2;
using RoR2.CharacterAI;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using UnityEngine;
using UnityEngine.Networking;
using LemurFusion.Config;

namespace LemurFusion.Devotion.Tweaks
{
    public class DevotionTweaks
    {
        public enum DeathItem
        {
            None,
            Scrap,
            Original,
            Custom
        }

        public static DevotionTweaks instance { get; private set; }

        public static HashSet<EquipmentIndex> gigaChadLvl = [];
        public static HashSet<EquipmentIndex> lowLvl = [];
        public static HashSet<EquipmentIndex> highLvl = [];

        public static GameObject masterPrefab;
        public static GameObject bodyPrefab;
        public static GameObject bigBodyPrefab;

        public const string devotedPrefix = "Devoted";
        public const string cloneSuffix = "(Clone)";

        public const string lemBodyName = "LemurianBody";
        public const string bigLemBodyName = "LemurianBruiserBody";

        public const string devotedMasterName = "BetterDevotedLemurianMaster";
        public const string devotedLemBodyName = devotedPrefix + lemBodyName;
        public const string devotedBigLemBodyName = devotedPrefix + bigLemBodyName;

        public static bool EnableSharedInventory { get; private set; }

        private DevotionTweaks()
        {
            EnableSharedInventory = PluginConfig.enableSharedInventory.Value;

            // artifact setup
            RunArtifactManager.onArtifactEnabledGlobal += RunArtifactManager_onArtifactEnabledGlobal;
            RunArtifactManager.onArtifactDisabledGlobal += RunArtifactManager_onArtifactDisabledGlobal;

            // external lem hooks
            IL.RoR2.CharacterAI.LemurianEggController.SummonLemurian += LemurianEggController_SummonLemurian;

            // egg interaction display
            if (ConfigExtended.Blacklist_Enable.Value)
                On.RoR2.PickupPickerController.SetOptionsFromInteractor += PickupPickerController_SetOptionsFromInteractor;
        }

        public static void Init()
        {
            if (instance != null)
                return;
            instance = new DevotionTweaks();
        }

        #region Artifact Setup
        private void RunArtifactManager_onArtifactEnabledGlobal(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (artifactDef != CU8Content.Artifacts.Devotion) return;

            // global hooks
            On.RoR2.MasterSummon.Perform += MasterSummon_Perform;
            StatTweaks.instance.InitHooks();

            CreateEliteLists();
        }

        private void RunArtifactManager_onArtifactDisabledGlobal(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (artifactDef != CU8Content.Artifacts.Devotion) return;

            On.RoR2.MasterSummon.Perform -= MasterSummon_Perform;
            StatTweaks.instance.RemoveHooks();

            lowLvl.Clear();
            highLvl.Clear();
            gigaChadLvl.Clear();
        }

        private void CreateEliteLists()
        {
            lowLvl.Clear();
            highLvl.Clear();
            gigaChadLvl.Clear();

            // thank you moffein for showing me the way, fuck this inconsistent bs
            // holy shit this is horrible i hate it i hate it i hate it
            foreach (var etd in EliteAPI.GetCombatDirectorEliteTiers().ToList())
            {
                if (etd != null && etd.eliteTypes.Length > 0)
                {
                    if (etd.eliteTypes.Any(ed => ed != null && ed.name.Contains("Honor") == true)) continue;

                    var isT2 = false;
                    var isT1 = false;
                    foreach (EliteDef ed in etd.eliteTypes)
                    {
                        if (!ed || !ed.eliteEquipmentDef) continue;

                        if (ed.eliteEquipmentDef == RoR2Content.Equipment.AffixPoison || ed.eliteEquipmentDef == RoR2Content.Equipment.AffixLunar)
                        {
                            isT2 = true;
                            break;
                        }
                        else if (ed.eliteEquipmentDef == RoR2Content.Equipment.AffixBlue)
                        {
                            isT1 = true;
                            break;
                        }
                    }

                    if (isT1 || isT2)
                    {
                        foreach (var ed in etd.eliteTypes)
                        {
                            if (!ed || !ed.eliteEquipmentDef ||
                                ed.eliteEquipmentDef.equipmentIndex == EquipmentIndex.None ||
                                !ed.eliteEquipmentDef.passiveBuffDef ||
                                !ed.eliteEquipmentDef.passiveBuffDef.isElite) continue;

                            if (isT1)
                            {
                                lowLvl.Add(ed.eliteEquipmentDef.equipmentIndex);
                                highLvl.Add(ed.eliteEquipmentDef.equipmentIndex);
                            }
                            if (isT2)
                            {
                                highLvl.Add(ed.eliteEquipmentDef.equipmentIndex);
                                gigaChadLvl.Add(ed.eliteEquipmentDef.equipmentIndex);
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Summon
        private void PickupPickerController_SetOptionsFromInteractor(On.RoR2.PickupPickerController.orig_SetOptionsFromInteractor orig, PickupPickerController self, Interactor activator)
        {
            if (!self.GetComponent<LemurianEggController>() || !activator || !activator.TryGetComponent<CharacterBody>(out var body) || !body.inventory)
            {
                orig(self, activator);
                return;
            }

            List<PickupPickerController.Option> list = [];
            foreach (var itemIndex in body.inventory.itemAcquisitionOrder)
            {
                if (ConfigExtended.Blacklist_Filter(itemIndex))
                {
                    PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(itemIndex);
                    if (pickupIndex != PickupIndex.none)
                    {
                        list.Add(new PickupPickerController.Option
                        {
                            available = true,
                            pickupIndex = pickupIndex
                        });
                    }
                }
            }
            self.SetOptionsServer(list.ToArray());
        }

        private CharacterMaster MasterSummon_Perform(On.RoR2.MasterSummon.orig_Perform orig, MasterSummon self)
        {
            if (self?.masterPrefab == masterPrefab && self.summonerBodyObject && self.summonerBodyObject.TryGetComponent<CharacterBody>(out var body) && body)
            {
                // successful lemurmeld
                // hijacking this var for CreateTwin_ExtraLife. fuck your config, you get another friend.
                var targetSummon = self.ignoreTeamMemberLimit ? null : TrySummon(body.masterObjectId);
                self.ignoreTeamMemberLimit = true;
                if (targetSummon != null)
                {
                    return targetSummon;
                }
            }
            return orig(self);
        }

        private CharacterMaster TrySummon(NetworkInstanceId summoner)
        {
            List<BetterLemurController> lemCtrlList = [];
            foreach (MinionOwnership minionOwnership in MinionOwnership.MinionGroup.FindGroup(summoner)?.members ?? [])
            {
                if (minionOwnership && minionOwnership.gameObject.TryGetComponent<BetterLemurController>(out var lemCtrl) && lemCtrl)
                {
                    lemCtrlList.Add(lemCtrl);
                }
            }

            // replace summon with an existing guy
            // hijacking ignoreMaxAllies for CreateTwin_ExtraLife
            if (lemCtrlList.Count >= PluginConfig.maxLemurs.Value && lemCtrlList.Any())
            {
                var meldTarget = lemCtrlList.OrderBy(l => l.FusionCount).FirstOrDefault();
                if (meldTarget && meldTarget._lemurianMaster)
                {
                    meldTarget.FusionCount++;
                    return meldTarget._lemurianMaster;
                }
            }

            // let orig do the summoning
            return null;
        }

        private static void LemurianEggController_SummonLemurian(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.Before,
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<LemurianEggController>(nameof(LemurianEggController.masterPrefab))
                ))
            {
                c.RemoveRange(2);
                c.Emit<DevotionTweaks>(OpCodes.Ldsfld, nameof(masterPrefab));
            }
            else
            {
                LemurFusionPlugin.LogError("Hook failed for LemurianEggController_SummonLemurian # 1");
            }

            if (c.TryGotoNext(MoveType.Before,
                i => i.MatchStfld<MasterSummon>(nameof(MasterSummon.ignoreTeamMemberLimit))
                ))
            {
                c.Prev.OpCode = OpCodes.Ldc_I4_0;
            }
            else
            {
                LemurFusionPlugin.LogError("Hook failed for LemurianEggController_SummonLemurian # 2");
            }
        }
        #endregion

    }
}
