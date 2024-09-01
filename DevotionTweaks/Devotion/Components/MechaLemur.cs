using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine;
using UE = UnityEngine;
using RoR2;

namespace LemurFusion.Devotion.Components
{
    public class MechaLemur
    {
        // what the fuck?
        private static GameObject hatDisplay, chaingunDisplay, launcherDisplay;
        private static Material mechaMat;

        public static MechaLemur instance { get; private set; }

        public static void Init()
        {
            if (instance != null)
                return;

            instance = new MechaLemur();
        }

        public MechaLemur()
        {
            mechaMat = Addressables.LoadAssetAsync<Material>("RoR2/DLC1/DroneCommander/matDroneCommander.mat").WaitForCompletion();
            chaingunDisplay = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/DroneWeapons/DisplayDroneWeaponMinigun.prefab").WaitForCompletion();
            launcherDisplay = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/DroneWeapons/DisplayDroneWeaponLauncher.prefab").WaitForCompletion();
            hatDisplay = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Bandit2/mdlBandit2.fbx").WaitForCompletion().transform
            .GetChild(4)
            .GetChild(2)
            .GetChild(0)
            .GetChild(6)
            .GetChild(0)
            .GetChild(2)
            .GetChild(0)
            .gameObject;

            hatDisplay.AddComponent<NetworkIdentity>();
            chaingunDisplay.AddComponent<NetworkIdentity>();
            launcherDisplay.AddComponent<NetworkIdentity>();

            On.RoR2.CharacterModel.UpdateOverlays += CharacterModel_UpdateOverlays;
        }

        private void CharacterModel_UpdateOverlays(On.RoR2.CharacterModel.orig_UpdateOverlays orig, CharacterModel self)
        {
            orig.Invoke(self);
            if (Utils.IsDevoted(self.body) && self.activeOverlayCount < CharacterModel.maxOverlays)
              // && self.body.inventory.GetItemCount(DLC1Content.Items.DroneWeaponsBoost) > 0)
            {
                self.currentOverlays[self.activeOverlayCount++] = mechaMat;
            }
        }

        public void AutoBotsRollOut(Transform baseTransform)
        {
            if (!baseTransform || !NetworkServer.active)
            {
                LemurFusionPlugin.LogError("EnablingNOTHING FUCK");
                return;
            }
            else
            {

                LemurFusionPlugin.LogError("Enabling ROLLOUT");
            }

            var mesh = baseTransform.GetComponentInChildren<SkinnedMeshRenderer>();
            if (mesh && mechaMat)
            {
                mesh.material = mechaMat;
                LemurFusionPlugin.LogError("Enabling mechamat");
            }

            var loc = baseTransform.GetComponent<ChildLocator>();
            if (!loc)
            {
                LemurFusionPlugin.LogError("No cchild loc???");
                return;
            }
            else
            {

                LemurFusionPlugin.LogError("loc FOUNDYYEYE");
            }
            var head = loc.FindChild("Head");
            if (head && hatDisplay)
            {
                LemurFusionPlugin.LogError("SPAWQNNNNN HAT");
                var hat = UE.Object.Instantiate(hatDisplay, head);
                /*var hatTransform = hat.transform;
                hatTransform.localScale = new Vector3(5f, 5f, 5f);
                hatTransform.localPosition = new Vector3(0f, -2.25f, 2.25f);
                hatTransform.eulerAngles = new Vector3(0f, 90f, 0f);*/
                NetworkServer.Spawn(hat);
            }
            else
            {

                LemurFusionPlugin.LogError("so no head");
            }
            var chest = loc.FindChild("Chest");
            if (!chest)
            {
                LemurFusionPlugin.LogError("SO NO CHEST mechamat");
                return;
            }
            var shoulder = chest.Find("shoulder.l");

            if (shoulder && !shoulder.Find("DisplayDroneWeaponMinigun(Clone)"))
            {
                LemurFusionPlugin.LogError("SOPAWN LESHOULDER mechamat");
                var gun = UE.Object.Instantiate(chaingunDisplay, shoulder);
                //gun.transform.localPosition = new Vector3(0f, 1.7f, -0.5f);
                //gun.transform.rotation *= Quaternion.Euler(0f, 90f, 90f);
                NetworkServer.Spawn(gun);
            }

            shoulder = chest.Find("shoulder.r");
            if (shoulder && !shoulder.Find("DisplayDroneWeaponLauncher(Clone)"))
            {
                LemurFusionPlugin.LogError("SOPAWN rerightiighty mechamat");
                var bigGun = UE.Object.Instantiate(launcherDisplay, shoulder);
                //bigGun.transform.localPosition = new Vector3(1f, -0.75f, 0.25f);
                NetworkServer.Spawn(bigGun);
            }
        }
    }
}
