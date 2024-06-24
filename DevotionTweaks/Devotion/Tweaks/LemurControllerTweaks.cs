using LemurFusion.Config;
using RoR2;
using System.Linq;
using UnityEngine.AddressableAssets;
using UnityEngine;

namespace LemurFusion.Devotion.Tweaks
{
    internal class LemurControllerTweaks
    {
        public static LemurControllerTweaks instance { get; private set; }

        private LemurControllerTweaks()
        {
            On.DevotedLemurianController.InitializeDevotedLemurian += DevotedLemurianController_InitializeDevotedLemurian;
            On.DevotedLemurianController.OnDevotedBodyDead += DevotedLemurianController_OnDevotedBodyDead;
            On.RoR2.CharacterMasterNotificationQueue.PushItemTransformNotification += CharacterMasterNotificationQueue_PushItemTransformNotification;
        }

        public static void Init()
        {
            if (instance != null)
                return;

            instance = new LemurControllerTweaks();
        }

        #region Hooks

        private void CharacterMasterNotificationQueue_PushItemTransformNotification(
            On.RoR2.CharacterMasterNotificationQueue.orig_PushItemTransformNotification orig,
            CharacterMaster characterMaster, ItemIndex oldIndex, ItemIndex newIndex,
            CharacterMasterNotificationQueue.TransformationType transformationType)
        {
            orig(characterMaster, oldIndex, newIndex, transformationType);

            if (characterMaster.hasAuthority)
                return;

            if (characterMaster.name.StartsWith(DevotionTweaks.devotedMasterName) &&
                characterMaster.TryGetComponent<BetterLemurController>(out var lemCtrl))
            {
                if (lemCtrl.LemurianInventory && lemCtrl._devotedItemList.TryGetValue(oldIndex, out var oldDevotionCount))
                {
                    if (transformationType == CharacterMasterNotificationQueue.TransformationType.ContagiousVoid)
                    {
                        if (DevotionTweaks.EnableSharedInventory)
                            lemCtrl._devotionInventoryController.RemoveItem(oldIndex, oldDevotionCount);
                        Utils.AddItem(lemCtrl._devotedItemList, newIndex, oldDevotionCount);
                        Utils.SetItem(lemCtrl._devotedItemList, oldIndex, 0);
                    }
                    else
                    {
                        if (DevotionTweaks.EnableSharedInventory)
                            lemCtrl._devotionInventoryController.RemoveItem(oldIndex);
                        Utils.AddItem(lemCtrl._devotedItemList, newIndex);
                        Utils.RemoveItem(lemCtrl._devotedItemList, oldIndex);
                    }


                    lemCtrl._devotionInventoryController.UpdateAllMinions(false);
                }
            }
        }

        private void DevotedLemurianController_InitializeDevotedLemurian(On.DevotedLemurianController.orig_InitializeDevotedLemurian orig,
            DevotedLemurianController self, ItemIndex itemIndex, DevotionInventoryController devInvCtrl)
        {
            orig(self, itemIndex, devInvCtrl);

            if (self is BetterLemurController lemCtrl)
            {
                lemCtrl._leashDistSq = PluginConfig.teleportDistance.Value * PluginConfig.teleportDistance.Value;

                if (PluginConfig.cloneReplacesRevive.Value)
                {
                    if (itemIndex == RoR2Content.Items.ExtraLife.itemIndex)
                    {
                        CreateTwin_ExtraLife(lemCtrl.FusionCount, lemCtrl.DevotedEvolutionLevel, RoR2Content.Items.Bear.itemIndex,
                            self.gameObject.transform.position, self.gameObject.transform.rotation, devInvCtrl);
                        itemIndex = RoR2Content.Items.ExtraLifeConsumed.itemIndex;
                        lemCtrl._devotionItem = itemIndex;
                    }
                    else if (itemIndex == DLC1Content.Items.ExtraLifeVoid.itemIndex)
                    {
                        CreateTwin_ExtraLife(lemCtrl.FusionCount, lemCtrl.DevotedEvolutionLevel, DLC1Content.Items.BearVoid.itemIndex,
                            self.gameObject.transform.position, self.gameObject.transform.rotation, devInvCtrl);
                        itemIndex = DLC1Content.Items.ExtraLifeVoidConsumed.itemIndex;
                        lemCtrl._devotionItem = itemIndex;
                    }
                }

                lemCtrl._devotedItemList ??= [];
                Utils.AddItem(lemCtrl._devotedItemList, itemIndex);

                lemCtrl._untrackedItemList ??= [];
                if (!lemCtrl._untrackedItemList.Any())
                {
                    Utils.SetItem(lemCtrl._untrackedItemList, CU8Content.Items.LemurianHarness);
                    Utils.SetItem(lemCtrl._untrackedItemList, RoR2Content.Items.MinionLeash);
                    Utils.SetItem(lemCtrl._untrackedItemList, RoR2Content.Items.UseAmbientLevel);
                    Utils.SetItem(lemCtrl._untrackedItemList, RoR2Content.Items.TeleportWhenOob);
                    if (LemurFusionPlugin.riskyInstalled)
                    {
                        lemCtrl.AddRiskyAllyItem();
                    }
                }
            }
        }


