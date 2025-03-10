﻿using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace VatBaker
{
    public class VatBakerWindowForAnimation : EditorWindow
    {
        [MenuItem("Window/VatBaker")]
        static void ShowWindow() => GetWindow<VatBakerWindowForAnimation>();

        public GameObject gameObject;
        public Space space = Space.Self;
        public int textureWidth = 512;
        public int animationFps = 5;
        public Shader sampleShader;
        
        private SkinnedMeshRenderer _skin;
        private AnimationClip[] _clips;
        private Button _bakeButton;

        private void OnEnable()
        {
            if (Selection.activeObject is GameObject go
                && go.gameObject.GetComponentInChildren<SkinnedMeshRenderer>() != null)
            {
                gameObject = go;
            }
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;

            var gameObjectPropertyField = new PropertyField() {bindingPath = nameof(gameObject)};
            _bakeButton = new Button(Bake) {text = nameof(Bake)};
            
            gameObjectPropertyField.RegisterValueChangeCallback(OnGameObjectChanged);
            
            root.Add(gameObjectPropertyField);
            root.Add(new PropertyField() {bindingPath = nameof(space)});
            root.Add(new PropertyField() {bindingPath = nameof(textureWidth)});
            root.Add(new PropertyField() {bindingPath = nameof(animationFps)});
            root.Add(new PropertyField() {bindingPath = nameof(sampleShader)});
            root.Add(_bakeButton);

            root.Bind(new SerializedObject(this));
        }

        private void OnGameObjectChanged(SerializedPropertyChangeEvent evt)
        {
            var valid = UpdateSkinAndClipsFromGameObject();
            _bakeButton.SetEnabled(valid);
        }

        private bool UpdateSkinAndClipsFromGameObject()
        {
            if ( gameObject == null) return false;
        
            _skin = gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
            if (_skin == null)
            {
                EditorUtility.DisplayDialog("Invalid GameObject", NotFoundMessage(nameof(SkinnedMeshRenderer)), "Close");
                return false;
            }
            
            _clips = AnimationUtility.GetAnimationClips(gameObject);
            if (!_clips.Any())
            {
                EditorUtility.DisplayDialog("Invalid GameObject", NotFoundMessage(nameof(AnimationClip)), "Close");
                return false;
            }

            return true;

            string NotFoundMessage(string target) => $"{target} is not found at GameObject[{gameObject.name}].";
        }


        public void Bake()
        {
            foreach(var clip in _clips)
            {
                var objName = gameObject.name.Replace("_", "-");
                var clipName = clip.name.Replace("_", "-");
                var assetName = $"{objName}_{clipName}_{animationFps}_{textureWidth}";
                var (posTex, normTex) = VatBakerCore.BakeClip(assetName, gameObject, _skin, clip, textureWidth, animationFps, space);
                VatBakerCore.GenerateAssets(assetName, _skin, animationFps, clip.length, sampleShader, posTex, normTex);
            }
        }
    }
}