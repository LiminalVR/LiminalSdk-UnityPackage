using UnityEngine;

namespace Liminal.SDK.PostFX
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(Camera))]
	public class ColorGradingEffect : MonoBehaviour
	{
		[SerializeField] private Shader _shader;
		[SerializeField][Range(0f, 2f)] private float _brightness = 1f;
		[SerializeField][Range(0f, 2f)] private float _contrast = 1f;
		[SerializeField][Range(-180f, 180f)] private float _hueShift = 0f;

		private Material _material;

		private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
		private static readonly int ContrastId = Shader.PropertyToID("_Contrast");
		private static readonly int HueShiftId = Shader.PropertyToID("_HueShift");

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

		protected void OnEnable()
		{
			if (_shader == null)
				_shader = Shader.Find("Liminal/PostFX/ColorGrading");

			if (_shader != null && _material == null)
				_material = new Material(_shader) { hideFlags = HideFlags.HideAndDontSave };
		}

		protected void OnDisable()
		{
			if (_material != null)
			{
				DestroyImmediate(_material);
				_material = null;
			}
		}

		protected void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			if (_material == null)
			{
				Graphics.Blit(source, destination);
				return;
			}

			_material.SetFloat(BrightnessId, _brightness);
			_material.SetFloat(ContrastId, _contrast);
			_material.SetFloat(HueShiftId, _hueShift);
			Graphics.Blit(source, destination, _material);
		}
	}
}
