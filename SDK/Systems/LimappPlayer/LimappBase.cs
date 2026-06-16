using Liminal.Core.Fader;
using Liminal.SDK.Core;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Devices.GearVR.Avatar;
using Liminal.SDK.VR.Pointers;
using Liminal.Systems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using App.core;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.SpatialTracking;
using Liminal.Platform.Experimental.App.Experiences;
using Liminal.SDK.VR.Utils;
using App.Shared;

namespace App.Core
{
    public class LimappBase
    {
        public int Id;

        public AssetBundle CurrentBundle;
        public string SceneName;
        public List<Assembly> Assemblies;

        private IVRAvatar _avatarBefore;

        private DateTime _startTime;
        private float _loadProgress;
        public DateTime StartTime => _startTime;
        public float _gameStartTime;

        public ExperienceApp ExperienceApp { get; set; }

        public float Duration => Time.time - _gameStartTime;

        public TimeSpan RealtimeDuration
        {
            get
            {
                if (State == ELimappState.Running)
                    return ExperienceApp == null
                        ? TimeSpan.FromSeconds(0)
                        : DateTime.UtcNow.Subtract(_startTime);

                return EndTime.Subtract(_startTime);
            }
        }

        public DateTime EndTime { get; set; }

        public ELimappState State { get; set; } = ELimappState.Idle;
        public ELimappLoadState LoadState = ELimappLoadState.NotInProgress;

        public bool Completed;

        private List<Assembly> _assemblies = new List<Assembly>();

        public float LoadProgress
        {
            get
            {
                // 5 states
                // 0
                var stateCount = Enum.GetValues(typeof(ELimappLoadState)).Length;
                return _loadProgress = (float)LoadState + 1 / (float)stateCount;
            }
        }

        public AsyncOperation SceneLoadingOperation;

        public LimappBase(int id)
        {
            Id = id;
        }

        public static void LogMemory(string s)
        {
            var mem = System.GC.GetTotalMemory(true);
            Debug.Log($"memory {s}: {mem / 1e+6}");
        }

        public void Clean()
        {
            SceneLoadingOperation = null;
            _avatarBefore = null;
        }


        public void SetStartAndEndTime(DateTime unfinishedSessionStartTime, DateTime unfinishedSessionEndTime)
        {
            _startTime = unfinishedSessionStartTime;
            EndTime = unfinishedSessionEndTime;
        }

        public static string GetStreamingAssetFolderPath(int id)
        {
            return $"Limapps/{GetPlatformName()}/{id}";
        }

        // Prefer the .bundle name (extensionless files can be dropped from Android builds), fall back
        // to the legacy "appBundle" name so already-shipped APKs and downloaded experiences keep working.
        private static string ResolveBundlePath(string appFolder, bool useStreamingAsset)
        {
            var withExt = $"{appFolder}/appBundle.bundle";
            var legacy = $"{appFolder}/appBundle";

            if (useStreamingAsset)
                return BetterStreamingAssets.FileExists(withExt) ? withExt : legacy;

            return File.Exists(withExt) ? withExt : legacy;
        }

        public bool UseSceneName => Id == 35;

