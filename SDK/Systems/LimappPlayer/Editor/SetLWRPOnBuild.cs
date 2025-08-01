#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

public class SetLWRPOnBuild : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPostprocessBuild(BuildReport report)
    {
        return;
        GraphicsSettings.defaultRenderPipeline = null;
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        return;
        string targetAssetName = "LWRPAsset"; // without .asset extension

        // Search for all RenderPipelineAssets
        string[] guids = AssetDatabase.FindAssets("t:RenderPipelineAsset");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);

            if (fileName == targetAssetName)
            {
                var pipelineAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(path);
                if (pipelineAsset != null)
                {
                    GraphicsSettings.defaultRenderPipeline = pipelineAsset;
                    Debug.Log($"Render Pipeline set to: {path} for build.");
                    return;
                }
            }
        }

        Debug.LogError($"Render Pipeline Asset named '{targetAssetName}' not found.");
    }

}
#endif