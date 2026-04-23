using System;
using UnityEngine;

namespace Liminal.SDK.Extensions
{
    public static class TransformExtensions
    {
        /// <summary>
        /// Resets the transform to its identity state, where <see cref="Transform.localPosition"/> is equal to <see cref="Vector3.zero"/>, <see cref="Transform.localRotation"/> is equal to
        /// <see cref="Quaternion.identity"/> and <see cref="Transform.localScale"/> is equal to <see cref="Vector3.one"/>.
        /// </summary>
        /// <param name="transform">The transform to reset to its identity state.</param>
        public static void Identity(this Transform transform)
        {
            if (transform == null)
                return;

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Sets the parent of the transform and resets it to its identity state once it has been reparented. This is synonymous with calling <see cref="Transform.SetParent(Transform)"/> followed
        /// by <see cref="Identity(Transform)"/>.
        /// </summary>
        /// <param name="transform">The transform to reparent.</param>
        /// <param name="parent">The new parent of <paramref name="transform"/></param>
        public static void SetParentAndIdentity(this Transform transform, Transform parent)
        {
            if (transform == null)
                return;

            transform.SetParent(parent);
            transform.Identity();
        }

        /// <summary>
        /// Returns the component of the specified type on the transform, or if the component does not exist, adds it to the transform's GameObject and then returns the new component.
        /// </summary>
        /// <typeparam name="T">The component type</typeparam>
        /// <param name="transform">The transform to retrieve the component from or add the component to.</param>
        /// <returns>The existing component of the specified type on <paramref name="transform"/>, or the newly created component if one does not already exist.</returns>
        public static T GetOrAddComponent<T>(this Transform transform) where T : Component
        {
            if (transform == null)
                throw new ArgumentNullException("transform");
            
            var component = transform.GetComponent<T>();
            if (component == null)
                component = transform.gameObject.AddComponent<T>();

            return component;
        }

        /// <summary>
        /// Returns the component of the specified type on the transform, or if the component does not exist, adds it to the transform's GameObject and then returns the new component.
        /// </summary>
        /// <param name="transform">The transform to retrieve the component from or add the component to.</param>
        /// <param name="type">The type of the component to retrieve and/or add.</type>
        /// <returns>The component of the specified type.</returns>
        public static Component GetOrAddComponent(this Transform transform, Type type)
        {
            if (transform == null)
                throw new ArgumentNullException("transform");

            if (type == null)
                throw new ArgumentNullException("type");

            var component = transform.GetComponent(type);
            if (component == null)
                component = transform.gameObject.AddComponent(type);

            return component;
        }

        /// <summary>
        /// Returns a boolean value indicating if the specified Transform has a component of the specified type.
        /// </summary>
        /// <typeparam name="T">The component type</typeparam>
        /// <param name="transform">The transform to operate on.</param>
        /// <returns>A boolean value indicating if the supplied <see cref="Transform"/> has a component of the specified type.</returns>
        public static bool HasComponent<T>(this Transform transform)
        {
            if (transform == null)
                throw new ArgumentNullException("transform");

            return (transform.GetComponent<T>() != null);
        }

        /// <summary>
        /// Indicates if this Transform is a descendent of the supplied ancestor Transform.
        /// </summary>
        /// <param name="transform">The transform.</param>
        /// <param name="ancestor">The ancestor transform.</param>
        /// <returns>A boolean value indicating if this Transform is a descendent of the supplied ancestor Transform.</returns>
        public static bool IsDescendentOf(this Transform transform, Transform ancestor)
        {
            if (ancestor == null)
                throw new ArgumentNullException("ancestor");

            if (transform == ancestor)
                return false;

            var parent = transform.parent;
            while (parent != null)
            {
                if (parent == ancestor)
                    return true;

                parent = parent.parent;
            }

            return false;
        }
    }
}
