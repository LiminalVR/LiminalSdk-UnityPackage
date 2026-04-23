using Liminal.SDK.Core;
using UnityEngine;
using UnityEngine.Assertions;

namespace Liminal.Core.Fader
{
    /// <summary>
    /// ScreenFader implementation adapted from Oculus's OVRScreenFade 
    /// </summary>
    public class MeshScreenFader : ScreenFaderBase
    {
        public Material CustomFaderMaterial;
        private Material mFadeMaterial;
        private MeshFilter mMesh;
        private MeshRenderer mFadeRenderer;		
		private Camera mCamera;

        public const string ShaderName = "Liminal/Screen Fade";
        private bool Initialized = false;

        private void Initialize()
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogErrorFormat("MeshScreenFader was unable to find required shader \"{0}\". " +
                                     "No references in project, or not included in Resources folder?", ShaderName);
            }

            mFadeMaterial = CustomFaderMaterial == null ? new Material(shader) : CustomFaderMaterial;

            mMesh = gameObject.AddComponent<MeshFilter>();
            mFadeRenderer = gameObject.AddComponent<MeshRenderer>();

            var mesh = new Mesh();
            mMesh.mesh = mesh;

            Vector3[] vertices = new Vector3[4];

            float width = 2f;
            float height = 2f;
            float depth = 1f;

            vertices[0] = new Vector3(-width, -height, depth);
            vertices[1] = new Vector3(width, -height, depth);
            vertices[2] = new Vector3(-width, height, depth);
            vertices[3] = new Vector3(width, height, depth);

            mesh.vertices = vertices;

            int[] tri = new int[6];

            tri[0] = 0;
            tri[1] = 2;
            tri[2] = 1;

            tri[3] = 2;
            tri[4] = 3;
            tri[5] = 1;

            mesh.triangles = tri;

            Vector3[] normals = new Vector3[4];

            normals[0] = -Vector3.forward;
            normals[1] = -Vector3.forward;
            normals[2] = -Vector3.forward;
            normals[3] = -Vector3.forward;

            mesh.normals = normals;

            Vector2[] uv = new Vector2[4];

            uv[0] = new Vector2(0, 0);
            uv[1] = new Vector2(1, 0);
            uv[2] = new Vector2(0, 1);
            uv[3] = new Vector2(1, 1);

            mesh.uv = uv;
            mCamera = GetComponent<Camera>();

            Initialized = true;
        }

        protected override void OnAwake()
        {
            if (!Initialized)
                Initialize();

            ApplyColor(CurrentColor);
		}

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                if (mFadeRenderer != null)
                    Destroy(mFadeRenderer);

                if (mFadeMaterial != null)
                    Destroy(mFadeMaterial);

                if (mMesh != null)
                    Destroy(mMesh);
            }
        }

        protected override void OnValidate()
        {
            // Prevent call to base.OnValidate() as ApplyColor() won't work before OnAwake()
            // Don't want to setup the meshes etc as this messes with prefabs
        }

        protected override void ApplyColor(Color color)
        {
            if (!Initialized)
                Initialize();

            if (mFadeMaterial != null)
            {
                mFadeMaterial.color = color;
                mFadeMaterial.renderQueue = LiminalRenderQueue.Fader;
                mFadeRenderer.material = mFadeMaterial;
                mFadeRenderer.enabled = color.a > 0;
            }
        }
	}
}