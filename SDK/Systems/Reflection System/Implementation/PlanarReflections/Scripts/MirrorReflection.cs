using Liminal.Systems;

namespace App
{
    using UnityEngine;
    using UnityEngine.XR;
    using System.Collections;

    // This is in fact just the Water script from Pro Standard Assets,
    // just with refraction stuff removed.

    public class MirrorReflection : MonoBehaviour
    {
        public Renderer m_Renderer;
        public bool m_DisablePixelLights = true;
        public int m_TextureSize = 256;
        public float m_ClipPlaneOffset = 0.07f;

        public Vector3 Offset;

        public LayerMask m_ReflectLayers = -1;

        private Hashtable m_ReflectionCameras = new Hashtable(); // Camera -> Camera table

        private RenderTexture m_ReflectionTexture = null;
        private int m_OldReflectionTextureSize = 0;

        private static bool s_InsideRendering = false;
        private static readonly int s_offsetEnabled = Shader.PropertyToID("_OffsetEnabled");

        // Diagnostic logging - only log the first few render attempts to avoid per-frame spam.
        private int m_RenderLogCount;
        private const int MaxRenderLogs = 5;

#if SMOOTH_CAM
        public Camera Cam => GameObject.Find("SmoothCam").GetComponent<Camera>();
#else
        public Camera Cam => Camera.main;
#endif

        public ReflectionOffsetModel Ipd58OffsetModel = new ReflectionOffsetModel(1.194927f, -0.186721f, 0.8499745f);
        public ReflectionOffsetModel Ipd63OffsetModel = new ReflectionOffsetModel(1.077431f, -0.07495025f, 0.9323733f);
        public ReflectionOffsetModel Ipd68OffsetModel = new ReflectionOffsetModel(1.044061f, -0.006401608f, 1.038761f, -0.03448246f);

        public static ReflectionOffsetModel IpdModel = null;

        public bool HasQuest2FOV;

        public static float ipd = 0;

        private void Awake()
        {
            if (ipd <= 0)
            {
                ipd = GetXRIpd();
            }
            Debug.Log($"[MirrorReflection] Awake on '{name}' | ipd: {ipd} | Cam: {(Cam != null ? Cam.name : "NULL")} | Cam FOV: {(Cam != null ? Cam.fieldOfView.ToString() : "n/a")}");

            HasQuest2FOV = Cam.fieldOfView <= 100;

            if (IpdModel != null)
            {
                Debug.Log("[MirrorReflection] IpdModel already set, skipping selection");
                return;
            }

            if (ipd > 0)
                SelectIpdModel();
        }

        /// <summary>
        /// IPD from the XR head device's eye poses. OVRPlugin.ipd is unavailable under
        /// Unity XR/OpenXR (native OVRPlugin lib is not shipped - DllNotFoundException).
        /// Returns 0 until the XR session starts reporting eye positions.
        /// </summary>
        private static float GetXRIpd()
        {
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (head.isValid &&
                head.TryGetFeatureValue(CommonUsages.leftEyePosition, out var leftEye) &&
                head.TryGetFeatureValue(CommonUsages.rightEyePosition, out var rightEye))
            {
                return Vector3.Distance(leftEye, rightEye);
            }

            return 0f;
        }

        private void SelectIpdModel()
        {
            var deviceModel = XRDeviceUtils.GetDeviceModelType();

            if (deviceModel == EDeviceModelType.QuestPro)
            {
                if (ipd >= 0.055f && ipd < 0.061f)
                    IpdModel = Ipd58OffsetModel;

                if (ipd >= 0.061f && ipd < 0.0655f)
                    IpdModel = Ipd63OffsetModel;

                if (ipd >= 0.0655f)
                    IpdModel = Ipd68OffsetModel;
            }
            else // Quest 2 
            {
                if (ipd >= 0.055f && ipd < 0.062f)
                    IpdModel = Ipd58OffsetModel;

                if (ipd >= 0.062f && ipd < 0.067f)
                    IpdModel = Ipd63OffsetModel;

                if (ipd >= 0.067f)
                    IpdModel = Ipd68OffsetModel;
            }

            // There is an odd reason where Quest 3 in the Platform uses different IPD values than SDK.
            if (deviceModel == EDeviceModelType.Quest3)
            {
                if (ipd < 0.060f)
                    IpdModel = Ipd58OffsetModel;
                else if (ipd < 0.066f)
                    IpdModel = Ipd63OffsetModel;
                else
                    IpdModel = Ipd68OffsetModel;
            }

            var modelName = IpdModel == null ? "NULL (ipd below 0.055 threshold?)"
                : IpdModel == Ipd58OffsetModel ? "Ipd58"
                : IpdModel == Ipd63OffsetModel ? "Ipd63"
                : "Ipd68";
            Debug.Log($"[MirrorReflection] IPD {ipd} | deviceModel: {deviceModel} | selected IpdModel: {modelName}");
        }

