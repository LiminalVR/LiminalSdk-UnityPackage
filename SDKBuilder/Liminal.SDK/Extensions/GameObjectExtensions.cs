using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Liminal.SDK.Extensions
{
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Gets the component of the specified type from a GameObject. If the component does not exist, it will be added
        /// to the GameObject and then returned.
        /// </summary>
        /// <typeparam name="T">The type of the component</typeparam>
        /// <param name="gameObject">The GameObject to get the component from, or add the component to.</param>
        /// <returns>The component of the specified type.</returns>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            if (gameObject == null)
                throw new ArgumentNullException("gameObject");

            var component = gameObject.GetComponent<T>();
            if (component == null)
                component = gameObject.AddComponent<T>();

            return component;
        }

        /// <summary>
        /// Gets the component of the specified type from a GameObject. If the component does not exist, it will be added
        /// to the GameObject and then returned.
        /// </summary>
        /// <param name="gameObject">The GameObject to get the component from, or add the component to.</param>
        /// <param name="type">The type of the component</type>
        /// <returns>The component of the specified type.</returns>
        public static Component GetOrAddComponent(this GameObject gameObject, Type type)
        {
            if (gameObject == null)
                throw new ArgumentNullException("gameObject");

            if (type == null)
                throw new ArgumentNullException("type");

            var component = gameObject.GetComponent(type);
            if (component == null)
                component = gameObject.AddComponent(type);

            return component;
        }

        /// <summary>
        /// Returns a boolean value indicating if the specified GameObject has a component of the specified type.
        /// </summary>
        /// <typeparam name="T">The component type</typeparam>
        /// <param name="gameObject">The GameObject to operate on.</param>
        /// <returns>A boolean value indicating if the supplied <see cref="GameObject"/> has a component of the specified type.</returns>
        public static bool HasComponent<T>(this GameObject gameObject)
        {
            if (gameObject == null)
                throw new ArgumentNullException("gameObject");

            return (gameObject.GetComponent<T>() != null);
        }
    }
}