        /// <summary>
        /// Load the scene from the AppPack.
        /// </summary>
        public IEnumerator LoadScene()
        {
            // Extract if you haven't?
            Extract(Id);

            LogMemory("[Loading]");
            _avatarBefore = VRAvatar.Active;

            // Prefer a downloaded copy in persistentDataPath so re-downloads / updates take effect;
            // fall back to a StreamingAssets-shipped copy only when there's no download.
            var persistentFolder = GetLimappPath(Id);
            var streamingAssetFolderPath = $"Limapps/{GetPlatformName()}/{Id}";
            var useStreamingAsset = !Directory.Exists(persistentFolder)
                && BetterStreamingAssets.DirectoryExists(streamingAssetFolderPath);

            var appFolder = useStreamingAsset ? streamingAssetFolderPath : persistentFolder;

            LoadState = ELimappLoadState.LoadingAssetBundle;

            var bundlePath = ResolveBundlePath(appFolder, useStreamingAsset);

            AssetBundle assetBundle = null;

            // Currently rise has to be fully embeded.
            if (!UseSceneName)
            {
                if (useStreamingAsset)
                {
                    Debug.Log($"Loading from streaming asset at {bundlePath}");
                    var request = BetterStreamingAssets.LoadAssetBundleAsync(bundlePath);
                    yield return new WaitUntil(() => request.isDone);
                    assetBundle = request.assetBundle;

                    yield return SceneManagerLoadScene();
                }
                else
                {
                    Debug.Log($"Loading from file at {bundlePath}");
                    var fileStream = new FileStream(bundlePath, FileMode.Open, FileAccess.Read);
                    var request = AssetBundle.LoadFromStreamAsync(fileStream);
                    yield return new WaitUntil(() => request.isDone);
                    assetBundle = request.assetBundle;

                    yield return SceneManagerLoadScene();
                }
            }
            else
            {
                yield return SceneManagerLoadScene();
            }

            ExperienceApp = GetExperienceApp();

            LoadState = ELimappLoadState.NotInProgress;
            // is this called multiple times?
            IEnumerator SceneManagerLoadScene()
            {
                var sceneName = "";
                if (assetBundle != null)
                {
                    sceneName = assetBundle.GetAllScenePaths()[0];
                    CurrentBundle = assetBundle;
                }
                else
                {
                    sceneName = "Energizing";
                }

                SceneName = sceneName;

                // Duplicate below
                LoadState = ELimappLoadState.LoadingScene;

                var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                SceneManager.sceneLoaded += PlayApp;
                op.allowSceneActivation = false;

                yield return new WaitUntil(() => op.progress >= 0.9f);
                LoadState = ELimappLoadState.ActivatingScene;
                op.allowSceneActivation = true;

                yield return new WaitUntil(() => op.isDone);
            }

            /*string[] GetAssemblyPaths()
            {
                return useStreamingAsset
                    ? BetterStreamingAssets.GetFiles(assemblyFolder, "*")
                    : Directory.GetFiles(assemblyFolder);
            }*/

            void PlayApp(Scene scene, LoadSceneMode mode)
            {
                SceneManager.SetActiveScene(scene);
                SceneManager.sceneLoaded -= PlayApp;

                _startTime = DateTime.UtcNow;
                _gameStartTime = Time.time;

                // Basically some experiences may fail to call .Resume and this can stall and break things!
                InitializeApp();
            }

            void LoadAssemblies()
            {
                /*foreach (var path in asmPaths)
                {
                    // Though this uses BetterStreamingAssets, it works on both. 
                    var asmBytes = useStreamingAsset ? BetterStreamingAssets.ReadAllBytes(path) : File.ReadAllBytes(path);

                    if (asmBytes == null || asmBytes.Length == 0)
                        Debug.LogError("Assembly has no bytes");

                    var asm = Assembly.Load(asmBytes);
                    assemblies.Add(asm);

                    // Note: This is required by the app to be able to correctly deserialize some types after being imported.
                    try
                    {
                        SerializationUtils.AddGlobalSerializableTypes(asm);
                        Debug.Log($"Assembly Loaded {asm}");
                    }
                    catch
                    {
                        Debug.LogError("Unable to fully load assembly");
                    }
                }*/
            }
        }

        /// <summary>
        /// Unloads the limapp and clean up to ensure the next app can be run without conflicts.
        /// This also reload the previous scenes before loading a limapp.
        /// </summary>
        public IEnumerator Unload()
        {
            /*
            foreach (var asm in Assemblies)
            {
                var types = asm.GetTypes();

                foreach (var type in types)
                    ResetAllStaticsVariables(type);
            }

            SerializationUtils.ClearGlobalSerializableTypes();
            */

            if (ExperienceApp == null)
                Debug.Log("Experience app is null");

            ExperienceApp = null;
            Assemblies = null;

            yield return SceneManager.UnloadSceneAsync(SceneName);

            UnloadAssetBundle();
            ExperienceAppReflectionCache.IsEndingField.SetValue(null, false);

            yield return Resources.UnloadUnusedAssets();
            GC.Collect();

            Debug.Log("Unloaded...");

            void UnloadAssetBundle()
            {
                if (CurrentBundle != null)
                    CurrentBundle.Unload(unloadAllLoadedObjects: true);

                ExperienceAppReflectionCache.AssetBundleField.SetValue(null, null);
                CurrentBundle = null;
            }
        }

