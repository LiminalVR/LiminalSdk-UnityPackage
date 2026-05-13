using Liminal.SDK.Build;

[System.Serializable]
public class BuildWindowConfig
{
    public string TargetScene = "";
    public BuildPlatform SelectedPlatform = BuildPlatform.Current;
}
