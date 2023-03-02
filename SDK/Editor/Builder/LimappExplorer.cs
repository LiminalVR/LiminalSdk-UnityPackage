﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Experimental;
using Liminal.SDK.Editor.Build;
using Liminal.SDK.Serialization;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace Liminal.SDK.Build
{
    public class LimappExplorer : BaseWindowDrawer
    {
        public static string InputDirectory = "C:/Users/ticoc/Documents/Liminal/Limapps/Standalone";
        public static string OutputDirectory = "C:/Users/ticoc/Documents/Liminal/Limapps-new-output/Standalone";
        public static string PlatformAppDirectory;


        public static HashSet<int> ProcessedFile = new HashSet<int>();
        public static bool IsAndroid = false;
        public static string PlatformName => IsAndroid ? "Android" : "Standalone";

        public const string LimappInputPathKey = "limappInputDirectory";
        public const string LimappOutputPathKey = "limappOutputDirectory";
        public const string PlatformAppPathKey = "PlatformAppDirectory";

        public override void OnEnabled()
        {
            InputDirectory = EditorPrefs.HasKey(LimappInputPathKey) ? EditorPrefs.GetString(LimappInputPathKey) : GetDefaultOutputPath;
            OutputDirectory = EditorPrefs.HasKey(LimappOutputPathKey) ? EditorPrefs.GetString(LimappOutputPathKey) : GetDefaultOutputPath;

            if (EditorPrefs.HasKey(PlatformAppPathKey))
                PlatformAppDirectory = EditorPrefs.GetString(PlatformAppPathKey);

            base.OnEnabled();
        }

        public override void Draw(BuildWindowConfig config)
        {
            EditorGUIHelper.DrawTitle("Migration Window");

            EditorGUILayout.LabelField("The migration window lets you convert limapp v1 to limapp v2. This is only necessary for Quest.");
            EditorGUILayout.LabelField("This process will extract .limapp into raw data and put it into a folder and then zip it.");
            EditorGUILayout.LabelField("The DLL need to be copied into the Platform project and added to the link.xml");

            GUILayout.Space(20);

            EditorGUILayout.LabelField("Select limapp folder");
            DrawDirectorySelection(ref InputDirectory, "Input Directory");
            DrawDirectorySelection(ref OutputDirectory, "Output Directory");
            DrawDirectorySelection(ref PlatformAppDirectory, "Platform Assets Folder");

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Save Paths", GUILayout.Width(110)))
            {
                EditorPrefs.SetString(LimappInputPathKey, InputDirectory);
                EditorPrefs.SetString(LimappOutputPathKey, OutputDirectory);
                EditorPrefs.SetString(PlatformAppPathKey, PlatformAppDirectory);
            }
            GUILayout.Space(20);
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Convert"))
            {
                ProcessedFile.Clear();

                var limapps = Directory.GetFiles(InputDirectory);
                EditorCoroutineUtility.StartCoroutineOwnerless(ExtractAll(limapps));
            }

            if (GUILayout.Button("Sync with Platform"))
            {
                SyncWithPlatform();
            }


            IEnumerator DownloadAll()
            {
                var getExperiences = "https://api.liminalvr.com/api/experiences/all";
                using (var www = UnityWebRequest.Get(getExperiences))
                {
                    yield return www.SendWebRequest();
                    var response = www.downloadHandler.text;
                    Debug.Log(response);

                    var experienceCollection = JsonConvert.DeserializeObject<ExperienceCollection>(response);
                    Debug.Log(experienceCollection.Experiences.Count);

                    foreach (var experience in experienceCollection.Experiences)
                    {
                        if (!experience.Approved || !experience.Enabled)
                            continue;

                        var experienceGuid = IsAndroid ? experience.LimappGearVrGuid : experience.LimappEmulatorGuid;
                        var getResource = $"https://api.liminalvr.com/api/resource/guid/{experienceGuid}";
                        using (var resourceWww = UnityWebRequest.Get(getResource))
                        {
                            yield return resourceWww.SendWebRequest();

                            if (string.IsNullOrEmpty(resourceWww.downloadHandler.text))
                            {
                                Debug.Log("wtf");
                                continue;
                            }

                            var limappResource = JsonConvert.DeserializeObject<Resource>(resourceWww.downloadHandler.text);
                            Debug.Log(limappResource.Uri);
                            // Download these!

                            var mainPath = OutputDirectory;
                            yield return EditorCoroutineUtility.StartCoroutineOwnerless(UnzipTest.Download(limappResource.Uri, experience.Id, mainPath));
                        }
                    }
                }
            }

            IEnumerator ExtractAll(string[] paths)
            {
                var limappPaths = paths.Where(x => Path.GetExtension(x) == ".limapp").ToArray();
                for (var i = 0; i < limappPaths.Length; i++)
                {
                    var limappPath = limappPaths[i];

                    if (Path.GetExtension(limappPath) != ".limapp")
                        continue;

                    EditorUtility.DisplayProgressBar("Extracting...", limappPath, i / (float)limappPath.Length);

                    Debug.Log($"Processing: {limappPath}");
                    yield return EditorCoroutineUtility.StartCoroutineOwnerless(ExtractPack(limappPath, PlatformName, true));
                }

                yield return new EditorWaitForSeconds(1);
                RenameDLL(OutputDirectory);

                EditorUtility.ClearProgressBar();
            }
        }


        //  path = Android folder?
        void RenameDLL(string path)
        {
            var stringBuilder = new StringBuilder();

            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (file.Contains("App000") && file.Contains(","))
                {
                    stringBuilder.AppendLine($"<assembly fullname=\"{fileName}\" preserve=\"all\"/>");

                    var newFullPath = $"{path}/{GetDllNameWithoutAssembly(file)}.dll";

                    if (File.Exists(newFullPath))
                    {
                        Debug.Log($"{newFullPath} exists so it has been deleted.");
                        File.Delete(newFullPath);
                    }

                    File.Copy(file, newFullPath);

                    Debug.Log($"Success! Add to link.xml: \n {stringBuilder.ToString()}");

                    Process.Start(OutputDirectory);
                }
            }
        }

        string GetDllNameWithoutAssembly(string file)
        {
            var fileNames = file.Split(',');
            var fileNameWithoutAssembly = fileNames[0];
            var newFileName = Path.GetFileName(fileNameWithoutAssembly);
            return newFileName;
        }

        public static string GetDefaultOutputPath => Path.Combine(Application.dataPath, @"..\Limapp-output");

        public static void SyncWithPlatform()
        {
            // Copy the dll over
            var appManifest = AppBuilder.ReadAppManifest();
            var asmFolder = $"{GetDefaultOutputPath}/Android/{appManifest.Id}/assemblyFolder/";
            var dllPaths = Directory.GetFiles(asmFolder);
            var platformDllFolder = $"{PlatformAppDirectory}/App/Limapps";

            var dllName = "";
            foreach (var dllPath in dllPaths)
            {
                var fileName = Path.GetFileName(dllPath);
                if (fileName.Contains("App"))
                {
                    dllName = fileName.Split(',')[0];
                    fileName = dllName;
                    var dest = $"{platformDllFolder}/{fileName}.dll";
                    File.Copy(dllPath, dest, true);
                }
            }

            var linkerPath = $"{PlatformAppDirectory}/link.xml";
            var linkerText = File.ReadAllText(linkerPath);

            if (linkerText.Contains(dllName))
            {
                Debug.Log($"{dllName} already exist in linker file, no need to edit.");
            }
            else
            {
                var linkerLine = File.ReadAllLines(linkerPath).ToList();
                var newLinkerLine = $"<assembly fullname=\"{dllName}\" preserve=\"all\"/>";
                linkerLine.Insert(linkerLine.Count - 1, newLinkerLine);

                File.WriteAllLines(linkerPath, linkerLine);
            }

            var scriptPath = $"{PlatformAppDirectory}/App/Scripts/Server/AppServerController/AppServerExperiencesController.cs";
            var scriptLines = File.ReadAllLines(scriptPath);
            var scriptTexts = File.ReadAllText(scriptPath);

            if (!scriptTexts.Contains($",{appManifest.Id}"))
            {
                scriptLines[77] += $",{appManifest.Id}";
                File.WriteAllLines(scriptPath, scriptLines);
            }
            else
            {
                Debug.Log($"{appManifest.Id} already exist in script AppServerExperiencesController, no need to edit.");
            }
        }

        public static IEnumerator ExtractPack(string limappPath, string platformName, bool useSelectedOutputPath = false)
        {
            var appBytes = File.ReadAllBytes(limappPath);

            Debug.Log("Unpacking...");
            var unpacker = new AppUnpacker();
            unpacker.UnpackAsync(appBytes);

            yield return new WaitUntil(() => unpacker.IsDone);

            var fileName = Path.GetFileNameWithoutExtension(limappPath);

            // write all assemblies on disk
            var assmeblies = unpacker.Data.Assemblies;
            var outputPath = useSelectedOutputPath ? OutputDirectory : $"{GetDefaultOutputPath}/{platformName}";
            var appFolder = $"{outputPath}/{unpacker.Data.ApplicationId}";

            if (ProcessedFile.Contains(unpacker.Data.ApplicationId))
            {
                appFolder = $"{outputPath}/{unpacker.Data.ApplicationId}-{ProcessedFile.Count}";
                Debug.Log($"Multiple limapps of this id exist. {unpacker.Data.ApplicationId}");
            }

            var assemblyFolder = $"{appFolder}/assemblyFolder";

            if (Directory.Exists(appFolder))
                Directory.Delete(appFolder, true);

            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);

            if (!Directory.Exists(assemblyFolder))
                Directory.CreateDirectory(assemblyFolder);

            // Wait, in theory, I can rewrite the assembly to match ah, but that's not it.

            for (var i = 0; i < assmeblies.Count; i++)
            {
                var asmBytes = assmeblies[i];
                var asm = Assembly.Load(asmBytes);
              
                File.WriteAllBytes($"{assemblyFolder}/{asm.GetName().Name}.dll", asmBytes);
            }

            File.WriteAllBytes($"{appFolder}/appBundle", unpacker.Data.SceneBundle);

            var manifest = new AppManifest
            {
                ExtractedFrom = Path.GetFileName(limappPath),
                CreatedDate = DateTime.UtcNow.ToString()
            };

            var manifestJson = JsonConvert.SerializeObject(manifest);

            File.WriteAllText($"{appFolder}/manifest.json", manifestJson);

            // We are adding this to the procesesed file so that when we do this in batches, if there are multiple limapps, we'll get a message about it.
            ProcessedFile.Add(unpacker.Data.ApplicationId);

            Debug.Log("Done!");

            UnzipTest.ZipFolder(appFolder, $"{GetDefaultOutputPath}/{platformName}/{unpacker.Data.ApplicationId}.zip");
        }

        public class AppManifest
        {
            public string ExtractedFrom;
            public string CreatedDate;
        }

        public class ExperienceCollection
        {
            public List<Experience> Experiences;
        }

        public class Experience
        {
            public int Id;
            public string Name;
            public Guid LimappEmulatorGuid { get; set; }
            public Guid LimappGearVrGuid { get; set; }

            public bool Approved;
            public bool Enabled;
        }

        public class Resource
        {
            public string Uri;
        }

        public void DrawDirectorySelection(ref string directoryPath, string title)
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label(title, GUILayout.Width(Size.x * 0.15F));
                directoryPath = GUILayout.TextField(directoryPath, GUILayout.Width(Size.x * 0.7F));

                if (GUILayout.Button("...", GUILayout.Width(Size.x * 0.1F)))
                {
                    directoryPath = EditorUtility.OpenFolderPanel("Select a Folder", "", "");
                    directoryPath = DirectoryUtils.ReplaceBackWithForwardSlashes(directoryPath);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}