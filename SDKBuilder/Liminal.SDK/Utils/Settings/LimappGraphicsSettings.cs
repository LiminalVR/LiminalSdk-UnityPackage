using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class LimappGraphicsSettings : LimappSettings
{
    public RenderPipelineAsset ScriptableRenderPipelineSettings;
    public TransparencySortMode TransparencySortMode;
    public Vector3 TransparencySortAxis;

    public override void SaveSettings()
    {
        ScriptableRenderPipelineSettings = GraphicsSettings.renderPipelineAsset;
        TransparencySortMode = GraphicsSettings.transparencySortMode;
        TransparencySortAxis = GraphicsSettings.transparencySortAxis;
    }

    public override void ApplySettings()
    {
        GraphicsSettings.renderPipelineAsset = ScriptableRenderPipelineSettings;
        GraphicsSettings.transparencySortMode = TransparencySortMode;
        GraphicsSettings.transparencySortAxis = TransparencySortAxis;
    }
}
