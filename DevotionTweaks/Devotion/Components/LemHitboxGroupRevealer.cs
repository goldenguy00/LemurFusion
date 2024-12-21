using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using R2API;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace LemurFusion.Devotion.Components
{
    public class LemHitboxGroupRevealer : MonoBehaviour
    {
        public static GameObject prefab;
        public static void Init()
        {
            prefab = PrefabAPI.InstantiateClone(new GameObject(), "RevealerPrefab", false);


            prefab.AddComponent<MeshFilter>().sharedMesh = Addressables.LoadAssetAsync<Mesh>("Decalicious/DecalCube.asset").WaitForCompletion();
            prefab.AddComponent<MeshRenderer>().material = new Material(Addressables.LoadAssetAsync<Material>("RoR2/Base/Engi/matBlueprintsOk.mat").WaitForCompletion());
            prefab.GetComponent<MeshRenderer>().enabled = false;
            prefab.AddComponent<LemHitboxRevealer>();

            prefab.transform.localPosition = Vector3.zero;
            prefab.transform.localRotation = Quaternion.identity;
            prefab.transform.localScale = Vector3.one;
        }

        public LemHitboxRevealer[] _revealers = [];

        private bool _revealed;

        public void Start()
        {
            if (this.TryGetComponent<HitBoxGroup>(out var hitboxGroup))
            {
                _revealers = new LemHitboxRevealer[hitboxGroup.hitBoxes.Length];
                for (int i = 0; i < hitboxGroup.hitBoxes.Length; i++)
                {
                    _revealers[i] = GameObject.Instantiate(prefab, hitboxGroup.hitBoxes[i].transform).GetComponent<LemHitboxRevealer>();
                }
            }
            else if (this.TryGetComponent<ProjectileController>(out var ctrl) && ctrl.myColliders.Length > 0)
            {
                _revealers = new LemHitboxRevealer[ctrl.myColliders.Length];
                for (int i = 0; i < _revealers.Length; i++)
                {
                    _revealers[i] = GameObject.Instantiate(prefab, ctrl.myColliders[i].transform).GetComponent<LemHitboxRevealer>();
                }
            }
            else if (this.TryGetComponent<ProjectileExplosion>(out var impact))
            {
                _revealers = [GameObject.Instantiate(prefab, this.transform).GetComponent<LemHitboxRevealer>()];
                _revealers[0].transform.localScale = Vector3.one * impact.blastRadius;
            }
            else
            {
                _revealers = [GameObject.Instantiate(prefab, this.transform).GetComponent<LemHitboxRevealer>()];
            }

            Reveal(true);
        }

        public void Reveal(bool active)
        {
            _revealed = active;
            for (int i = 0; i < _revealers.Length; i++)
            {
                if (_revealers[i])
                    _revealers[i].enabled = active;
            }
        }

        void OnDestroy()
        {
            Reveal(false);

            for (int i = 0; i < _revealers.Length; i++)
            {
                if (_revealers[i])
                    Destroy(_revealers[i].gameObject);
            }
        }
    }
}