        private IEnumerator Start()
        {
            m_Renderer = GetComponent<Renderer>();

            Debug.Log($"[MirrorReflection] Start on '{name}' | renderer: {(m_Renderer != null ? m_Renderer.GetType().Name : "NULL")} | shader: {(m_Renderer != null ? m_Renderer.material.shader.name : "n/a")} | has _ReflectionTex: {(m_Renderer != null && m_Renderer.material.HasProperty("_ReflectionTex"))}");

            m_Renderer.material.SetFloat("_Quest", 0);
            m_Renderer.material.SetFloat("_Rift", 0);
            m_Renderer.material.SetFloat("_RiftS", 0);
            m_Renderer.material.SetFloat("_Vive", 0);
            m_Renderer.material.SetFloat("_VivePro", 0);

            var model = XRDeviceUtils.GetDeviceModelType();

            // Quest 1 specifically. OVRUtils.IsOculusQuest was used here but it calls
            // into the native OVRPlugin lib, which is absent under Unity XR/OpenXR
            // (DllNotFoundException aborted Start on device).
            var isQuest1 = model == EDeviceModelType.Quest;

#if UNITY_ANDROID
            m_Renderer.material.SetFloat(s_offsetEnabled, isQuest1 ? 1 : 0);
            m_Renderer.material.SetFloat("_Quest", isQuest1 ? 1 : 0);
#endif
            Debug.Log($"[Mirror Reflection] Model Name: {model}, is Quest 1: {isQuest1}");
#if UNITY_STANDALONE
            m_Renderer.material.SetFloat(s_offsetEnabled, 1);

            switch (model)
            {
                case EDeviceModelType.Rift:
                    m_Renderer.material.SetFloat("_Rift", 1);
                    break;
                case EDeviceModelType.RiftS:
                    m_Renderer.material.SetFloat("_RiftS", 1);
                    break;
                case EDeviceModelType.HtcVive:
                    m_Renderer.material.SetFloat("_Vive", 1);
                    break;
                case EDeviceModelType.HtcVivePro:
                    m_Renderer.material.SetFloat("_VivePro", 1);
                    break;
                case EDeviceModelType.Quest:
                    m_Renderer.material.SetFloat("_Quest", 1);
                    break;
            }
#endif

            if (model == EDeviceModelType.Quest2 ||
                model == EDeviceModelType.QuestPro ||
                model == EDeviceModelType.Quest3)
            {
                // Awake can run before the XR session reports eye poses, leaving ipd 0
                // and no IpdModel selected - wait for a real IPD instead of applying null.
                var timeout = Time.unscaledTime + 5f;
                while (IpdModel == null && Time.unscaledTime < timeout)
                {
                    if (ipd <= 0)
                        ipd = GetXRIpd();

                    if (ipd > 0)
                        SelectIpdModel();

                    if (IpdModel == null)
                        yield return null;
                }

                if (IpdModel == null)
                {
                    Debug.LogWarning($"[MirrorReflection] No IPD from XR after 5s (ipd: {ipd}) - falling back to Ipd63 offsets");
                    IpdModel = Ipd63OffsetModel;
                }

                m_Renderer.material.SetFloat("_Quest", 0);
                m_Renderer.material.SetFloat(s_offsetEnabled, 1);
                m_Renderer.material.SetFloat("_Debug", 1);

                SetMaterial(IpdModel);
            }
            else
            {
                Debug.Log($"[MirrorReflection] Model {model} not in Quest2/Pro/3 branch - no IPD offset material applied");
            }

            // This is only true in Unity 2019!
            HasQuest2FOV = Cam.fieldOfView <= 100;

            // In 2022, even without Meta's hack, our FOV is 95. 
            // This should be renamed to HasMetaHack
#if UNITY_2022_1_OR_NEWER
            HasQuest2FOV = false;
#endif

            

            // Meta has a hack in 2023 for app with com.LiminalVR.Liminal as the package name
            // The hack would enforce FOV of Quest 2. 
            // So here we are checking if this hack has been removed on this device. (could be through meta switch or firmware.)
            // And if so, use these values, they work for all IPD!
            Debug.Log($"[MirrorReflection] HasQuest2FOV: {HasQuest2FOV} | Cam FOV: {(Cam != null ? Cam.fieldOfView.ToString() : "NULL cam")} | model: {model}");

            if (!HasQuest2FOV)
            {
                if (model == EDeviceModelType.Quest3 || model == EDeviceModelType.QuestPro)
                {
                    Debug.Log("[MirrorReflection] Applying no-Meta-hack offsets for Quest3/QuestPro");
                    m_Renderer.material.SetFloat("_Quest", 0);
                    m_Renderer.material.SetFloat("_OffsetEnabled", 1);
                    m_Renderer.material.SetFloat("_Debug", 1);
                    m_Renderer.material.SetFloat("_UseL", 0);
                    m_Renderer.material.SetFloat("_OffsetRX", 1.233837f);
                    m_Renderer.material.SetFloat("_OffsetRY", 1);
                    m_Renderer.material.SetFloat("_OffsetRZ", -0.2374479f);
                    m_Renderer.material.SetFloat("_OffsetRW", 0);
                    m_Renderer.material.SetFloat("_OffsetX", 0.8047135f);
                }
            }


            // For some highly unknown reason the SDK only works with below. I do not know why and really want to know why!
            /*if (model == EDeviceModelType.Quest3)
            {
                m_Renderer.material.SetFloat("_Quest", 0);
                m_Renderer.material.SetFloat("_OffsetEnabled", 1);
                m_Renderer.material.SetFloat("_Debug", 1);
                m_Renderer.material.SetFloat("_OffsetRX", 1.230723f);
                m_Renderer.material.SetFloat("_OffsetRY", 1);
                m_Renderer.material.SetFloat("_OffsetRZ", -0.2374479f);
                m_Renderer.material.SetFloat("_OffsetRW", 0);
                m_Renderer.material.SetFloat("_OffsetX", 0.8047135f);
            }*/

            void SetMaterial(ReflectionOffsetModel m)
            {
                m_Renderer.material.SetFloat("_OffsetRX", m.RX);
                m_Renderer.material.SetFloat("_OffsetRY", m.RY);
                m_Renderer.material.SetFloat("_OffsetRZ", m.RZ);
                m_Renderer.material.SetFloat("_OffsetRW", m.RW);
                m_Renderer.material.SetFloat("_OffsetX", m.LOffset);

                m_Renderer.material.SetFloat("_UseL", m.UseL ? 1 : 0);
                m_Renderer.material.SetFloat("_OffsetLX", m.LX);
                m_Renderer.material.SetFloat("_OffsetLZ", m.LZ);
            }
        }

