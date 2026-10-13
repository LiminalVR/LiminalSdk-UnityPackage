namespace App
{
    using UnityEngine;
    using UnityEngine.XR;
    using UnityEngine.Rendering;
    using System.Collections;
    using System.Collections.Generic;

    // This is in fact just the Water script from Pro Standard Assets,
    // just with refraction stuff removed.

    // Preserve the authored reflection-plane offsets, projection and shader math.
    // SRP snapshots the running provider's ordinary two-eye render parameters;
    // Camera.GetStereoViewMatrix may still contain URP's previous-frame override.
    // Before-render snapshots do not prove equivalence after later XR reconfiguration
    // or GPU late latching. Registration, winding, clipping and frame cost still
    // require rendered mono/stereo and moving-head tests before deployment.

    public class MirrorReflection : MonoBehaviour
    {
        public Renderer m_Renderer;
        public bool m_DisablePixelLights = true;
        public int m_TextureSize = 256;
        public float m_ClipPlaneOffset = 0.07f;

        public Vector3 Offset;

        public LayerMask m_ReflectLayers = -1;

        private Hashtable m_ReflectionCameras = new Hashtable(); // Camera -> Camera table

        private RenderTexture m_ReflectionTextureLeft = null;
        private RenderTexture m_ReflectionTextureRight = null;
        private int m_OldReflectionTextureSize = 0;

        private static bool s_InsideRendering = false;
        private static readonly int s_reflectionTex = Shader.PropertyToID("_ReflectionTex");
        private static readonly int s_reflectionTexRight = Shader.PropertyToID("_ReflectionTexRight");

        // Diagnostic logging - only log the first few render attempts to avoid per-frame spam.
        private int m_RenderLogCount;
        private readonly List<XRDisplaySubsystem> m_XRDisplays = new List<XRDisplaySubsystem>();
        private const int MaxRenderLogs = 5;

#if SMOOTH_CAM
        public Camera Cam => GameObject.Find("SmoothCam").GetComponent<Camera>();
#else
        public Camera Cam => Camera.main;
#endif

        // Kept for diagnostics (DebugInfo/ReflectionTester). No longer drives the reflection.
        public static float ipd = 0;

        private void Awake()
        {
            if (ipd <= 0)
                ipd = GetXRIpd();

            Debug.Log($"[MirrorReflection] Awake on '{name}' | ipd: {ipd} | Cam: {(Cam != null ? Cam.name : "NULL")}");
        }

        /// <summary>
        /// IPD from the XR head device's eye poses. OVRPlugin.ipd is unavailable under
        /// Unity XR/OpenXR (native OVRPlugin lib is not shipped - DllNotFoundException).
        /// Returns 0 until the XR session starts reporting eye positions.
        /// Diagnostic only - the reflection itself uses the per-eye stereo matrices.
        /// </summary>
        public static float GetXRIpd()
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

        private void Start()
        {
            m_Renderer = GetComponent<Renderer>();

            Debug.Log($"[MirrorReflection] Start on '{name}' | renderer: {(m_Renderer != null ? m_Renderer.GetType().Name : "NULL")} | shader: {(m_Renderer != null ? m_Renderer.material.shader.name : "n/a")} | has _ReflectionTex: {(m_Renderer != null && m_Renderer.material.HasProperty(s_reflectionTex))}");
        }

        // This is called when it's known that the object will be rendered by some
        // camera. We render reflections and do other updates here.
        // Because the script executes in edit mode, reflections for the scene view
        // camera will just work!
        private void OnEnable()
        {
            Application.onBeforeRender += OnBeforeRender;
        }

        // Submit SRP requests before the main render, outside the pipeline's
        // render loop. End-of-frame requests would leave a moving VR mirror
        // displaying the previous frame's view. GPU/XR alignment still needs
        // verification with the actual provider and late-latching settings.
        private void OnBeforeRender()
        {
            if (GraphicsSettings.currentRenderPipeline == null)
                return;
            // Graphics-enabled batch mode does not necessarily dispatch this
            // event. Mono batch cameras use LateUpdate below; normal sessions
            // and stereo retain the provider-timed before-render path.
            Camera camera = Cam;
            if (Application.isBatchMode && camera != null && !camera.stereoEnabled)
                return;
            RenderMirror();
        }

        private void LateUpdate()
        {
            if (!Application.isBatchMode || GraphicsSettings.currentRenderPipeline == null)
                return;
            Camera camera = Cam;
            if (camera != null && !camera.stereoEnabled)
                RenderMirror();
        }

        // Read-only native identity association for scoped camera rendering.
        // Hidden reflection cameras are separate roots; their source camera
        // supplies the owning app without names or hierarchy changes.
        public bool TryGetSourceCamera(Camera reflectionCamera, out Camera sourceCamera)
        {
            sourceCamera = null;
            if (reflectionCamera == null)
                return false;
            foreach (DictionaryEntry entry in m_ReflectionCameras)
            {
                if (entry.Value is Camera created && created == reflectionCamera &&
                    entry.Key is Camera original && original != null)
                {
                    sourceCamera = original;
                    return true;
                }
            }
            return false;
        }

        public void OnWillRenderObject()
        {
            if (GraphicsSettings.currentRenderPipeline == null)
                RenderMirror();
        }

        private void RenderMirror()
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
            // OnBeforeRender is not a renderer-visibility callback. Preserve
            // the source camera's layer exclusion before allocating/rendering.
            if ((cam.cullingMask & (1 << m_Renderer.gameObject.layer)) == 0)
                return;

            int oldPixelLightCount = QualitySettings.pixelLightCount;
            s_InsideRendering = true;
            try
            {
                var stereo = cam.stereoEnabled;
                EyeMatrices eyes = default;
                // No eye passes this frame (session not yet visible, headset
                // idle, unfocused editor): keep the previous reflection.
                if (stereo && !TryReadEyeMatrices(cam, out eyes))
                    return;

                Camera reflectionCamera;
                CreateMirrorObjects(cam, stereo, out reflectionCamera);

                // find out the reflection plane: position and normal in world space
                Vector3 pos = transform.position;
                Vector3 normal = transform.up + Offset;

                // Optionally disable pixel lights for reflection
                if (m_DisablePixelLights)
                    QualitySettings.pixelLightCount = 0;

                UpdateCameraModes(cam, reflectionCamera);

                // Reflect camera around reflection plane
                float d = -Vector3.Dot(normal, pos) - m_ClipPlaneOffset;
                Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);

                Matrix4x4 reflection = Matrix4x4.zero;
                CalculateReflectionMatrix(ref reflection, reflectionPlane);

                if (stereo)
                {
                    RenderReflection(reflectionCamera,
                        eyes.leftView,
                        eyes.leftProjection,
                        reflection, pos, normal, m_ReflectionTextureLeft);

                    RenderReflection(reflectionCamera,
                        eyes.rightView,
                        eyes.rightProjection,
                        reflection, pos, normal, m_ReflectionTextureRight);
                }
                else
                {
                    RenderReflection(reflectionCamera,
                        cam.worldToCameraMatrix,
                        cam.projectionMatrix,
                        reflection, pos, normal, m_ReflectionTextureLeft);
                }

                Material[] materials = m_Renderer.sharedMaterials;
                var materialsWithReflectionTex = 0;
                foreach (Material mat in materials)
                {
                    if (mat.HasProperty(s_reflectionTex))
                    {
                        mat.SetTexture(s_reflectionTex, m_ReflectionTextureLeft);
                        materialsWithReflectionTex++;
                    }

                    if (mat.HasProperty(s_reflectionTexRight))
                        mat.SetTexture(s_reflectionTexRight, stereo ? m_ReflectionTextureRight : m_ReflectionTextureLeft);
                }

                if (m_RenderLogCount < MaxRenderLogs)
                {
                    m_RenderLogCount++;
                    Debug.Log($"[MirrorReflection] Rendered reflection #{m_RenderLogCount} on '{name}' | cam: {cam.name} @ {cam.transform.position} | stereo: {stereo} | texture: {m_ReflectionTextureLeft.width}px created: {m_ReflectionTextureLeft.IsCreated()} | materials with _ReflectionTex: {materialsWithReflectionTex}/{materials.Length}");
                }

            }
            finally
            {
                if (m_DisablePixelLights)
                    QualitySettings.pixelLightCount = oldPixelLightCount;
                s_InsideRendering = false;
            }
        }

        // Renders the scene reflected around the plane, from a single eye's point of view.
        // eyeView/eyeProjection must be the matrices the main render actually uses for that
        // eye - that is what makes the screen-space sampling in the shader line up exactly.
        private void RenderReflection(Camera reflectionCamera, Matrix4x4 eyeView, Matrix4x4 eyeProjection,
            Matrix4x4 reflection, Vector3 planePos, Vector3 planeNormal, RenderTexture target)
        {
            // Unity6.7 can log and return from a null-destination request
            // without throwing. Reject it before mutating any render state.
            if (!target)
                throw new System.ArgumentNullException(nameof(target));
            RenderTexture oldTargetTexture = reflectionCamera.targetTexture;
            RenderTexture oldActiveRenderTexture = RenderTexture.active;
            CameraType oldCameraType = reflectionCamera.cameraType;
            bool oldInvertCulling = GL.invertCulling;
            try
            {
                reflectionCamera.worldToCameraMatrix = eyeView * reflection;

                // Setup oblique projection matrix so that near plane is our reflection
                // plane. This way we clip everything below/above it for free.
                Vector4 clipPlane = CameraSpacePlane(reflectionCamera, planePos, planeNormal, 1.0f);
                reflectionCamera.projectionMatrix = eyeProjection;
                reflectionCamera.projectionMatrix = reflectionCamera.CalculateObliqueMatrix(clipPlane);

                reflectionCamera.cullingMask = ~(1 << 4) & m_ReflectLayers.value; // never render water layer
                reflectionCamera.targetTexture = target;

                // Mirror the eye position across the plane so _WorldSpaceCameraPos is correct
                // in the reflection render.
                Vector3 eyePos = eyeView.inverse.GetColumn(3);
                reflectionCamera.transform.position = reflection.MultiplyPoint(eyePos);

                GL.invertCulling = true;
                if (GraphicsSettings.currentRenderPipeline == null)
                {
                    reflectionCamera.Render();
                }
                else
                {
                    // This camera is created here, has no camera stack, and
                    // renders one mono eye into one ordinary 2D texture. URP's
                    // StandardRequest therefore renders only this Base camera;
                    // it also avoids a new URP assembly dependency for SDK users.
                    var request = new RenderPipeline.StandardRequest { destination = target };
                    if (!RenderPipeline.SupportsRenderRequest(reflectionCamera, request))
                        throw new System.NotSupportedException("The active render pipeline does not support the SDK mirror request.");
                    RenderPipeline.SubmitRenderRequest(reflectionCamera, request);
                }
            }
            finally
            {
                GL.invertCulling = oldInvertCulling;
                reflectionCamera.targetTexture = oldTargetTexture;
                reflectionCamera.cameraType = oldCameraType;
                RenderTexture.active = oldActiveRenderTexture;
            }
        }

        private struct EyeMatrices
        {
            public Matrix4x4 leftView, leftProjection, rightView, rightProjection;
        }

        // Returns false when the display has nothing to render this frame;
        // throws only for layouts the mirror genuinely cannot support.
        private bool TryReadEyeMatrices(Camera camera, out EyeMatrices eyes)
        {
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                eyes = new EyeMatrices
                {
                    leftView = camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left),
                    leftProjection = camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left),
                    rightView = camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right),
                    rightProjection = camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right)
                };
                return true;
            }

            eyes = default;
            SubsystemManager.GetSubsystems(m_XRDisplays);
            if (m_XRDisplays.Count > 1)
                throw new System.NotSupportedException("Stereo mirror requires exactly one registered XR display.");
            if (m_XRDisplays.Count == 0 || !m_XRDisplays[0].running)
                return false;

            var display = m_XRDisplays[0];
            int passCount = display.GetRenderPassCount();
            if (passCount == 0)
                return false;
            XRDisplaySubsystem.XRRenderParameter left, right;
            if (passCount == 1)
            {
                display.GetRenderPass(0, out var pass);
                if (pass.GetRenderParameterCount() != 2)
                    throw new System.NotSupportedException("Stereo mirror requires two ordinary eye parameters.");
                pass.GetRenderParameter(camera, 0, out left);
                pass.GetRenderParameter(camera, 1, out right);
            }
            else if (passCount == 2)
            {
                display.GetRenderPass(0, out var leftPass);
                display.GetRenderPass(1, out var rightPass);
                if (leftPass.GetRenderParameterCount() != 1 || rightPass.GetRenderParameterCount() != 1)
                    throw new System.NotSupportedException("Stereo mirror does not support incomplete or quad-view layouts.");
                leftPass.GetRenderParameter(camera, 0, out left);
                rightPass.GetRenderParameter(camera, 0, out right);
            }
            else
            {
                throw new System.NotSupportedException("Stereo mirror requires one two-eye pass or two single-eye passes.");
            }

            // Preserve provider pass/parameter order. Both multipass eyes may
            // use texture array slice zero, so sorting by slice loses eye order.
            eyes = new EyeMatrices
            {
                leftView = left.view, leftProjection = left.projection,
                rightView = right.view, rightProjection = right.projection
            };
            return true;
        }

        // Cleanup all the objects we possibly have created
        void OnDisable()
        {
            Application.onBeforeRender -= OnBeforeRender;
            if (m_ReflectionTextureLeft)
            {
                DestroyImmediate(m_ReflectionTextureLeft);
                m_ReflectionTextureLeft = null;
            }

            if (m_ReflectionTextureRight)
            {
                DestroyImmediate(m_ReflectionTextureRight);
                m_ReflectionTextureRight = null;
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
        private void CreateMirrorObjects(Camera currentCamera, bool stereo, out Camera reflectionCamera)
        {
            reflectionCamera = null;

            // Reflection render textures
            if (!m_ReflectionTextureLeft || m_OldReflectionTextureSize != m_TextureSize)
            {
                if (m_ReflectionTextureLeft)
                    DestroyImmediate(m_ReflectionTextureLeft);
                m_ReflectionTextureLeft = CreateReflectionTexture("L");
                m_OldReflectionTextureSize = m_TextureSize;
            }

            if (stereo && (!m_ReflectionTextureRight || m_ReflectionTextureRight.width != m_TextureSize))
            {
                if (m_ReflectionTextureRight)
                    DestroyImmediate(m_ReflectionTextureRight);
                m_ReflectionTextureRight = CreateReflectionTexture("R");
            }

            RenderTexture CreateReflectionTexture(string eye)
            {
                var rt = new RenderTexture(m_TextureSize, m_TextureSize, 16);
                rt.name = "__MirrorReflection" + eye + GetEntityId();
                rt.isPowerOfTwo = true;
                rt.hideFlags = HideFlags.DontSave;
                return rt;
            }

            // Camera for reflection
            reflectionCamera = m_ReflectionCameras[currentCamera] as Camera;
            if (!reflectionCamera) // catch both not-in-dictionary and in-dictionary-but-deleted-GO
            {
                GameObject go = new GameObject("Mirror Refl Camera id" + GetEntityId() + " for " + currentCamera.GetEntityId(), typeof(Camera), typeof(Skybox));
                reflectionCamera = go.GetComponent<Camera>();
                reflectionCamera.enabled = false;
                // Each call supplies one eye's view/projection explicitly.
                // The reflection render must not invoke XR again on this camera.
                reflectionCamera.stereoTargetEye = StereoTargetEyeMask.None;
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