        private void CreateTwin_ExtraLife(int fusionCount, int evoCount, ItemIndex devotedItem, Vector3 position, Quaternion rotation, DevotionInventoryController devotionInventoryController)
        {
            if (devotionInventoryController)
            {
                CharacterMaster ownerMaster = devotionInventoryController._summonerMaster;
                if (ownerMaster)
                {
                    CharacterBody ownerBody = ownerMaster.GetBody();
                    if (ownerBody)
                    {
                        MasterSummon masterSummon = new MasterSummon
                        {
                            masterPrefab = DevotionTweaks.masterPrefab,
                            position = position,
                            rotation = rotation,
                            summonerBodyObject = ownerBody.gameObject,
                            ignoreTeamMemberLimit = true,
                            useAmbientLevel = true
                        };
                        CharacterMaster twinMaster = masterSummon.Perform();

                        if (twinMaster && twinMaster.TryGetComponent<BetterLemurController>(out var lemCtrl))
                        {
                            lemCtrl.InitializeDevotedLemurian(devotedItem, devotionInventoryController);
                            if (fusionCount != 0) lemCtrl.FusionCount = fusionCount;
                            lemCtrl.DevotedEvolutionLevel = evoCount;
                            if (evoCount > 1) lemCtrl._lemurianMaster.TransformBody(DevotionTweaks.devotedBigLemBodyName);

                            devotionInventoryController.UpdateAllMinions(false);
                        }
                    }
                }
            }
        }

        private void DevotedLemurianController_OnDevotedBodyDead(On.DevotedLemurianController.orig_OnDevotedBodyDead orig, DevotedLemurianController self)
        {
            if (self is not BetterLemurController lemCtrl)
            {
                orig(self);
                return;
            }
            if (!lemCtrl || !lemCtrl._lemurianMaster)
                return;

            if (!lemCtrl._lemurianMaster.IsInvoking("RespawnExtraLife") && !lemCtrl._lemurianMaster.IsInvoking("RespawnExtraLife"))
            {
                lemCtrl._lemurianMaster.destroyOnBodyDeath = true;
                lemCtrl.DropScrapOnDeath();

                if (ConfigExtended.DeathDrop_DropEgg.Value)
                    PlaceDevotionEgg(lemCtrl.LemurianBody.footPosition);

                Object.Destroy(lemCtrl._lemurianMaster.gameObject, 1f);
            }
            if (lemCtrl._devotionInventoryController)
                lemCtrl._devotionInventoryController.UpdateAllMinions(false);

            // not a fan of doing this but fuck it, the vanilla class is giga hard coded
            // id essentially just have to ILModify it to do literally nothing anyways.
            //
            // fuck you gearbox.
            //
            // orig(self);
        }

        private void PlaceDevotionEgg(Vector3 spawnLoc)
        {
            if (!Run.instance || !DirectorCore.instance) return;
            if (Physics.Raycast(spawnLoc + Vector3.up * 1f, Vector3.down, out var raycastHit, float.PositiveInfinity, LayerIndex.world.mask))
            {
                DirectorPlacementRule placementRule = new()
                {
                    placementMode = DirectorPlacementRule.PlacementMode.Direct,
                    position = raycastHit.point
                };
                DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(Addressables.LoadAssetAsync<SpawnCard>
                    ("RoR2/CU8/LemurianEgg/iscLemurianEgg.asset").WaitForCompletion(), placementRule, new Xoroshiro128Plus(0UL)));
            }
        }
        #endregion
    }
}
