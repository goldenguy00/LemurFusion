using BepInEx;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine;
using UE = UnityEngine;
using RoR2;

namespace LemurFusion.Devotion.Components
{
    public class MechaLemur
    {
        private readonly GameObject chaingunDisplay = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/DroneWeapons/DisplayDroneWeaponMinigun.prefab").WaitForCompletion();
        private readonly GameObject launcherDisplay = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/DroneWeapons/DisplayDroneWeaponLauncher.prefab").WaitForCompletion();
        private readonly Material mechaMat = Addressables.LoadAssetAsync<Material>("RoR2/DLC1/DroneCommander/matDroneCommander.mat").WaitForCompletion();

        // what the fuck?
        private readonly GameObject hat = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Bandit2/mdlBandit2.fbx").WaitForCompletion().transform
            .GetChild(4)
            .GetChild(2)
            .GetChild(0)
            .GetChild(6)
            .GetChild(0)
            .GetChild(2)
            .GetChild(0)
            .gameObject;

        public static MechaLemur instance { get; private set; }

        public static void Init()
        {
            if (instance != null)
                return;

            instance = new MechaLemur();
        }

        public MechaLemur()
        {
            chaingunDisplay.AddComponent<NetworkIdentity>();
            launcherDisplay.AddComponent<NetworkIdentity>();
            DevotionTweaks.instance.bodyPrefab.GetComponent<CharacterBody>().bodyFlags |= CharacterBody.BodyFlags.Mechanical;
            DevotionTweaks.instance.bigBodyPrefab.GetComponent<CharacterBody>().bodyFlags |= CharacterBody.BodyFlags.Mechanical;

            On.RoR2.CharacterBody.OnInventoryChanged += CharacterBody_OnInventoryChanged;
            On.RoR2.CharacterModel.UpdateOverlays += CharacterModel_UpdateOverlays;
        }

        private void CharacterModel_UpdateOverlays(On.RoR2.CharacterModel.orig_UpdateOverlays orig, CharacterModel self)
        {
            orig.Invoke(self);
            if (Utils.IsDevoted(self.body) && self.activeOverlayCount < CharacterModel.maxOverlays
                && self.body.inventory.GetItemCount(DLC1Content.Items.DroneWeaponsBoost) > 0)
            {
                self.currentOverlays[self.activeOverlayCount++] = this.mechaMat;
            }
        }

        private void CharacterBody_OnInventoryChanged(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            orig.Invoke(self);
            if (Utils.IsDevoted(self) && self.inventory.GetItemCount(DLC1Content.Items.DroneWeaponsBoost) > 0)
            {
                var model = self.modelLocator.modelTransform.GetChild(0).GetChild(0).GetChild(0).GetChild(0);
                var hat = UE.Object.Instantiate(this.hat, model.GetChild(0).GetChild(0));
                var hatTransform = hat.transform;
                hatTransform.localScale = new Vector3(5f, 5f, 5f);
                hatTransform.localPosition = new Vector3(0f, -2.25f, 2.25f);
                hatTransform.eulerAngles = new Vector3(0f, 90f, 0f);
                hatTransform.GetChild(0).GetComponent<MeshRenderer>().material = this.mechaMat;

                NetworkServer.Spawn(hat);

                if (!model.Find("DisplayDroneWeaponMinigun(Clone)"))
                {
                    var gun = UE.Object.Instantiate(this.chaingunDisplay, model.GetChild(0));
                    gun.transform.localPosition = new Vector3(0f, 1.7f, -0.5f);
                    gun.transform.rotation *= Quaternion.Euler(0f, 90f, 90f);
                    NetworkServer.Spawn(gun);
                }

                if (!model.Find("DisplayDroneWeaponLauncher(Clone)"))
                {
                    GameObject bigGun = UE.Object.Instantiate(this.launcherDisplay, model.GetChild(0).GetChild(0));
                    bigGun.transform.localPosition = new Vector3(1f, -0.75f, 0.25f);
                    NetworkServer.Spawn(bigGun);
                }
            }
        }
    }
}
