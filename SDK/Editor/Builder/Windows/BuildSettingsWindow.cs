using System;
using System.Collections.Generic;
using System.IO;
using Assets.Oculus.VR.Editor;
using UnityEditor;
using UnityEngine;
using System.IO.Compression;

namespace Liminal.SDK.Build
{
    /// <summary>
    /// A window for developers to configure and build for Limapps
    /// </summary>
    public class BuildSettingsWindow : EditorWindow
    {
        private const int _width = 550;
        private const int _height = 300;

        public static EditorWindow Window;
        public static Dictionary<BuildSettingMenus, BaseWindowDrawer> BuildSettingLookup = new Dictionary<BuildSettingMenus, BaseWindowDrawer>();

        private BuildSettingMenus _selectedMenu = BuildSettingMenus.Setup;
        private BuildWindowConfig _windowConfig = new BuildWindowConfig();
        private Vector2 _scroll;

        public int SelectedMenuIndex { get { return (int)_selectedMenu; } }

        [MenuItem("Liminal/Build Window")]
        public static void OpenBuildWindow()
        {
            Window = GetWindow(typeof(BuildSettingsWindow), false, "Build Settings");
            Window.minSize = new Vector2(_width, _height);
            Window.Show();
        }

        [MenuItem("Liminal/Update Package")]
        public static void RefreshPackage()
        {
            File.WriteAllText(UnityPackageManagerUtils.ManifestPath, UnityPackageManagerUtils.ManifestWithoutLock);
            AssetDatabase.Refresh();
        }


        private void OnEnable()
        {
            var fileExists = Directory.Exists(BuildWindowConsts.ConfigFolderPath) || File.Exists(BuildWindowConsts.BuildWindowConfigPath);

            if (fileExists)
            {
                if (File.Exists(BuildWindowConsts.BuildWindowConfigPath))
                {
                    var json = File.ReadAllText(BuildWindowConsts.BuildWindowConfigPath);
                    _windowConfig = JsonUtility.FromJson<BuildWindowConfig>(json);
                    AssetDatabase.Refresh();
                }
            }

            SetupFolderPaths();

            SetupMenuWindows();
            
            var activeWindow = BuildSettingLookup[_selectedMenu];
            activeWindow.OnEnabled();
        }

        private void OnGUI()
        {
            var values = (BuildSettingMenus[])Enum.GetValues(typeof(BuildSettingMenus));
            var tabs = new string[values.Length];
            for (var i = 0; i < values.Length; i++)
                tabs[i] = values[i].ToString();

            EditorGUI.BeginChangeCheck();
            _selectedMenu = (BuildSettingMenus) GUILayout.Toolbar(SelectedMenuIndex, tabs);
            var activeWindow = BuildSettingLookup[_selectedMenu];
            if(EditorGUI.EndChangeCheck())
            {
                activeWindow.OnEnabled();
                _scroll = Vector2.zero;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            activeWindow.Draw(_windowConfig);
            EditorGUILayout.EndScrollView();

            activeWindow.DrawFooter(_windowConfig);

            if (!Directory.Exists(BuildWindowConsts.ConfigFolderPath))
            {
                Directory.CreateDirectory(BuildWindowConsts.ConfigFolderPath);
            }

            // boolean true is used to format the resulting string for maximum readability. False would format it for minimum size.
            var configJson = JsonUtility.ToJson(_windowConfig, true);
            File.WriteAllText(BuildWindowConsts.BuildWindowConfigPath, configJson);


            foreach (var entry in BuildSettingLookup)
            {
                entry.Value.Size = position.size;
            }
        }

        private void SetupMenuWindows()
        {
            BuildSettingLookup.AddSafe(BuildSettingMenus.Build, new BuildWindow());
            BuildSettingLookup.AddSafe(BuildSettingMenus.Publishing, new PublishConfigurationWindow());
            BuildSettingLookup.AddSafe(BuildSettingMenus.Setup, new SetupWindow());
            BuildSettingLookup.AddSafe(BuildSettingMenus.Settings, new SettingsWindow());
        }

        private void SetupFolderPaths()
        {
            if (!Directory.Exists(BuildWindowConsts.BuildPath))
            {
                Directory.CreateDirectory(BuildWindowConsts.BuildPath);
            }

            AssetDatabase.Refresh();
        }
    }
}
