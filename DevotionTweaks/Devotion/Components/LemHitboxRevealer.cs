using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LemurFusion.Devotion.Components
{
    public class LemHitboxRevealer : MonoBehaviour
    {
        private MeshFilter meshFilter;
        public MeshRenderer renderer;

        #region hitbox

        private void Awake()
        {
            renderer = GetComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();
            renderer.enabled = false;
            this.enabled = false;
        }

        private void OnEnable()
        {
            var matProperties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(matProperties);

            var matColor = UnityEngine.Random.ColorHSV(0f, 1f, 0.5f, 0.5f, 0.3f, 0.6f, 0.3f, 0.6f);
            matProperties.SetColor("_Color", matColor);

            renderer.SetPropertyBlock(matProperties);
            renderer.enabled = true;
        }

        private void OnDisable()
        {
            if (renderer)
                renderer.enabled = false;
        }
        #endregion
    }
}
