using System;
using System.Reflection;

namespace Liminal.SDK.Serialization
{
    /// <summary>
    /// Provides assembly name information.
    /// </summary>
    public class AssemblyDataProvider : IAssemblyDataProvider
    {
        private string mName;
        private string mFullName;

        #region Properties

        /// <summary>
        /// Gets the name of the assembly.
        /// </summary>
        public string Name
        {
            get { return mName; }
        }
        
        /// <summary>
        /// Gets the full name of the assembly.
        /// </summary>
        public string FullName
        {
            get { return mFullName; }
        }

        #endregion

        /// <summary>
        /// Creates an <see cref="AssemblyDataProvider"/> with a specific assembly name.
        /// </summary>
        /// <param name="name">The assembly name.</param>
        public AssemblyDataProvider(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            mName = name;
            mFullName = (mName + ", Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
        }

        /// <summary>
        /// Creates an <see cref="AssemblyDataProvider"/> with a specific assembly.
        /// </summary>
        /// <param name="asm">The assembly.</param>
        public AssemblyDataProvider(Assembly asm)
        {
            if (asm == null)
                throw new ArgumentNullException("asm");

            mName = asm.GetName().Name;
            mFullName = asm.FullName;
        }
    }
}