        // This is called when it's known that the object will be rendered by some
        // camera. We render reflections and do other updates here.
        // Because the script executes in edit mode, reflections for the scene view
        // camera will just work!
        public void OnWillRenderObject()
        {
            var log = m_RenderLogCount < MaxRenderLogs;

            if (m_Renderer == null)
            {
                if (log) { m_RenderLogCount++; Debug.Log($"[MirrorReflection] OnWillRenderObject skipped on '{name}': m_Renderer is null (Start not run yet?)"); }
                return;
            }

            if (!enabled || !m_Renderer || !m_Renderer.sharedMaterial || !m_Renderer.enabled)
            {
                if (log) { m_RenderLogCount++; Debug.Log($"[MirrorReflection] OnWillRenderObject skipped on '{name}': enabled={enabled}, sharedMaterial={(m_Renderer.sharedMaterial != null)}, rendererEnabled={m_Renderer.enabled}"); }
                return;
            }

            Camera cam = Cam;
            if (!cam)
            {
                if (log) { m_RenderLogCount++; Debug.Log($"[MirrorReflection] OnWillRenderObject skipped on '{name}': Cam is null (no MainCamera tag?)"); }
                return;
            }

            // Safeguard from recursive reflections.
            if (s_InsideRendering)
            {
                if (log) { m_RenderLogCount++; Debug.Log($"[MirrorReflection] OnWillRenderObject skipped on '{name}': s_InsideRendering is true (stuck from an earlier exception?)"); }
                return;
            }
            s_InsideRendering = true;

            Camera reflectionCamera;
            CreateMirrorObjects(cam, out reflectionCamera);

            // find out the reflection plane: position and normal in world space
            Vector3 pos = transform.position;
            Vector3 normal = transform.up + Offset;

            // Optionally disable pixel lights for reflection
            int oldPixelLightCount = QualitySettings.pixelLightCount;
            if (m_DisablePixelLights)
                QualitySettings.pixelLightCount = 0;

            UpdateCameraModes(cam, reflectionCamera);

            // Render reflection
            // Reflect camera around reflection plane
            float d = -Vector3.Dot(normal, pos) - m_ClipPlaneOffset;
            Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);