        /// <summary>
        /// TODO update this to also give the 'version' if there is a difference.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool DownloadedAndShouldUseFromCache(int id)
        {
            // Rise is always true.
            if (id == 35)
                return true;

            var mainPath = GetLimappFolderPath();
            var downloadToPath = $"{mainPath}/{id}.zip";
            var extractToPath = $"{mainPath}/{id}";
            var streamingAssetFolderPath = GetStreamingAssetFolderPath(id);
            return File.Exists(downloadToPath) || BetterStreamingAssets.DirectoryExists(streamingAssetFolderPath);
        }

        // TODO Implement Update Required method
        public static bool UpdateRequired(int id)
        {
            return false;
        }

        /// <summary>Base URL for v3 limapp uploads (produced by the SDK's S3 uploader window).</summary>
        public const string S3LimappV3BaseUrl = "https://liminal-resources.s3.ap-southeast-2.amazonaws.com/app/Limapp/v3";

        /// <summary>S3 URL of the platform-specific zip for <paramref name="id"/>.</summary>
        public static string GetExperienceUrl(int id) => $"{S3LimappV3BaseUrl}/{GetPlatformName()}/{id}.zip";

        /// <summary>Path of the cached version marker (stores the downloaded object's ETag/Last-Modified).</summary>
        private static string GetMarkerPath(int id) => $"{GetLimappFolderPath()}/{id}.etag";

        // Ids whose EnsureLatest is currently running, to coalesce concurrent requests.
        private static readonly HashSet<int> _ensuring = new HashSet<int>();

        public static IEnumerator DownloadAndExtractExperience(int id)
        {
            yield return DownloadAndExtract(GetExperienceUrl(id), id);
        }

        /// <summary>
        /// True when <paramref name="id"/> can be loaded locally — embedded, shipped in StreamingAssets,
        /// or already downloaded into persistentDataPath.
        /// </summary>
        public static bool IsContentAvailable(int id)
        {
            if (id == 35)
                return true;
            if (BetterStreamingAssets.DirectoryExists(GetStreamingAssetFolderPath(id)))
                return true;
            return Directory.Exists($"{GetLimappFolderPath()}/{id}");
        }

        /// <summary>
        /// Ensures limapp <paramref name="id"/> is present and current in persistentDataPath before it is
        /// played. HEADs the S3 object and compares its ETag/Last-Modified against a marker cached next to
        /// the zip; (re)downloads and extracts only when missing or changed, wiping any stale copy first.
        /// If S3 is unreachable, falls back to whatever is already cached.
        /// </summary>
        public static IEnumerator EnsureLatest(int id, Action<float> onProgress = null, Action<string> onStatus = null)
        {
            // Embedded experience — nothing to fetch.
            if (id == 35)
                yield break;

            // A StreamingAssets-shipped copy is used as-is (no remote check).
            if (BetterStreamingAssets.DirectoryExists(GetStreamingAssetFolderPath(id)))
                yield break;

            // Coalesce concurrent requests for the same id.
            if (_ensuring.Contains(id))
            {
                yield return new WaitUntil(() => !_ensuring.Contains(id));
                yield break;
            }

            _ensuring.Add(id);
            try
            {
                var folder = GetLimappFolderPath();
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var url = GetExperienceUrl(id);
                var zipPath = $"{folder}/{id}.zip";
                var extractDir = $"{folder}/{id}";
                var markerPath = GetMarkerPath(id);

                onStatus?.Invoke("Checking");

                // --- Detect a new upload via the object's ETag/Last-Modified ---
                string remoteTag = null;
                var headOk = false;
                using (var head = UnityWebRequest.Head(url))
                {
                    yield return head.SendWebRequest();
                    headOk = head.result == UnityWebRequest.Result.Success;
                    if (headOk)
                        remoteTag = head.GetResponseHeader("ETag") ?? head.GetResponseHeader("Last-Modified");
                    else
                        Debug.LogWarning($"[Limapp] Version check failed for {id} ({head.responseCode}): {head.error}");
                }

                var haveExtract = Directory.Exists(extractDir);
                var localTag = File.Exists(markerPath) ? File.ReadAllText(markerPath).Trim() : null;

                if (!headOk)
                {
                    // Offline or missing object: use the cached copy if we have one.
                    if (!haveExtract)
                        Debug.LogError($"[Limapp] {id} is not downloaded and could not be reached on S3 ({url}).");
                    yield break;
                }

                var needDownload = !haveExtract
                    || string.IsNullOrEmpty(localTag)
                    || (remoteTag != null && remoteTag != localTag);

                if (!needDownload)
                {
                    onStatus?.Invoke("Up to date");
                    yield break;
                }

                onStatus?.Invoke(haveExtract ? "Updating" : "Downloading");
                Debug.Log($"[Limapp] {(haveExtract ? "Updating" : "Downloading")} {id} from {url}");

                // Replace any stale copy entirely so the new version fully takes over.
                TryDelete(() => { if (File.Exists(zipPath)) File.Delete(zipPath); });
                TryDelete(() => { if (Directory.Exists(extractDir)) Directory.Delete(extractDir, recursive: true); });
                TryDelete(() => { if (File.Exists(markerPath)) File.Delete(markerPath); });

                using (var www = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
                {
                    www.downloadHandler = new DownloadHandlerFile(zipPath) { removeFileOnAbort = true };
                    var op = www.SendWebRequest();
                    while (!op.isDone)
                    {
                        onProgress?.Invoke(www.downloadProgress);
                        yield return null;
                    }

                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"[Limapp] Download failed for {id} ({www.responseCode}): {www.error}");
                        TryDelete(() => { if (File.Exists(zipPath)) File.Delete(zipPath); });
                        yield break;
                    }
                }
                onProgress?.Invoke(1f);

                if (!TryExtract(zipPath, extractDir))
                    yield break;

                File.WriteAllText(markerPath, remoteTag ?? string.Empty);
                onStatus?.Invoke("Ready");
                Debug.Log($"[Limapp] {id} ready at {extractDir}");
            }
            finally
            {
                _ensuring.Remove(id);
            }
        }

