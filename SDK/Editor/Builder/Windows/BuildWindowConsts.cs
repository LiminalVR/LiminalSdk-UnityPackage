public static class BuildWindowConsts
{
    /// <summary>
    /// Path to limapp builds
    /// </summary>
    public const string BuildPath = "Assets/_Builds";

    /// <summary>
    /// Path to config folder
    /// </summary>
    public const string ConfigFolderPath = BuildPath + "/Config";

    /// <summary>
    /// Path to Build Window Configuration
    /// </summary>
    public const string BuildWindowConfigPath = ConfigFolderPath + "/BuildWindowConfig.json";

    /// <summary>
    /// A resources folder for Liminal SDK assets. This is mainly a workaround for third party frameworks needing a resources folder.
    /// </summary>
    public const string ResourcesFolder = "Liminal/Resources";
}
