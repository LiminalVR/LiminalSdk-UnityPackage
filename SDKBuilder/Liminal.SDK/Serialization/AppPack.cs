using System.Collections.Generic;
using System.Text;

namespace Liminal.SDK.Serialization
{
    /// <summary>
    /// A class representing the Liminal AppPack file format.
    /// </summary>
    public class AppPack
    {
        #region Static

        private static readonly byte[] _identifier = Encoding.ASCII.GetBytes("LIMAPP");

        /// <summary>
        /// The current version number for AppPack files.
        /// </summary>
        /// VERSION 1 - Original
        /// VERSION 2 - Add ApplicationVersion
        public const ushort Version = 2;

        /// <summary>
        /// The file identifier for AppPack files.
        /// </summary>
        public static byte[] Identifier
        {
            get { return _identifier; }
        }

        #endregion

        /// <summary>
        /// The target platform the application is built for.
        /// </summary>
        public AppPackPlatform TargetPlatform { get; set; }

        /// <summary>
        /// The id of the application packed into the AppPack.
        /// </summary>
        public int ApplicationId { get; set; }

        public int ApplicationVersion { get; set; }

        /// <summary>
        /// The method of compression used in the AppPack.
        /// </summary>
        public ECompressionType CompressionType { get; set; }

        /// <summary>
        /// The raw byte array for the application assembly.
        /// </summary>
        public List<byte[]> Assemblies { get; set; }

        /// <summary>
        /// The raw byte array for the application scene bundle.
        /// </summary>
        public byte[] SceneBundle { get; set; }
    }
}
