using System;
using System.Collections.Generic;
using System.Linq;

using R2API;
using RoR2;
using RoR2.CharacterAI;

using MonoMod.Cil;
using Mono.Cecil.Cil;

using UE = UnityEngine;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using LemurFusion.Config;

namespace LemurFusion
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

        public static DevotionTweaks instance;

        public static HashSet<EquipmentIndex> gigaChadLvl = [];
        public static HashSet<EquipmentIndex> lowLvl = [];
        public static HashSet<EquipmentIndex> highLvl = [];

        public static GameObject masterPrefab;
        public static GameObject bodyPrefab;

        public const string masterPrefabName = "BetterDevotedLemurianMaster";
        public const string masterCloneName = masterPrefabName + "(Clone)";

        public static bool EnableSharedInventory { get; private set; }

        private DevotionTweaks() { }

        public static void Init()
        {
            if (instance != null) return;
            instance = new DevotionTweaks();

            EnableSharedInventory = PluginConfig.enableSharedInventory.Value;

            //        //
            // assets //
            //        //
            LoadAssets();

            //       //
            // hooks //
            //       //

            // artifact setup
            RunArtifactManager.onArtifactEnabledGlobal += RunArtifactManager_onArtifactEnabledGlobal;
            RunArtifactManager.onArtifactDisabledGlobal += RunArtifactManager_onArtifactDisabledGlobal;

            // internal hooks
            BetterLemurController.InitHooks();

            // external lem hooks
            IL.RoR2.CharacterAI.LemurianEggController.SummonLemurian += LemurianEggController_SummonLemurian;
            On.RoR2.DevotionInventoryController.ActivateDevotedEvolution += DevotionInventoryController_ActivateDevotedEvolution;
            On.RoR2.DevotionInventoryController.UpdateMinionInventory += DevotionInventoryController_UpdateMinionInventory;
            IL.RoR2.DevotionInventoryController.UpdateMinionInventory += DevotionInventoryController_UpdateMinionInventory;
            IL.RoR2.DevotionInventoryController.EvolveDevotedLumerian += DevotionInventoryController_EvolveDevotedLumerian;
            IL.RoR2.DevotionInventoryController.GenerateEliteBuff += DevotionInventoryController_GenerateEliteBuff;

            if (ConfigExtended.Blacklist_Enable.Value)
                On.RoR2.PickupPickerController.SetOptionsFromInteractor += PickupPickerController_SetOptionsFromInteractor;
        }

        private static void LoadAssets()
        {
            //Fix up the tags on the Harness
            LemurFusionPlugin.LogInfo("Adding Tags ItemTag.BrotherBlacklist, ItemTag.CannotSteal, ItemTag.CannotCopy to Lemurian Harness.");
            ItemDef itemDef = Addressables.LoadAssetAsync<ItemDef>("RoR2/CU8/Harness/LemurianHarness.asset").WaitForCompletion();
            if (itemDef)
            {
                itemDef.tags = itemDef.tags.Concat([ItemTag.BrotherBlacklist, ItemTag.CannotSteal, ItemTag.CannotCopy]).Distinct().ToArray();
            }

            // better master
            masterPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/CU8/LemurianEgg/DevotedLemurianMaster.prefab").WaitForCompletion().InstantiateClone(masterPrefabName, true);
            MonoBehaviour.DestroyImmediate(masterPrefab.GetComponent<DevotedLemurianController>());
            masterPrefab.AddComponent<BetterLemurController>();

            // dupe body
            GameObject lemurianBodyPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Lemurian/LemurianBody.prefab").WaitForCompletion();
            bodyPrefab = lemurianBodyPrefab.InstantiateClone("DevotedLemurianBody", true);
            masterPrefab.GetComponent<CharacterMaster>().bodyPrefab = bodyPrefab;

            // fix original
            CharacterBody lemurianBody = lemurianBodyPrefab.GetComponent<CharacterBody>();
            lemurianBody.bodyFlags &= ~CharacterBody.BodyFlags.Devotion;

            ContentAddition.AddMaster(DevotionTweaks.masterPrefab);
            ContentAddition.AddBody(DevotionTweaks.bodyPrefab);
        }

        #region Artifact Setup
        private static void RunArtifactManager_onArtifactEnabledGlobal(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (artifactDef != CU8Content.Artifacts.Devotion) return;

            // global hooks
            On.RoR2.MasterSummon.Perform += MasterSummon_Perform;
            StatHooks.instance.InitHooks();

            CreateEliteLists();
        }

        private static void RunArtifactManager_onArtifactDisabledGlobal(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (artifactDef != CU8Content.Artifacts.Devotion) return;

            On.RoR2.MasterSummon.Perform -= MasterSummon_Perform;
            StatHooks.instance.RemoveHooks();

            lowLvl.Clear();
            highLvl.Clear();
            gigaChadLvl.Clear();
        }

        private static void CreateEliteLists()
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
        private static void PickupPickerController_SetOptionsFromInteractor(On.RoR2.PickupPickerController.orig_SetOptionsFromInteractor orig, PickupPickerController self, Interactor activator)
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

        private static CharacterMaster MasterSummon_Perform(On.RoR2.MasterSummon.orig_Perform orig, MasterSummon self)
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

        private static CharacterMaster TrySummon(NetworkInstanceId summoner)
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
                c.Emit<DevotionTweaks>(OpCodes.Ldsfld, nameof(DevotionTweaks.masterPrefab));
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

        #region Evolution
        private static void DevotionInventoryController_ActivateDevotedEvolution(On.RoR2.DevotionInventoryController.orig_ActivateDevotedEvolution orig)
        {
            orig();
            foreach (DevotionInventoryController devotionInventoryController in DevotionInventoryController.InstanceList)
            {
                devotionInventoryController.UpdateAllMinions(false);
            }
        }

        private static void DevotionInventoryController_UpdateMinionInventory(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.Before,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<DevotionInventoryController>(nameof(DevotionInventoryController._devotionMinionInventory)),
                    i => i.MatchLdarg(1),
                    i => i.MatchCallvirt<DevotedLemurianController>("get_DevotionItem"),
                    i => i.MatchLdcI4(out _),
                    i => i.MatchCallvirt<Inventory>(nameof(Inventory.GiveItem))
                ))
            {
                c.RemoveRange(6);
            }
            else
            {
                LemurFusionPlugin.LogError("IL Hook failed for DevotionInventoryController_UpdateMinionInventory #1");
            }

            if (c.TryGotoNext(MoveType.After,
                    i => i.MatchCall<DevotionInventoryController>(nameof(DevotionInventoryController.EvolveDevotedLumerian))) &&
                c.TryFindNext(out var next,
                    i => i.MatchCallvirt<Inventory>(nameof(Inventory.GiveItem)),
                    i => i.MatchCallvirt<Inventory>(nameof(Inventory.GiveItem))
                ))
            {
                // nuke all 40 something lines i do not care
                c.MoveAfterLabels();
                c.RemoveRange((next.Last().Index + 1) - c.Index);
            }
            else
            {
                LemurFusionPlugin.LogError("IL Hook failed for DevotionInventoryController_UpdateMinionInventory #2");
            }

            if (c.TryGotoNext(MoveType.Before,
                    i => i.MatchCallvirt<Inventory>(nameof(Inventory.AddItemsFrom))
                ))
            {
                if (ConfigExtended.Blacklist_Enable.Value && EnableSharedInventory)
                {
                    c.Remove();
                    c.Emit<ConfigExtended>(OpCodes.Ldsfld, nameof(ConfigExtended.Blacklist_Filter));
                    c.Emit(OpCodes.Callvirt, typeof(Inventory).GetMethod(nameof(Inventory.AddItemsFrom), [typeof(Inventory), typeof(Func<ItemIndex, bool>)]));
                }
                if (!EnableSharedInventory)
                {
                    c.Remove();
                    c.Emit(OpCodes.Pop);
                    c.Emit(OpCodes.Pop);
                }
            }
            else
            {
                LemurFusionPlugin.LogError("IL Hook failed for DevotionInventoryController_UpdateMinionInventory #3");
            }
        }

        private static void DevotionInventoryController_UpdateMinionInventory(On.RoR2.DevotionInventoryController.orig_UpdateMinionInventory orig,
            DevotionInventoryController self, DevotedLemurianController lem, bool shouldEvolve)
        {
            if (!NetworkServer.active || lem is not BetterLemurController lemCtrl)
            {
                orig(self, lem, shouldEvolve);
                return;
            }

            if (shouldEvolve && lemCtrl._devotedItemList.Any())
            {
                foreach (var item in lemCtrl._devotedItemList.Keys.ToList())
                {
                    if (EnableSharedInventory)
                        self.GiveItem(item);

                    lemCtrl._devotedItemList[item]++;
                }
            }

            orig(self, lem, shouldEvolve);

            lemCtrl.ReturnItems();
        }

        private static void DevotionInventoryController_EvolveDevotedLumerian(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.AfterLabel,
                i => i.MatchLdstr("shouldn't evolve!"),
                i => i.MatchCall<UE.Debug>("LogError")))
            {
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate<Action<DevotedLemurianController>>((lem) =>
                {
                    var list = PluginConfig.highTierElitesOnly.Value ? gigaChadLvl.ToList() : highLvl.ToList();

                    var idx = UE.Random.Range(0, list.Count);
                    lem.LemurianInventory.SetEquipmentIndex(list[idx]);
                });
                c.RemoveRange(2);
            }
            else
            {
                LemurFusionPlugin.LogError("Hook failed for DevotionInventoryController_EvolveDevotedLumerian");
            }
        }
        
        private static void DevotionInventoryController_GenerateEliteBuff(ILContext ll)
        {
            var c = new ILCursor(ll);

            if (c.TryGotoNext(MoveType.Before,
                i => i.MatchBrtrue(out _),
                i => i.MatchLdsfld<DevotionInventoryController>(nameof(DevotionInventoryController.highLevelEliteBuffs)),
                i => i.MatchBr(out _),
                i => i.MatchLdsfld<DevotionInventoryController>(nameof(DevotionInventoryController.lowLevelEliteBuffs))
                ))
            {
                // fuck it just nuke it all
                c.RemoveRange(4);
                c.EmitDelegate<Func<bool, List<EquipmentIndex>>>((isLowLvl) =>
                {
                    return isLowLvl ? lowLvl.ToList() : highLvl.ToList();
                });
            }
            else
            {
                LemurFusionPlugin.LogError("Hook failed for DevotionInventoryController_GenerateEliteBuff");
            }
        }
        #endregion
    }
}
