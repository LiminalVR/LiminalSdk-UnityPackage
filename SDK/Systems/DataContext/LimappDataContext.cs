using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Liminal.Shared
{
    public static class LimappDataContext
    {
        public static int ExperienceId;
        public static string DataPath => $"{Application.persistentDataPath}/Data/{ExperienceId}";
        public static string RuntimeSettingsPath => $"{DataPath}/runtimeSettings.json";

        public static LimappRuntimeSettings Load()
        {
            if (!File.Exists(RuntimeSettingsPath))
            {
                Debug.LogError("No Runtime Settings Json to load, creating a new one. This must be done by the platform. If you're in a limapp project, ignore this.");
                CreateData();
            }

            var settingsFile = File.ReadAllText(RuntimeSettingsPath);
            var settings = JsonConvert.DeserializeObject<LimappRuntimeSettings>(settingsFile);
            return settings;
        }

        public static void CreateData()
        {
            var settings = new LimappRuntimeSettings
            {
                Features = new Dictionary<string, object>()
                {
                    {LimappRuntimeSettings.RuntimeDurationKey, TimeSpan.FromSeconds(600)},
                },
                Tags = new Dictionary<string, string>()

            };

            CreateData(settings);
        }

        public static void CreateData(LimappRuntimeSettings settings)
        {
            var settingsJson = JsonConvert.SerializeObject(settings, Formatting.Indented);

            if (!Directory.Exists(DataPath))
                Directory.CreateDirectory(DataPath);

            File.WriteAllText(RuntimeSettingsPath, settingsJson);
        }
    }

    /// <summary>
    /// The runtime settings is created and managed by the Platform App.
    /// It will go into persistent data path / data / {experienceId} / runtimeSettings.json
    /// </summary>
    public class LimappRuntimeSettings
    {
        public const string RuntimeDurationKey = "RuntimeDuration";
        public Dictionary<string, object> Features;
        public Dictionary<string, string> Tags;

        public void SetRuntimeTag(string key, string value)
        {
            if (!Tags.ContainsKey(key))
            {
                Tags.Add(key, value);
            }
            else
            {
                Tags[key] = value;
            }
        }

        public string GetRuntimeTag(string key)
        {
            if (Tags.TryGetValue(key, out var tag))
            {
                return tag;
            }

            return "";
        }

        public void SetRuntimeDuration(object value)
        {
            Features[RuntimeDurationKey] = value;
        }

        public LimappRuntimeDuration GetRuntimeDuration()
        {
            var limappDuration = new LimappRuntimeDuration();
            if (Features.TryGetValue(RuntimeDurationKey, out var duration))
            {
                if (TimeSpan.TryParse((string)duration, out var timeSpan))
                    limappDuration.TimeSpan = timeSpan;
                else
                    limappDuration.Unlimited = true;
            }

            return limappDuration;
        }
    }

    public class LimappRuntimeDuration
    {
        public TimeSpan TimeSpan = TimeSpan.FromSeconds(600);
        public bool Unlimited;
    }

#if UNITY_EDITOR

    public static class LimappDataMenus
    {
        [UnityEditor.MenuItem("Liminal/Limapp Data/Open Directory", false)]
        public static void OpenDataDirectory()
        {
            // explorer doesn't like front slashes
            var directoryPath = LimappDataContext.DataPath.Replace(@"/", @"\");
            System.Diagnostics.Process.Start("explorer.exe", "/select," + directoryPath);
        }

        [UnityEditor.MenuItem("Liminal/Limapp Data/Create", false)]
        public static void CreateData()
        {
            var appManifest = Liminal.SDK.Editor.Build.AppBuilder.ReadAppManifest();
            LimappDataContext.ExperienceId = appManifest.Id;
            LimappDataContext.CreateData();
        }
    }
#endif
}