        private static bool TryExtract(string zipPath, string extractDir)
        {
            try
            {
                if (Directory.Exists(extractDir))
                    Directory.Delete(extractDir, recursive: true);
                ZipFile.ExtractToDirectory(zipPath, extractDir);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Limapp] Extract failed for '{zipPath}': {ex.Message}");
                return false;
            }
        }

        private static void TryDelete(Action action)
        {
            try { action(); }
            catch (Exception ex) { Debug.LogWarning($"[Limapp] Cleanup failed: {ex.Message}"); }
        }

        public static string GetPlatformName()
        {
            var platformName = Application.platform == RuntimePlatform.Android ? "Android" : "Standalone";
            var resolvedName = Application.isEditor ? "Standalone" : platformName;
            return resolvedName;
        }

        public static string GetLimappPath(int id)
        {
            return $"{GetLimappFolderPath()}/{id}";
        }

        public static string GetLimappFolderPath()
        {
            var folderName = GetPlatformName();
            var mainPath = $"{Application.persistentDataPath}/Limapps/{folderName}";
            return mainPath;
        }

        public static Dictionary<int, UnityWebRequest> DownloadRequests = new Dictionary<int, UnityWebRequest>();

        public class LimappDownloadRequest
        {
            public UnityWebRequest Request;
        }

        public static IEnumerator DownloadAndExtract(string url, int id)
        {
            Debug.Log($"[Downloading] {id}");

            var mainPath = GetLimappFolderPath();

            // No need to, extract to actual folder.
            //mainPath += $"/{id}";

            if (!Directory.Exists(mainPath))
                Directory.CreateDirectory(mainPath);

            var name = id.ToString();
            var downloadToPath = $"{mainPath}/{name}.zip";
            var extractToPath = $"{mainPath}/{name}";

            var www = new UnityWebRequest(url) { method = UnityWebRequest.kHttpVerbGET };
            var dh = new DownloadHandlerFile(downloadToPath) { removeFileOnAbort = true };
            www.downloadHandler = dh;

            DownloadRequests.Add(id, www);

            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
                Debug.Log(www.error);
            else
                Debug.Log("Download saved to: " + downloadToPath);

            www.Dispose();
            DownloadRequests.Remove(id);
        }

        public static bool Extract(int id)
        {
            var mainPath = GetLimappFolderPath();
            var name = id.ToString();
            var downloadToPath = $"{mainPath}/{name}.zip";
            var extractToPath = $"{mainPath}/{name}";

            if (Directory.Exists(extractToPath))
            {
                // This zip has already been extracted.
                return true;
            }

            if (File.Exists(downloadToPath))
            {
                ZipFile.ExtractToDirectory(downloadToPath, extractToPath);
                return true;
            }
            else
            {
                Debug.Log($"Experience {id} has not been uploaded to the s3 or is not downloaded.");
                return false;
            }
        }

        public void ResetAllStaticsVariables(Type type)
        {
            var fields = type.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Public);
            foreach (var fieldInfo in fields)
            {
                if (fieldInfo.IsLiteral && !fieldInfo.IsInitOnly)
                    continue;

                if (fieldInfo.FieldType == typeof(Texture2D) ||
                    fieldInfo.FieldType == typeof(AudioClip) ||
                    fieldInfo.FieldType == typeof(List<AudioClip>) ||
                    fieldInfo.FieldType == typeof(List<Texture2D>) ||
                    fieldInfo.FieldType == typeof(AudioClip[]) ||
                    fieldInfo.FieldType == typeof(Texture2D[]) ||
                    fieldInfo.FieldType == typeof(Texture) ||
                    fieldInfo.FieldType == typeof(RenderTexture) ||
                    fieldInfo.FieldType == typeof(List<Texture>) ||
                    fieldInfo.FieldType == typeof(List<RenderTexture>) ||
                    fieldInfo.FieldType == typeof(Texture[]) ||
                    fieldInfo.FieldType == typeof(Material[]) ||
                    fieldInfo.FieldType == typeof(Material) ||
                    fieldInfo.FieldType == typeof(List<Material>) ||
                    fieldInfo.FieldType == typeof(RenderTexture[]))
                {
                    fieldInfo.SetValue(null, GetDefault(type));
                }
            }
        }

        public object GetDefault(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);

            return null;
        }

        /// <summary>
        /// Get ExperienceApp in the currently loaded scene. There should only ever be one.
        /// TODO Use a cleaner, less unpredictable method. With this, we cannot have multiple ExperienceApp.
        /// </summary>
        /// <returns></returns>
        private ExperienceApp GetExperienceApp()
        {
            var apps = Resources.FindObjectsOfTypeAll<ExperienceApp>();
            return apps.Length > 0 ? apps[0] : null;
        }

        /// <summary>
        /// Ensure the limapp is not trying to run itself and allow the platform app to run it.
        /// </summary>
        private void EnsureEmulatorFlagIsFalse()
        {
            var isEmulator = typeof(ExperienceApp).GetField("_isEmulator", BindingFlags.Static | BindingFlags.NonPublic);
            isEmulator.SetValue(null, false);
        }

        /// <summary>
        /// Before initialization, all children of ExperienceApp of a limapp is turned off.
        /// The initialize call will turn on all game objects that should be active, on.
        /// </summary>
        private void InitializeApp()
        {
            // Forcing this to be null
            ApplyExperienceSettings(Id);
        }

        private void ApplyExperienceSettings(int id)
        {
            // TODO is this done somewhere on the platform instead?
            switch (id)
            {
                case 89:
                    QualitySettings.pixelLightCount = 3;
                    break;
                default:
                    QualitySettings.pixelLightCount = 1;
                    break;
            }
        }


        public static void FindMissingReferences(MonoBehaviour component)
        {
            if (component == null)
                return;

            // Check fields
            FieldInfo[] fields = component.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                if (typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                {
                    UnityEngine.Object value = field.GetValue(component) as UnityEngine.Object;
                    if (value == null)
                    {
                        var foundComponent = GameObject.FindObjectOfType(field.FieldType);
                        if (foundComponent != null)
                        {
                            field.SetValue(component, foundComponent);
                        }
                        Debug.LogError($"Missing reference found in {component.gameObject.name} on component {component.GetType()}, field {field.Name}", component.gameObject);
                    }
                }
            }

            // Check properties
            PropertyInfo[] properties = component.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (PropertyInfo property in properties)
            {
                if (typeof(UnityEngine.Object).IsAssignableFrom(property.PropertyType) && property.CanRead)
                {
                    UnityEngine.Object value = property.GetValue(component, null) as UnityEngine.Object;
                    if (value == null && property.GetIndexParameters().Length == 0) // Exclude indexed properties
                    {
                        Debug.LogError($"Missing reference found in {component.gameObject.name} on component {component.GetType()}, property {property.Name}", component.gameObject);
                    }
                }
            }
        }

        public void SetState(ELimappState state)
        {
            State = state;
        }
    }
}


