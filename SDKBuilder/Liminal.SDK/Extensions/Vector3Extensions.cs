using UnityEngine;

namespace Liminal.SDK.Extensions
{
    public static class Vector3Extensions
    {
        /// <summary>
        /// Returns a new Vector3 that contains only the X and Z components of the original Vector3.
        /// </summary>
        /// <param name="vector">The original Vector3.</param>
        /// <returns>A new Vector3 that contains only the X and Z components of the input Vector3</returns>
        public static Vector3 GetXZ(this Vector3 vector)
        {
            return new Vector3(vector.x, 0, vector.z);
        }

        /// <summary>
        /// Returns a new, normalized Vector3 that contains only the X and Z components of the original Vector3.
        /// </summary>
        /// <param name="vector">The original Vector3.</param>
        /// <returns>A new, normalized Vector3 that contains only the X and Z components of the input Vector3</returns>
        public static Vector3 GetXZNormalized(this Vector3 vector)
        {
            var output = new Vector3(vector.x, 0, vector.z);
            output.Normalize();

            return output;
        }
    }
}