﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Liminal.SDK.Editor.Build;
using Liminal.SDK.VR.Avatars;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.VR;
using UnityEngine.XR;

namespace Liminal.SDK.Build
{
    /// <summary>
    /// The window to export and build the limapp
    /// </summary>
    public class IssueWindow : BaseWindowDrawer
    {

        public override void Draw(BuildWindowConfig config)
        {
            EditorGUILayout.BeginVertical("Box");
            {
                EditorGUIHelper.DrawTitle("Issue Resolution");
                EditorGUILayout.LabelField("This window will help you Identify and resolve known issues and edge cases");
                EditorGUILayout.TextArea("", GUI.skin.horizontalSlider);

                GetSceneGameObjects();

                GUILayout.Space(10);
                
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

                EditorStyles.label.wordWrap = true;

                CheckUnityEditor();
                CheckRendering();
                CheckVRAvatar();
                CheckTagsAndLayers();
                CheckIncompatibility();

                EditorGUILayout.EndScrollView();
                GUILayout.Space(EditorGUIUtility.singleLineHeight);

                if(GUILayout.Button("View Wiki"))
                    Application.OpenURL("https://github.com/LiminalVR/DeveloperWiki/wiki/Requirements-&-Optimisation");

                if (!_showRenderingSection && !_showRenderingSection && !_showIncompatibilitySection && !_showEditorSection)
                    EditorGUIHelper.DrawTitle("No Outstanding Issues");

                GUILayout.FlexibleSpace();
               
                EditorGUILayout.EndVertical();
            }
        }

        private void GetSceneGameObjects()
        {
            Scene scene = SceneManager.GetActiveScene();
            scene.GetRootGameObjects(_sceneGameObjects);
        }

        private void CheckUnityEditor()
        {
            if (_showEditorSection)
                EditorGUIHelper.DrawTitle("Unity Editor");

            if (!Application.unityVersion.Contains("2019.1.10f1"))
            {
                _showEditorSection = true;
                EditorGUILayout.LabelField("Please Ensure You Are Using Unity 2019.1.10f1 As Your Development Environment");
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                return;
            }

            _showEditorSection = false;
        }

        private void CheckRendering()
        {
            if (_showRenderingSection)
                EditorGUIHelper.DrawTitle("Rendering");

            if (!PlayerSettings.virtualRealitySupported)
            {
                _showRenderingSection = true;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Virtual Reality Must Be Supported");

                if (GUILayout.Button("Enable VR Support"))
                    PlayerSettings.virtualRealitySupported = true;

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                return;
            }

            if (PlayerSettings.stereoRenderingPath != StereoRenderingPath.SinglePass)
            {
                _showRenderingSection = true;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Stereo Rendering Mode Must be Set To Single Pass");

                if (GUILayout.Button("Set To Single Pass"))
                    PlayerSettings.stereoRenderingPath = StereoRenderingPath.SinglePass;

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                return;
            }

            _showRenderingSection = false;
        }

        private void CheckVRAvatar()
        {
            VRAvatar avatar = null;

            foreach (var item in _sceneGameObjects)
            {
                if(item.GetComponentInChildren<VRAvatar>())
                {
                    avatar = item.GetComponentInChildren<VRAvatar>();
                    break;
                }
            }

            if (_showVRAvatarSection)
                EditorGUIHelper.DrawTitle("VR Avatar");

            if (avatar == null)
            {
                _showVRAvatarSection = true;
                EditorGUILayout.LabelField("Scene Must Contain A VR Avatar");
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                return;
            }

            if (avatar.Head.Transform.localEulerAngles != Vector3.zero)
            {
                _showVRAvatarSection = true;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("VR Avatar Head Rotation Must be Zeroed");

                if (GUILayout.Button("Set Head Rotation To 0, 0, 0"))
                    avatar.Head.Transform.localEulerAngles = Vector3.zero;

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                return;
            }

            var eyes = new List<Camera>
            {
                avatar.Head.CenterEyeCamera,
                avatar.Head.LeftEyeCamera,
                avatar.Head.RightEyeCamera
            };

            var eyeRotWrong = false;
            var eyePosWrong = false;

            foreach (var item in eyes)
            {
                if (item.transform.localEulerAngles != Vector3.zero)
                    eyeRotWrong = true;

                if (item.transform.localPosition != Vector3.zero)
                    eyePosWrong = true;
            }

            if (eyeRotWrong || eyePosWrong)
            {
                _showVRAvatarSection = true;

                if (eyeRotWrong)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Eye Local Rotation Must be Zeroed");

                    if (GUILayout.Button("Set Local Rotation To 0, 0, 0"))
                        eyes.ForEach(x => x.transform.localEulerAngles = Vector3.zero);

                    EditorGUILayout.EndHorizontal();
                }

                if(eyePosWrong)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Eye Local Position Must be Zeroed");

                    if (GUILayout.Button("Set Local Position To 0, 0, 0"))
                        eyes.ForEach(x => x.transform.localPosition = Vector3.zero);

                    EditorGUILayout.EndHorizontal();
                }

                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                return;
            }

