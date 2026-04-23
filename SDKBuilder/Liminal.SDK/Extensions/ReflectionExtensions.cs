using System;
using System.Reflection;

namespace Liminal.SDK.Extensions
{
    /// <summary>
    /// A collection of extension methods for working with reflection.
    /// </summary>
    public static class ReflectionExtensions
    {
        /// <summary>
        /// Gets the the assembly with the specified name, if it is loaded into the AppDomain.
        /// </summary>
        /// <param name="appDomain">The AppDomain to retrieve the Assembly from.</param>
        /// <param name="name">The name of the Assembly to retrieve.</param>
        /// <returns>The Assembly with the specified name, or null if no assembly with the supplied name is loaded into the AppDomain.</returns>
        public static Assembly GetLoadedAssembly(this AppDomain appDomain, string name)
        {
            foreach (Assembly asm in appDomain.GetAssemblies())
            {
                if (asm.GetName().Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                {
                    return asm;
                }
            }

            return null;
        }
    }
}