/// <summary>
/// All need to be shared with the Platform instead.
/// </summary>
namespace App.Core
{
    public static class LimappStateUtils
    {
    }

    public enum ELimappState
    {
        Idle,
        Running,
        Ending
    }

    public enum ELimappLoadState
    {
        NotInProgress,
        Unpacking,
        LoadingAssetBundle,
        LoadingScene,
        ActivatingScene,
    }
}

// Ignored Experiences
// 70: Disabled.

public class LimappSpecialCases
{
    public void ApplyFFR(int experienceId)
    {
        switch (experienceId)
        {
            case 88:
            case 79:
            case 147:
                OVRManager.fixedFoveatedRenderingLevel = OVRManager.FixedFoveatedRenderingLevel.Medium;
                break;
        }
    }

    public void ApplySpecificAppSettings(int id, VRAvatar avatar, ExperienceConfig experienceConfig)
    {
        ApplyFFR(id);

        GearVRAvatar.PointerActivationType = EPointerActivationType.Both;

        /*if (CanUseLWRP(experience) && experienceConfig.Pipeline == EPipeline.LightWeightRenderPipeline)
            GraphicsSettings.renderPipelineAsset = LightWeightPipeline;*/

        //Debug.Log($"Controllers: Config - Controller count: {experienceConfig.ControllersCount}");

        if (experienceConfig.ControllersCount == EControllerCount.None)
        {
            var hands = avatar.GetComponentsInChildren<VRAvatarController>(includeInactive: true);

            //Debug.Log($"Controllers: Hands - Hand Count: {hands.Length}");

            foreach (var hand in hands)
            {
                var renderers = hand.GetComponentsInChildren<MeshRenderer>();

                foreach (var meshRenderer in renderers)
                {
                    meshRenderer.enabled = false;
                    //Debug.Log($"Controllers: Hands - Renderer: {meshRenderer.name}");
                }

                var skinnedMeshRenderers = hand.GetComponentsInChildren<SkinnedMeshRenderer>();

                foreach (var meshRenderer in skinnedMeshRenderers)
                {
                    meshRenderer.enabled = false;
                    //Debug.Log($"Controllers: Hands - Skinned Renderer: {meshRenderer.name}");
                }
            }

            //hand.gameObject.SetActive(false);
        }

        if (experienceConfig.ControllersCount == EControllerCount.Two)
        {
            var hands = avatar.GetComponentsInChildren<VRAvatarHand>(includeInactive: true);
            foreach (var hand in hands)
            {
                var controller = hand.GetComponentInChildren<VRAvatarController>(includeInactive: true);
                var anchor = hand.transform.Find("Anchor");

                if (controller != null)
                    controller.gameObject.SetActive(true);

                if (anchor != null)
                    anchor.gameObject.SetActive(true);
            }
        }

        //Debug.Log($"Controllers: Config - Pointer Status: {experienceConfig.PointerActive}");

        if (experienceConfig.PointerActive == EPointerActiveType.Hide)
            DisablePointerVisual(avatar);
    }

