using Liminal.SDK.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;

namespace Liminal.SDK.Serialization
{
    internal class Instantiator
    {
        #region Deserialization Methods

        /// <summary>
        /// Clones values from types marked with SerializableAttribute within the app assembly, from the source object into the destination object.
        /// </summary>
        /// <param name="source">The source object.</param>
        /// <param name="dest">The destination object.</param>
        public void Deserialize(UnityEngine.Object source, UnityEngine.Object dest)
        {
            // Component
            {
                var compDest = dest as Component;
                if (compDest != null)
                {
                    var goSource = ((Component)source).gameObject;
                    var goDest = compDest.gameObject;
                    Deserialize(source, dest, goSource, goDest);
                    return;
                }
            }

            // GameObject
            {
                var goDest = dest as GameObject;
                if (goDest != null)
                {
                    var goSource = (GameObject)source;
                    Deserialize(source, dest, goSource, goDest);
                    return;
                }
            }

            // ScriptableObject
            {
                var soDest = dest as ScriptableObject;
                if (soDest != null)
                {
                    var soSource = (ScriptableObject)source;
                    Deserialize(source, dest, soSource, soDest);
                }
            }
        }
        
        private void Deserialize(UnityEngine.Object rootSource, UnityEngine.Object rootDest, GameObject goSource, GameObject goDest)
        {
            return;

            // Fetch temporary lists to store component references
            // These are pooled, but not cached directly because recursive calls to Deserialize() methods
            // would end up modifying cached collecitons
            var compListA = ListPool<Component>.Get();
            var compListB = ListPool<Component>.Get();

            goSource.GetComponents(compListA);
            goDest.GetComponents(compListB);
            Assert.AreEqual(compListA.Count, compListB.Count, "Component list size on source and destination do not match");

            for (int i = 0; i < compListA.Count; ++i)
            {
                var componentA = compListA[i];
                var componentB = compListB[i];

                // NOTE: A component can be null from a GetComponents() call if the MonoScript cannot be resolved
                if (componentA == null)
                    continue;

                CloneFieldValues(rootSource, rootDest, componentA, componentB, cloneAllFields: false);
            }

            // Release temporary component lists
            ListPool<Component>.Release(ref compListA);
            ListPool<Component>.Release(ref compListB);

            // Step into children
            var childCount = goDest.transform.childCount;
            for (int i = 0; i < childCount; ++i)
            {
                var childA = goSource.transform.GetChild(i).gameObject;
                var childB = goDest.transform.GetChild(i).gameObject;
                Deserialize(rootSource, rootDest, childA, childB);
            }
        }

        private void Deserialize(UnityEngine.Object rootSource, UnityEngine.Object rootDest, ScriptableObject source, ScriptableObject dest)
        {
            return;

            CloneFieldValues(rootSource, rootDest, source, dest, cloneAllFields: false);
        }

        #endregion

        #region Clone Methods

        private object CloneObject(UnityEngine.Object rootSource, UnityEngine.Object rootDest, object source)
        {
            if (source == null)
                return null;

            var type = source.GetType();
            if (type.IsArray)
            {
                // Array types
                var len = ((Array)source).Length;
                var array = Array.CreateInstance(type.GetElementType(), len);
                CloneArrayContent(rootSource, rootDest, (Array)source, array);
                return array;
            }
            else if (typeof(IList).IsAssignableFrom(type))
            {
                // List types
                var list = Activator.CreateInstance(type);
                CloneListContent(rootSource, rootDest, (IList)source, (IList)list);
                return list;
            }
            else
            {
                var dest = Activator.CreateInstance(type);
                CloneFieldValues(rootSource, rootDest, source, dest, cloneAllFields: true);
                return dest;
            }
        }

        private void CloneArrayContent(UnityEngine.Object rootSource, UnityEngine.Object rootDest, Array source, Array dest)
        {
            for (int i = 0; i < source.Length; ++i)
            {
                var valueA = source.GetValue(i);
                var valueB = CloneObject(rootSource, rootDest, valueA);
                dest.SetValue(valueB, i);
            }
        }

