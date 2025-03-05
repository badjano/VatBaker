using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace VAT.Animation.VAT
{
    public class VATAnimator : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Shader shader;
        [SerializeField] private Texture2D mainTexture;

        [SerializeField] private VATAnimatorData vatAnimator;

        private string _currentAnimationName;
        private VATAnimation _currentAnimation;
        private float _time;
        private float _timeSpeed = 1;
        

        private static readonly int MainTex = Shader.PropertyToID("_MainTex");

        private static readonly int VatAnimationBlend = Shader.PropertyToID("_VatAnimationBlend");

        private static readonly int VatPositionTex = Shader.PropertyToID("_VatPositionTex");
        private static readonly int VatNormalTex = Shader.PropertyToID("_VatNormalTex");
        private static readonly int VatAnimationSpeed = Shader.PropertyToID("_VatAnimationSpeed");
        private static readonly int AnimationTimeOffset = Shader.PropertyToID("_AnimationTimeOffset");
        private static readonly int VatAnimFps = Shader.PropertyToID("_VatAnimFps");

        private static readonly int VatPositionTex2 = Shader.PropertyToID("_VatPositionTex2");
        private static readonly int VatNormalTex2 = Shader.PropertyToID("_VatNormalTex2");
        private static readonly int VatAnimationSpeed2 = Shader.PropertyToID("_VatAnimationSpeed2");
        private static readonly int AnimationTimeOffset2 = Shader.PropertyToID("_AnimationTimeOffset2");

        private static readonly int Loop = Shader.PropertyToID("_Loop");
        private static readonly int AnimationTime = Shader.PropertyToID("_VatAnimationTime");
        
        private Material _material;
        private MeshRenderer _meshRenderer;


        private void Start()
        {
            var uid = Guid.NewGuid().ToString();
            name = $"VAT Unit {uid}";
            var skinnedMesh = target.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMesh != null)
            {
                target.gameObject.AddComponent<MeshFilter>().sharedMesh = skinnedMesh.sharedMesh;
                Destroy(skinnedMesh);
                _meshRenderer = target.gameObject.AddComponent<MeshRenderer>();
                _material = new Material(shader);
                _material.name = $"{name}_material";
                _material.enableInstancing = true;
                _material.SetTexture(MainTex, mainTexture);
                _material.SetFloat(VatAnimationSpeed, 1);
                _material.SetFloat(VatAnimationSpeed2, 1);
                _meshRenderer.sharedMaterial = _material;
                _time = Random.Range(0f, 100f);
            } else
            {
                _meshRenderer = target.gameObject.GetComponent<MeshRenderer>();
                _material = _meshRenderer.sharedMaterial;
            }

            SetAnimation("idle", true);
        }

        public void SendWrappedMessage(string method, string key = null, object value = null)
        {
            switch (method)
            {
                case "SetBool":
                    SetBool(key, value != null && (bool)value);
                    break;
                case "SetFloat":
                    SetFloat(key, (float)value);
                    break;
                case "SetLayerWeight":
                    SetLayerWeight(Convert.ToInt32(key), (int)value);
                    break;
                case "SetTrigger":
                    SetTrigger(key);
                    break;
                case "ResetTrigger":
                    ResetTrigger(key);
                    break;
                default:
                    Debug.LogWarning("Unknown method: " + method);
                    break;
            }
        }

        private void Update()
        {
            _time += Time.deltaTime * _timeSpeed;
            _meshRenderer.sharedMaterial.SetFloat(AnimationTime, _time);
        }

        private void SetAnimation(string animation, bool value)
        {
            if (_currentAnimationName == animation && !value)
            {
                SetAnimation("idle", true);
            }
            else if (value && _currentAnimationName != animation && !string.IsNullOrEmpty(animation))
            {
                bool needsBlending = false;
                if (!string.IsNullOrEmpty(_currentAnimationName) && _currentAnimation != null)
                {
                    _material.SetFloat(AnimationTimeOffset2, _material.GetFloat(AnimationTimeOffset));
                    _material.SetTexture(VatPositionTex2, _currentAnimation.PositionTexture);
                    _material.SetTexture(VatNormalTex2, _currentAnimation.NormalTexture);
                    needsBlending = true;
                }

                var anim = vatAnimator.GetAnimation(animation);
                _material.SetInt(Loop, anim.Loop ? 1 : 0);
                _material.SetFloat(AnimationTimeOffset, -_time);
                _material.SetTexture(VatPositionTex, anim.PositionTexture);
                _material.SetTexture(VatNormalTex, anim.NormalTexture);
                _material.SetFloat(VatAnimFps, anim.FPS);
                _currentAnimation = anim;
                _currentAnimationName = animation;
                _material.SetFloat(VatAnimationBlend, needsBlending ? 1f : 0f);
                if (needsBlending)
                {
                    StartCoroutine(nameof(FadeBlend));
                }
            }
        }

        IEnumerator FadeBlend()
        {
            var blend = 1f;
            while (blend > 0)
            {
                blend -= Time.deltaTime * 5;
                _material.SetFloat(VatAnimationBlend, blend);
                yield return null;
            }

            _material.SetFloat(VatAnimationBlend, 0);
        }

        private void SetTrigger(string key)
        {
        }

        private void ResetTrigger(string key)
        {
        }

        private void SetLayerWeight(int layer, int value)
        {
        }

        private void SetFloat(string key, float value)
        {
            switch (key)
            {
                case "navigationSpeed":
                case "attackSpeed":
                    _timeSpeed = value;
                    break;
            }
        }

        private void SetBool(string key, bool value)
        {
            SetAnimation(key, value);
        }
    }
}