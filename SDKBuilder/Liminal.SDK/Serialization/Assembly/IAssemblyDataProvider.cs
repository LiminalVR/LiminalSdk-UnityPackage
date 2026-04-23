namespace Liminal.SDK.Serialization
{
    /// <summary>
    /// An interface for providing assembly information to the serialization framework.
    /// </summary>
    public interface IAssemblyDataProvider
    {
        /// <summary>
        /// Gets the name of the assembly.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the full name of the assembly.
        /// </summary>
        string FullName { get; }
    }
}