            Matrix4x4 reflection = Matrix4x4.zero;
            CalculateReflectionMatrix(ref reflection, reflectionPlane);
            Vector3 oldpos = cam.transform.position;
            Vector3 newpos = reflection.MultiplyPoint(oldpos);
            reflectionCamera.worldToCameraMatrix = cam.worldToCameraMatrix * reflection;

            // Setup oblique projection matrix so that near plane is our reflection
            // plane. This way we clip everything below/above it for free.
            Vector4 clipPlane = CameraSpacePlane(reflectionCamera, pos, normal, 1.0f);
            //Matrix4x4 projection = cam.projectionMatrix;
            Matrix4x4 projection = cam.CalculateObliqueMatrix(clipPlane);
            reflectionCamera.projectionMatrix = projection;

            reflectionCamera.cullingMask = ~(1 << 4) & m_ReflectLayers.value; // never render water layer
            reflectionCamera.targetTexture = m_ReflectionTexture;
            GL.invertCulling = true;
            reflectionCamera.transform.position = newpos;
            Vector3 euler = cam.transform.eulerAngles;
            reflectionCamera.transform.eulerAngles = new Vector3(0, euler.y, euler.z);

            try
            {
                reflectionCamera.Render();
            }
            catch (System.Exception e)
            {
                // Without this catch an exception here leaves s_InsideRendering stuck true,
                // permanently disabling every mirror in the scene with no visible error.
                Debug.LogError($"[MirrorReflection] reflectionCamera.Render() threw on '{name}': {e}");
            }

            reflectionCamera.transform.position = oldpos;
            GL.invertCulling = false;
            Material[] materials = m_Renderer.sharedMaterials;
            var materialsWithReflectionTex = 0;
            foreach (Material mat in materials)
            {
                if (mat.HasProperty("_ReflectionTex"))
                {
                    mat.SetTexture("_ReflectionTex", m_ReflectionTexture);
                    materialsWithReflectionTex++;
                }
            }

            if (m_RenderLogCount < MaxRenderLogs)
            {
                m_RenderLogCount++;
                Debug.Log($"[MirrorReflection] Rendered reflection #{m_RenderLogCount} on '{name}' | cam: {cam.name} @ {cam.transform.position} | texture: {m_ReflectionTexture.width}px created: {m_ReflectionTexture.IsCreated()} | cullingMask: {reflectionCamera.cullingMask} | materials with _ReflectionTex: {materialsWithReflectionTex}/{materials.Length}");
            }

            // Restore pixel light count
            if (m_DisablePixelLights)
                QualitySettings.pixelLightCount = oldPixelLightCount;

            s_InsideRendering = false;
        }


        // Cleanup all the objects we possibly have created
        void OnDisable()
        {
            if (m_ReflectionTexture)
            {
                DestroyImmediate(m_ReflectionTexture);
                m_ReflectionTexture = null;
            }

            try
            {
                foreach (DictionaryEntry kvp in m_ReflectionCameras)
                    DestroyImmediate(((Camera)kvp.Value).gameObject);
            }
            catch
            {
                Debug.Log("Caught reflection camera not destroying properly");
            }

            m_ReflectionCameras.Clear();

        }


