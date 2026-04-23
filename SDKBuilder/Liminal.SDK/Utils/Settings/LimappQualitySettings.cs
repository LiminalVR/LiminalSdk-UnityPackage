using UnityEngine;

[System.Serializable]
public class LimappQualitySettings : LimappSettings
{
    public float LodBias;
    public int MaximumLODLevel;
    public int ParticleRaycastBudget;
    public bool SoftParticles;
    public bool SoftVegetation;
    public bool RealtimeReflectionProbes;
    public bool BillboardsFaceCameraPosition;
    public int MaxQueuedFrames;
    public int VSyncCount;
    public int AntiAliasing;
    public SkinWeights BlendWeights;
    public int AsyncUploadTimeSlice;
    public int AsyncUploadBufferSize;
    public AnisotropicFiltering AnisotropicFiltering;
    public int MasterTextureLimit;
    public ShadowmaskMode ShadowmaskMode;
    public Vector3 ShadowCascade4Split;
    public float ResolutionScalingFixedDPIFactor;
    public int PixelLightCount;
    public ShadowQuality Shadows;
    public ShadowProjection ShadowProjection;
    public float ShadowDistance;
    public ShadowResolution ShadowResolution;
    public float ShadowNearPlaneOffset;
    public float ShadowCascade2Split;
    public int ShadowCascades;
    public int QualityLevel;

    public override void SaveSettings()
    {
        LodBias = UnityEngine.QualitySettings.lodBias;
        MaximumLODLevel = UnityEngine.QualitySettings.maximumLODLevel;
        ParticleRaycastBudget = UnityEngine.QualitySettings.particleRaycastBudget;
        SoftParticles = UnityEngine.QualitySettings.softParticles;
        SoftVegetation = UnityEngine.QualitySettings.softVegetation;
        RealtimeReflectionProbes = UnityEngine.QualitySettings.realtimeReflectionProbes;
        BillboardsFaceCameraPosition = UnityEngine.QualitySettings.billboardsFaceCameraPosition;
        MaxQueuedFrames = UnityEngine.QualitySettings.maxQueuedFrames;
        VSyncCount = UnityEngine.QualitySettings.vSyncCount;
        AntiAliasing = UnityEngine.QualitySettings.antiAliasing;
        BlendWeights = UnityEngine.QualitySettings.skinWeights;
        AsyncUploadTimeSlice = UnityEngine.QualitySettings.asyncUploadTimeSlice;
        AsyncUploadBufferSize = UnityEngine.QualitySettings.asyncUploadBufferSize;
        AnisotropicFiltering = UnityEngine.QualitySettings.anisotropicFiltering;
        MasterTextureLimit = UnityEngine.QualitySettings.globalTextureMipmapLimit;
        ShadowmaskMode = UnityEngine.QualitySettings.shadowmaskMode;
        ShadowCascade4Split = UnityEngine.QualitySettings.shadowCascade4Split;
        ResolutionScalingFixedDPIFactor = UnityEngine.QualitySettings.resolutionScalingFixedDPIFactor;
        PixelLightCount = UnityEngine.QualitySettings.pixelLightCount;
        Shadows = UnityEngine.QualitySettings.shadows;
        ShadowProjection = UnityEngine.QualitySettings.shadowProjection;
        ShadowDistance = UnityEngine.QualitySettings.shadowDistance;
        ShadowResolution = UnityEngine.QualitySettings.shadowResolution;
        ShadowNearPlaneOffset = UnityEngine.QualitySettings.shadowNearPlaneOffset;
        ShadowCascade2Split = UnityEngine.QualitySettings.shadowCascade2Split;
        ShadowCascades = UnityEngine.QualitySettings.shadowCascades;
    }

    public override void ApplySettings()
    {
        UnityEngine.QualitySettings.lodBias = LodBias;
        UnityEngine.QualitySettings.maximumLODLevel = MaximumLODLevel;
        UnityEngine.QualitySettings.particleRaycastBudget = ParticleRaycastBudget;
        UnityEngine.QualitySettings.softParticles = SoftParticles;
        UnityEngine.QualitySettings.softVegetation = SoftVegetation;
        UnityEngine.QualitySettings.realtimeReflectionProbes = RealtimeReflectionProbes;
        UnityEngine.QualitySettings.billboardsFaceCameraPosition = BillboardsFaceCameraPosition;
        UnityEngine.QualitySettings.maxQueuedFrames = MaxQueuedFrames;
        UnityEngine.QualitySettings.vSyncCount = VSyncCount;
        UnityEngine.QualitySettings.antiAliasing = AntiAliasing;
        UnityEngine.QualitySettings.skinWeights = BlendWeights;
        UnityEngine.QualitySettings.asyncUploadTimeSlice = AsyncUploadTimeSlice;
        UnityEngine.QualitySettings.asyncUploadBufferSize = AsyncUploadBufferSize;
        UnityEngine.QualitySettings.anisotropicFiltering = AnisotropicFiltering;
        UnityEngine.QualitySettings.globalTextureMipmapLimit = MasterTextureLimit;
        UnityEngine.QualitySettings.shadowmaskMode = ShadowmaskMode;
        UnityEngine.QualitySettings.shadowCascade4Split = ShadowCascade4Split;
        UnityEngine.QualitySettings.resolutionScalingFixedDPIFactor = ResolutionScalingFixedDPIFactor;
        UnityEngine.QualitySettings.pixelLightCount = PixelLightCount;
        UnityEngine.QualitySettings.shadows = Shadows;
        UnityEngine.QualitySettings.shadowProjection = ShadowProjection;
        UnityEngine.QualitySettings.shadowDistance = ShadowDistance;
        UnityEngine.QualitySettings.shadowResolution = ShadowResolution;
        UnityEngine.QualitySettings.shadowNearPlaneOffset = ShadowNearPlaneOffset;
        UnityEngine.QualitySettings.shadowCascade2Split = ShadowCascade2Split;
        UnityEngine.QualitySettings.shadowCascades = ShadowCascades;
    }
}