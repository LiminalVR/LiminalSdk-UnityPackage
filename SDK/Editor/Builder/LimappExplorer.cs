using System;
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
        public static BuildTarget Target;
        public static string PlatformName => Target.ToString();

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

            EditorGUILayout.LabelField("Select Build Type");
            Target = (BuildTarget)EditorGUILayout.EnumPopup("Build Target: ", Target);

            GUILayout.Space(10);

            EditorGUILayout.LabelField("Select limapp folder");

            EditorGUI.BeginChangeCheck();

            DrawDirectorySelection(ref InputDirectory, "Input Directory");
            DrawDirectorySelection(ref OutputDirectory, "Output Directory");
            DrawDirectorySelection(ref PlatformAppDirectory, "Platform Assets Folder");

            //! Replace save paths with a change check?
            if(EditorGUI.EndChangeCheck()) {

                if(string.IsNullOrEmpty(OutputDirectory)) {
                    OutputDirectory = GetDefaultOutputPath;
                }

                EditorPrefs.SetString(LimappInputPathKey, InputDirectory);
                EditorPrefs.SetString(LimappOutputPathKey, OutputDirectory);
                EditorPrefs.SetString(PlatformAppPathKey, PlatformAppDirectory);
            }

            //
            // EditorGUILayout.BeginHorizontal();
            // GUILayout.FlexibleSpace();
            // if (GUILayout.Button("Save Paths", GUILayout.Width(110)))
            // {
            //     EditorPrefs.SetString(LimappInputPathKey, InputDirectory);
            //     EditorPrefs.SetString(LimappOutputPathKey, OutputDirectory);
            //     EditorPrefs.SetString(PlatformAppPathKey, PlatformAppDirectory);
            // }
            // GUILayout.Space(20);
            // EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Convert"))
            {
                ProcessedFile.Clear();

                if(IsValidLocation(InputDirectory)) {

                    var limapps = Directory.GetFiles(InputDirectory);

                    if(limapps!=null) {
                        EditorCoroutineUtility.StartCoroutineOwnerless(ExtractAll(limapps));
                    }
                    else {
                        EditorUtility.DisplayDialog("Conversion Error", "No files were found at the specificed input directory. Please provide a valid limapp.", "Okay");
                    }
                }
                else {
                    EditorUtility.DisplayDialog("Conversion Error", "Invalid input directory. Please provide a valid location.", "Okay");
                }
            }

            if (GUILayout.Button("Sync with Platform")) {
            
                if(IsValidLocation(PlatformAppDirectory)) {
                    SyncWithPlatform(EditorPrefs.HasKey(LimappOutputPathKey));
                }
                else {
                    EditorUtility.DisplayDialog("Sync Error", "Platform Assets Directory is not valid. Please add a valid location in order to Sync with the platform", "Okay");
                }
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

                        var experienceGuid = Target==BuildTarget.Android ? experience.LimappGearVrGuid : experience.LimappEmulatorGuid;
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
                    
                    bool useSelectedOutputPath = EditorPrefs.HasKey(LimappOutputPathKey);

                    yield return EditorCoroutineUtility.StartCoroutineOwnerless(ExtractPack(limappPath, PlatformName, useSelectedOutputPath));
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

        public static void SyncWithPlatform(bool useSelectedOutputPath = false) {
            // Copy the dll over
            var appManifest = AppBuilder.ReadAppManifest();
            var outputPath = useSelectedOutputPath ? OutputDirectory : GetDefaultOutputPath;
            var asmFolder = $"{outputPath}/{PlatformName}/{appManifest.Id}/assemblyFolder/";
            var dllPaths = Directory.GetFiles(asmFolder);
            var platformDllFolder = $"{PlatformAppDirectory}/App/Limapps";

            var dllName = "";
            foreach (var dllPath in dllPaths)
            {
                var fileName = Path.GetFileName(dllPath);
                if (fileName.Contains("App"))
                {
                    dllName = fileName.Split(',')[0];
                    dllName = Path.GetFileNameWithoutExtension(dllName);
                    var dest = $"{platformDllFolder}/{dllName}.dll";
                    Debug.Log(dest);
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
                var newLinkerLine = $"\t<assembly fullname=\"{dllName}\" preserve=\"all\"/>";
                linkerLine.Insert(linkerLine.Count - 1, newLinkerLine);

                File.WriteAllLines(linkerPath, linkerLine);
            }

            var scriptPath = $"{PlatformAppDirectory}/App/Scripts/Server/AppServerController/AppServerExperiencesController.cs";
            var scriptLines = File.ReadAllLines(scriptPath);
            var scriptTexts = File.ReadAllText(scriptPath);

            if (!scriptTexts.Contains($",{appManifest.Id}"))
            {
                // scriptLines[77] += $",{appManifest.Id}";
                // File.WriteAllLines(scriptPath, scriptLines);
                string sIdentifier = "//-->";
                string eIdentifier = "//<--";

                int sIndex = scriptTexts.IndexOf(sIdentifier);
                int eIndex = scriptTexts.IndexOf(eIdentifier, sIndex + sIdentifier.Length);

                string experienceIds = scriptTexts.Substring(sIndex + sIdentifier.Length, eIndex - sIndex - sIdentifier.Length);
                scriptTexts = scriptTexts.Replace(experienceIds, experienceIds + $",{appManifest.Id}");
                File.WriteAllText(scriptPath, scriptTexts);
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
            var outputPath = useSelectedOutputPath ? $"{OutputDirectory}/{platformName}" : $"{GetDefaultOutputPath}/{platformName}";
            var appFolder = $"{outputPath}/{unpacker.Data.ApplicationId}";

            if (!Directory.Exists(outputPath)) {
                Directory.CreateDirectory(appFolder);
            }

            if (ProcessedFile.Contains(unpacker.Data.ApplicationId))
            {
                appFolder = $"{outputPath}/{unpacker.Data.ApplicationId}-{ProcessedFile.Count}";
                Debug.Log($"Multiple limapps of this id exist. {unpacker.Data.ApplicationId}");
            }

            var assemblyFolder = $"{appFolder}/assemblyFolder";

            // USe this rob
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
                var asm = UnityEngine.Assemblies.CurrentAssemblies.LoadFromBytes(asmBytes);
              
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

            //UnzipTest.ZipFolder(appFolder, $"{GetDefaultOutputPath}/{platformName}/{unpacker.Data.ApplicationId}.zip");
            UnzipTest.ZipFolder(appFolder, $"{outputPath}/{unpacker.Data.ApplicationId}.zip");
            Debug.Log($"{GetDefaultOutputPath}/{platformName}/{unpacker.Data.ApplicationId}.zip");
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

        private bool IsValidLocation(string location) {
            return !string.IsNullOrEmpty(location) && Directory.Exists(location);
        }

        public enum BuildTarget {
            Android,
            Standalone
        }
    }
}