        private void UpdateCameraModes(Camera src, Camera dest)
        {
            if (dest == null)
                return;
            // set camera to clear the same way as current camera
            dest.clearFlags = src.clearFlags;
            dest.backgroundColor = src.backgroundColor;
            if (src.clearFlags == CameraClearFlags.Skybox)
            {
                Skybox sky = src.GetComponent(typeof(Skybox)) as Skybox;
                Skybox mysky = dest.GetComponent(typeof(Skybox)) as Skybox;
                if (!sky || !sky.material)
                {
                    mysky.enabled = false;
                }
                else
                {
                    mysky.enabled = true;
                    mysky.material = sky.material;
                }
            }
            // update other values to match current camera.
            // even if we are supplying custom camera&projection matrices,
            // some of values are used elsewhere (e.g. skybox uses far plane)
            dest.farClipPlane = src.farClipPlane;
            dest.nearClipPlane = src.nearClipPlane;
            dest.orthographic = src.orthographic;
            dest.fieldOfView = src.fieldOfView;
            dest.aspect = src.aspect;
            dest.orthographicSize = src.orthographicSize;
        }

        // On-demand create any objects we need
        private void CreateMirrorObjects(Camera currentCamera, out Camera reflectionCamera)
        {
            reflectionCamera = null;

            // Reflection render texture
            if (!m_ReflectionTexture || m_OldReflectionTextureSize != m_TextureSize)
            {
                if (m_ReflectionTexture)
                    DestroyImmediate(m_ReflectionTexture);
                m_ReflectionTexture = new RenderTexture(m_TextureSize, m_TextureSize, 16);
                m_ReflectionTexture.name = "__MirrorReflection" + GetInstanceID();
                m_ReflectionTexture.isPowerOfTwo = true;
                m_ReflectionTexture.hideFlags = HideFlags.DontSave;
                m_OldReflectionTextureSize = m_TextureSize;
            }

            // Camera for reflection
            reflectionCamera = m_ReflectionCameras[currentCamera] as Camera;
            if (!reflectionCamera) // catch both not-in-dictionary and in-dictionary-but-deleted-GO
            {
                GameObject go = new GameObject("Mirror Refl Camera id" + GetInstanceID() + " for " + currentCamera.GetInstanceID(), typeof(Camera), typeof(Skybox));
                reflectionCamera = go.GetComponent<Camera>();
                reflectionCamera.enabled = false;
                reflectionCamera.transform.position = transform.position;
                reflectionCamera.transform.rotation = transform.rotation;
                reflectionCamera.gameObject.AddComponent<FlareLayer>();
                go.hideFlags = HideFlags.HideAndDontSave;
                m_ReflectionCameras[currentCamera] = reflectionCamera;
            }
        }

        // Extended sign: returns -1, 0 or 1 based on sign of a
        private static float sgn(float a)
        {
            if (a > 0.0f) return 1.0f;
            if (a < 0.0f) return -1.0f;
            return 0.0f;
        }

        // Given position/normal of the plane, calculates plane in camera space.
        private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
        {
            Vector3 offsetPos = pos + normal * m_ClipPlaneOffset;
            Matrix4x4 m = cam.worldToCameraMatrix;
            Vector3 cpos = m.MultiplyPoint(offsetPos);
            Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
        }

        // Calculates reflection matrix around the given plane
        private static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane)
        {
            reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
            reflectionMat.m01 = (-2F * plane[0] * plane[1]);
            reflectionMat.m02 = (-2F * plane[0] * plane[2]);
            reflectionMat.m03 = (-2F * plane[3] * plane[0]);

            reflectionMat.m10 = (-2F * plane[1] * plane[0]);
            reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
            reflectionMat.m12 = (-2F * plane[1] * plane[2]);
            reflectionMat.m13 = (-2F * plane[3] * plane[1]);

            reflectionMat.m20 = (-2F * plane[2] * plane[0]);
            reflectionMat.m21 = (-2F * plane[2] * plane[1]);
            reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
            reflectionMat.m23 = (-2F * plane[3] * plane[2]);

            reflectionMat.m30 = 0F;
            reflectionMat.m31 = 0F;
            reflectionMat.m32 = 0F;
            reflectionMat.m33 = 1F;
        }
    }
}