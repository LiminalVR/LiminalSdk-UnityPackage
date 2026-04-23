namespace Liminal.SDK.Serialization
{
    /// <summary>
    /// The platforms Liminal AppPack files can support.
    /// </summary>
    public enum AppPackPlatform : ushort
    {
        /// <summary>
        /// Unknown platform. If this value is detected or used, an exception should be thrown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Windows standalone platform.
        /// </summary>
        WindowsStandalone = 1,

        /// <summary>
        /// Android platform.
        /// </summary>
        Android = 2,
    }
}
