using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Rendering;
#endif

namespace Liminal.SDK.PostFX
{
	/// <summary>
	/// Camera color grading (brightness / contrast / hue / saturation) that
	/// stays compatible with Fixed Foveated Rendering on Quest.
	///
	/// On Android device builds the grade is drawn as an end-of-frame
	/// fullscreen triangle using framebuffer fetch (ColorGradingFetch.shader),
	/// so the camera keeps rendering directly into the foveated XR swapchain.
	/// An OnRenderImage method — even an empty one — forces Unity to render
	/// the scene into an unfoveated intermediate RenderTexture, which is why
	/// that path is compiled out of Android players entirely and only used
	/// for editor preview and PC builds, where FFR does not apply.
	/// </summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(Camera))]
	public class ColorGradingEffect : MonoBehaviour
	{
		[SerializeField] private Shader _shader;
		[SerializeField] private Shader _fetchShader;
		[SerializeField][Range(0f, 2f)] private float _brightness = 1f;
		[SerializeField][Range(0f, 2f)] private float _contrast = 1f;
		[SerializeField][Range(-180f, 180f)] private float _hueShift = 0f;
		[SerializeField][Range(0f, 2f)] private float _saturation = 1f;

		private Material _material;

#if UNITY_ANDROID && !UNITY_EDITOR
		private GameObject _fullscreenPass;
		private Mesh _mesh;
#endif

		private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
		private static readonly int ContrastId = Shader.PropertyToID("_Contrast");
		private static readonly int HueShiftId = Shader.PropertyToID("_HueShift");
		private static readonly int SaturationId = Shader.PropertyToID("_Saturation");

		public float Brightness
		{
			get => _brightness;
			set => _brightness = Mathf.Clamp(value, 0f, 2f);
		}

		public float Contrast
		{
			get => _contrast;
			set => _contrast = Mathf.Clamp(value, 0f, 2f);
		}

		public float HueShift
		{
			get => _hueShift;
			set => _hueShift = Mathf.Clamp(value, -180f, 180f);
		}

		public float Saturation
		{
			get => _saturation;
			set => _saturation = Mathf.Clamp(value, 0f, 2f);
		}

		protected void OnEnable()
		{
			if (_shader == null)
				_shader = Shader.Find("Liminal/PostFX/ColorGrading");

			if (_fetchShader == null)
				_fetchShader = Shader.Find("Liminal/PostFX/ColorGradingFetch");

#if UNITY_ANDROID && !UNITY_EDITOR
			if (_fetchShader == null)
			{
				Debug.LogWarning("[ColorGradingEffect] ColorGradingFetch shader not found — re-save the scene with the current SDK so the reference is baked into the build. Color grading disabled.");
				return;
			}

			if (_material == null)
				_material = new Material(_fetchShader) { hideFlags = HideFlags.HideAndDontSave };

			if (_fullscreenPass == null)
				CreateFullscreenPass();
#else
			if (_shader != null && _material == null)
				_material = new Material(_shader) { hideFlags = HideFlags.HideAndDontSave };
#endif
		}

		protected void OnDisable()
		{
			if (_material != null)
			{
				DestroyImmediate(_material);
				_material = null;
			}

#if UNITY_ANDROID && !UNITY_EDITOR
			if (_fullscreenPass != null)
			{
				Destroy(_fullscreenPass);
				_fullscreenPass = null;
			}

			if (_mesh != null)
			{
				Destroy(_mesh);
				_mesh = null;
			}
#endif
		}

#if UNITY_EDITOR
		protected void OnValidate()
		{
			// Bake the shader references into the scene so limapp builds
			// include them (Shader.Find alone won't pull them into a bundle).
			if (_shader == null)
				_shader = Shader.Find("Liminal/PostFX/ColorGrading");

			if (_fetchShader == null)
				_fetchShader = Shader.Find("Liminal/PostFX/ColorGradingFetch");
		}
#endif

		private void ApplyProperties(Material material)
		{
			material.SetFloat(BrightnessId, _brightness);
			material.SetFloat(ContrastId, _contrast);
			material.SetFloat(HueShiftId, _hueShift);
			material.SetFloat(SaturationId, _saturation);
		}

#if UNITY_ANDROID && !UNITY_EDITOR
		protected void LateUpdate()
		{
			if (_material != null)
				ApplyProperties(_material);
		}

		private void CreateFullscreenPass()
		{
			// Vertices are authored in clip space; the fetch shader passes xy
			// through untouched, so the transform never matters. The oversized
			// bounds keep frustum culling from ever rejecting the renderer.
			_mesh = new Mesh
			{
				vertices = new[]
				{
					new Vector3(-1f, -1f, 0f),
					new Vector3(3f, -1f, 0f),
					new Vector3(-1f, 3f, 0f),
				},
				triangles = new[] { 0, 1, 2 },
				bounds = new Bounds(Vector3.zero, Vector3.one * 100000f),
			};

			_fullscreenPass = new GameObject("ColorGradeFullscreenPass") { hideFlags = HideFlags.HideAndDontSave };
			_fullscreenPass.transform.SetParent(transform, false);
			_fullscreenPass.layer = FirstVisibleLayer();

			var filter = _fullscreenPass.AddComponent<MeshFilter>();
			filter.sharedMesh = _mesh;

			var meshRenderer = _fullscreenPass.AddComponent<MeshRenderer>();
			meshRenderer.sharedMaterial = _material;
			meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
			meshRenderer.receiveShadows = false;
			meshRenderer.lightProbeUsage = LightProbeUsage.Off;
			meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
			meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
			meshRenderer.allowOcclusionWhenDynamic = false;
		}

		private int FirstVisibleLayer()
		{
			var mask = GetComponent<Camera>().cullingMask;
			if ((mask & (1 << gameObject.layer)) != 0)
				return gameObject.layer;

			for (var i = 0; i < 32; i++)
			{
				if ((mask & (1 << i)) != 0)
					return i;
			}

			return gameObject.layer;
		}
#else
		protected void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			if (_material == null)
			{
				Graphics.Blit(source, destination);
				return;
			}

			ApplyProperties(_material);
			Graphics.Blit(source, destination, _material);
		}
#endif
	}
}