            _showVRAvatarSection = false;
        }

        private void CheckTagsAndLayers()
        {
            var allTags = UnityEditorInternal.InternalEditorUtility.tags;
            var allLayers = UnityEditorInternal.InternalEditorUtility.layers;

            if (allTags.Count() > 7 || allLayers.Count() > 5)
                EditorGUIHelper.DrawTitle("Tags And Layers");
            else
                return;

            if (allTags.Count() > 7)
            {
                EditorGUILayout.LabelField($"You Have {allTags.Count() - 7} Custom Tags In Your Tag List. Do Not Use Tags Unless They Are Assigned At Runtime.");
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
            }

            if (allLayers.Count() > 5)
                EditorGUILayout.LabelField($"You Have {allLayers.Count() - 5} Custom Layers In Your Layer List. It Is Not Recommended To Rely On Layers, " +
                    $"As Layers Other Than The Default Ones Are Not Carried Through In A Limapp And Will Returns Null References. If You Use Layers, " +
                    $"Make Sure To Refer To Their Number And Not Their String Name.");

            GUILayout.Space(EditorGUIUtility.singleLineHeight);
        }

        private void CheckIncompatibility()
        {
            if(_showIncompatibilitySection)
                EditorGUIHelper.DrawTitle("Known Incompatibilities");

            var incompatiblePackages = new List<string>();

            foreach (var item in _sceneGameObjects)
            {
                var scripts = item.GetComponentsInChildren<MonoBehaviour>();
                foreach (var script in scripts)
                {
                    if (script == null)
                        continue;

                    Type type = script.GetType();

                    if (type.Name.ToLower().Contains("postprocessing"))
                    {
                        if (!incompatiblePackages.Contains("Unity Post-Processing"))
                            incompatiblePackages.Add("Unity Post-Processing");
                    }

                    if (type.Name.ToLower().Contains("curvy"))
                    {
                        if (!incompatiblePackages.Contains("Curvy"))
                            incompatiblePackages.Add("Curvy");
                    }
                }
            }

            if (incompatiblePackages.Count > 0)
            {
                _showIncompatibilitySection = true;

                EditorGUILayout.LabelField($"The Following Packages Are Known To Be Incompatible With The Liminal SDK");
                EditorGUI.indentLevel++;

                foreach (var item in incompatiblePackages)
                {
                    EditorGUILayout.LabelField($"* {item}");
                }

                EditorGUI.indentLevel--;

                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                EditorGUILayout.LabelField($"Please Remove These Packages Before Building");

                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                return;
            }


            _showIncompatibilitySection = false;
        }

        private void DetectMethods()
        {
            List<MethodInfo> methods = new List<MethodInfo>();

            // this finds out if methods are included in scripts, but I need to change it to find if methods are being called in scripts

            foreach (var item in _sceneGameObjects)
            {
                var scripts = item.GetComponentsInChildren<MonoBehaviour>();

                foreach (var script in scripts)
                {
                    if (script == null)
                        continue;

                    Type type = script.GetType();
 
                    if (type.GetMethod("test") != null)
                        methods.Add(type.GetMethod("test"));

                    if (type.GetMethod("temp") != null)
                        methods.Add(type.GetMethod("temp"));
                }  
            }

            foreach (var item in methods)
            {
                Debug.Log(item);
            }
        }

        bool _showRenderingSection;
        bool _showVRAvatarSection;
        bool _showIncompatibilitySection;
        bool _showEditorSection; 
        List<GameObject> _sceneGameObjects = new List<GameObject>();
        Vector2 _scrollPos;
    }
}