    void DisablePointerVisual(VRAvatar avatar)
    {
        var pointers = avatar.GetComponentsInChildren<LaserPointerVisual>(includeInactive: true);
        foreach (var pointer in pointers)
        {
            pointer.DefaultReticleDistance = 0;
            pointer.GrowTime = 0;
            pointer.MaxDistance = 0;
            pointer.HideReticleWhenInvalid = true;

            var reticle = pointer.GetComponentInChildren<ReticleVisual>(includeInactive: true);
            var renderer = reticle.GetComponent<MeshRenderer>();
            renderer.enabled = false;
        }
    }

    public void ResetHandChildrenPositions(VRAvatar avatar, ExperienceConfig? config)
    {
        // Fix objects placed in the wrong location.
        var deviceType = XRDeviceUtils.GetDeviceModelType();

        // TODO Clean up the number of &&s
        if (deviceType != EDeviceModelType.Go
            && deviceType != EDeviceModelType.Quest
            && deviceType != EDeviceModelType.Quest2
            && deviceType != EDeviceModelType.QuestPro)
        {
            // offset hand anchor positions
            var vrHands = avatar.GetComponentsInChildren<VRAvatarHand>(includeInactive: true);
            foreach (var hand in vrHands)
            {
                foreach (Transform t in hand.transform)
                {
                    if (t != hand.Anchor)
                    {
                        var position = t.localPosition;
                        position.z = 0;
                        t.localPosition = position;
                    }

                    if (config != null)
                    {
                        var localOffset = config.Value.LocalOffset;
                        var offset = new Vector3(localOffset.X, localOffset.Y, localOffset.Z);

                        t.localPosition += offset;
                    }
                }
            }
        }
    }
}

