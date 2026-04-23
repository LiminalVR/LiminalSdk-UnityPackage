using Liminal.SDK.VR.Input;
using System;
using UnityEngine;

namespace Liminal.SDK.VR.Avatars
{
    /// <summary>
    /// A collection of helper methods for working with the VRAvatar system.
    /// </summary>
    public static class VRAvatarHelper
    {
        /// <summary>
        /// Ensures the specified prefab exists, and returns the prefab. An exception will be thrown if the prefab does not exist.
        /// </summary>
        /// <typeparam name="T">The type of the prefab.</typeparam>
        /// <param name="name">The name of the prefab.</param>
        /// <returns>The prefab of the specified type and name.</returns>
        public static T EnsureLoadPrefab<T>(string name) where T : UnityEngine.Object
        {
            var prefab = Resources.Load<T>(name);
            if (prefab == null)
            {
                throw new Exception(string.Format("No {0} prefab found: {1}", typeof(T).Name, name));
            }

            return prefab;
        }
    }
}