        private void CloneListContent(UnityEngine.Object rootSource, UnityEngine.Object rootDest, IList source, IList dest)
        {
            for (int i = 0; i < source.Count; ++i)
            {
                var valueA = source[i];
                var valueB = CloneObject(rootSource, rootDest, valueA);
                dest.Add(valueB);
            }
        }

        private void CloneFieldValues(UnityEngine.Object rootSource, UnityEngine.Object rootDest, object source, object dest, bool cloneAllFields)
        {
            Assert.IsTrue(rootSource.GetType() == rootDest.GetType(), "rootSource and rootDest are not of the same type");
            Assert.IsTrue(source.GetType() == dest.GetType(), "source and dest are not of the same type");

            const BindingFlags bindings = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            
            var sourceType = source.GetType();
            var fields = cloneAllFields
                ? SerializationUtils.GetFieldsHierarchy(sourceType, bindings) 
                : SerializationUtils.GetUnitySerializableFieldsHierarchy(sourceType);
            
            foreach (var field in fields)
            {
                var value = field.GetValue(source);
                if (value == null)
                {
                    field.SetValue(dest, null);
                    continue;
                }
                
                var valueType = value.GetType();
                if (SerializationUtils.IsUnityObjectType(valueType))
                {
                    // Unity object reference
                    // Things get complex here!!

                    // UnityEngine.Object fields can reference objects that are part of their own hierarchy (GameObjects or Components)
                    // This means that when cloning and object, any self-referencing values need to be redirected to the COPY

                    var sourceUO = source as UnityEngine.Object;
                    var valueUO = value as UnityEngine.Object;
                    if (sourceUO == valueUO)
                    {
                        // Value is a reference to the source, so the destiantion can just reference itself
                        field.SetValue(dest, dest);
                        continue;
                    }

                    var unitySourceGo = rootSource as GameObject;
                    if (unitySourceGo != null)
                    {
                        // Root source is a GameObject
                        // If the value is a descendant of the original source object, then we need to find the corresponding location in the NEW object

                        // Find the GameObject for the value
                        // If the value is a component, we can pull the owner GameObject, otherwise attempt to cast it...
                        var valueComponent = valueUO as Component;
                        var valueGameObject = (valueComponent != null)
                            ? valueComponent.gameObject
                            : (valueUO as GameObject);
                        
                        // If a valid GameObject was able to be pulled from the value, we need to check to see if it is a descendant
                        // of the original source GameObject - if so, we need to find the corresponding target in the destination
                        if (valueGameObject != null && IsDescendant(unitySourceGo, valueGameObject))
                        {
                            // The target GameObject is a decendant of the original clone source GameObject

                            // Safe to assume the destination root is GameObject now, since we know the source is one!
                            var unityDestGo = rootDest as GameObject;

                            // Find the path from the source GameObject (root) to the value GameObject
                            // Find the corresponding GameObject in the destination hierarchy
                            var path = GetGameObjectPath(unitySourceGo, valueGameObject);
                            var destGameObject = GetGameObjectFromPath(unityDestGo, path);

                            if (valueComponent == null)
                            {
                                // The original was not a Component, so the final value is a GameObject
                                // Set the field to the new destination
                                field.SetValue(dest, destGameObject);
                            }
                            else
                            {
                                // Final destination is a component
                                // Find the index of the component in the source object and set the field
                                var compIndex = GetComponentIndex(valueComponent);
                                var destComp = GetComponentAtIndex(destGameObject, compIndex);
                                field.SetValue(dest, destComp);
                            }

                            // -- DONE
                            continue;
                        }
                    }
                    
                    // Reference to another Unity Object OUTSIDE of this hierarchy (another prefab, a GameObject in the scene, or a ScriptableObject)
                    // In this case we can simly retain the reference...
                    field.SetValue(dest, value);
                }
                else if (CanAssignDirectToField(valueType))
                {
                    // Value types we can just copy
                    field.SetValue(dest, value);
                }
                else
                {
                    // Reference types will need to be instantiated and recursively deserialized...
                    field.SetValue(dest, CloneObject(rootSource, rootDest, value));
                }
            }
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Indicates if valeus of the supplied type can be assigned directly to a field.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>A boolean indicating if values of <paramref name="type"/> can be assigned directly to a field.</returns>
        private bool CanAssignDirectToField(Type type)
        {
            // Value types are fine to assign directly
            if (type.IsValueType)
                return true;

            // Strings are classes, so will return false for IsValueType, however it's
            // find to just assign it directly, so return true here
            if (type == typeof(string))
                return true;

            return false;
        }
        
        /// <summary>
        /// Finds the GameObject at the index path, relative to the supplied root GameObject.
        /// </summary>
        /// <param name="root">The root GameObject.</param>
        /// <param name="path">The path to the final GameObject, relative to <paramref name="root"/>.</param>
        /// <returns>The GameObject at the specified path relative to the supplied root GameObject.</returns>
        private GameObject GetGameObjectFromPath(GameObject root, int[] path)
        {
            var target = root.transform;
            for (int i = 0; i < path.Length; ++i)
            {
                var index = path[i];
                if (index < 0 || index >= target.childCount)
                {
                    Debug.LogError(string.Format("Child index is out of range: {0}, transform has {2} children.", index, target.childCount), target);
                    return null;
                }

                target = target.GetChild(index);
            }

            return target.gameObject;
        }
        
        /// <summary>
        /// Gets the child index path of <paramref name="child"/> relative to <paramref name="root"/>.
        /// </summary>
        /// <param name="root">The root GameObject.</param>
        /// <param name="child">The child GameObject.</param>
        /// <returns>The index path to <paramref name="child"/>, relative to <paramref name="root"/>.</returns>
        private int[] GetGameObjectPath(GameObject root, GameObject child)
        {
            var path = new List<int>();

            var target = child.transform;
            while (target != null && target != root.transform)
            {
                path.Add(target.GetSiblingIndex());   
                target = target.parent;
            }

            path.Reverse();
            return path.ToArray();
        }
        
        /// <summary>
        /// Gets the index of a specified component in its owning GameObject.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <returns>The index of the component within its owning GameObject.</returns>
        private int GetComponentIndex(Component component)
        {
            var list = ListPool<Component>.Get();
            component.gameObject.GetComponents(list);

            var index = list.IndexOf(component);

            ListPool<Component>.Release(ref list);
            return index;
        }

        /// <summary>
        /// Gets the <see cref="Component"/> at the specified index on <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">The GameObject to retrieve the Component from.</param>
        /// <param name="index">The index of the Component within the GameObject to retrieve.</param>
        /// <returns>The Component at the specified index on <paramref name="gameObject"/>.</returns>
        private Component GetComponentAtIndex(GameObject gameObject, int index)
        {
            var list = ListPool<Component>.Get();
            gameObject.GetComponents(list);

            Component component = null;
            if (index >= 0 && index < list.Count)
            {
                component = list[index];
            }
            else
            {
                Debug.LogError(string.Format("Component index is out of range: {0}. GameObject has {1} components.", index, list.Count), gameObject);
            }

            ListPool<Component>.Release(ref list);
            return component;
        }
        
        /// <summary>
        /// Indicates if the <paramref name="child"/> is a descendant of <paramref name="root"/>.
        /// </summary>
        /// <param name="root">The root GameObject.</param>
        /// <param name="child">The child GameObject.</param>
        /// <returns>A boolean indicating if <paramref name="child"/> is a descendant of <paramref name="root"/>.</returns>
        private bool IsDescendant(GameObject root, GameObject child)
        {
            if (child == root)
                return true;

            var parent = child.transform.parent;
            while (parent != null)
            {
                if (parent.gameObject == root)
                    return true;

                parent = parent.transform;
            }

            return false;
        }

        #endregion
    }
}