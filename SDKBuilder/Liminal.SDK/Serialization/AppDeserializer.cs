using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Liminal.SDK.Serialization
{
    internal class AppDeserializer
    {
        private readonly List<Component> mCachedComponentList = new List<Component>(100);
        private AssetLookup mAssetLookup;
        private List<GameObject> mRootGameObjects;
        private JsonSerializerSettings mJsonSettings;
        
        public AppDeserializer(AssetLookup assetLookup)
        {
            mAssetLookup = assetLookup;
            mJsonSettings = new JsonSerializerSettings()
            {
                ContractResolver = new UnityJsonContractResolver(null, mAssetLookup)
            };
        }

        public void Deserialize(List<GameObject> rootGameObjects, TextAsset appDataAsset)
        {
            Debug.Log("AppDeserializer.Deserialize()");

            if (appDataAsset == null)
                return;

            mRootGameObjects = rootGameObjects;
            try
            {
                var data = JsonConvert.DeserializeObject<SerializedData>(appDataAsset.text);
                Deserialize(data);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void Deserialize(SerializedData data)
        {
            foreach (var item in data.Prefabs)
            {
                DeserializePrefabGameObject(item);
            }

            foreach (var item in data.ScriptableObjects)
            {
                DeserializeScriptableObject(item);
            }

            foreach (var item in data.SceneGameObjects)
            {
                DeserializeSceneGameObject(item);
            }
        }

        private void DeserializeSceneGameObject(GameObjectData data)
        {
            var go = FindGameObjectByIndexPath(data.IndexPath, data.NamePath);
            if (go == null)
            {
                Debug.LogErrorFormat("Scene GameObject not found: {0} ({1})", data.IndexPath, data.NamePath);
            }
            else
            {
#if LIMINAL_SERIALIZE_VERBOSE
                Debug.LogFormat(go, "DeserializeSceneGameObject {0} {1} {2}", go.name, data.IndexPath, data.Name);
#endif
                DeserializeGameObjectData(go, data);
            }
        }

        private void DeserializePrefabGameObject(PrefabGameObjectData data)
        {
            var go = mAssetLookup.GetAsset(data.Id) as GameObject;
            if (go == null)
            {
                Debug.LogErrorFormat("Prefab GameObject not found: {0} ({1})", data.Id, data.Name);
                return;
            }
#if LIMINAL_SERIALIZE_VERBOSE
            Debug.LogFormat(go, "DeserializePrefabGameObject {0} {1} {2}", go.name, data.Id, data.Name);
#endif
            DeserializeComponents(go, data.Components);

            for (int i = 0; i < data.Children.Count; ++i)
            {
                var childData = data.Children[i];
                if (childData.Index < 0 || childData.Index >= go.transform.childCount)
                {
                    Debug.LogErrorFormat(go, "Child not found at index: {0} (path={1}, name={2}). Transform {3} has {4} children.",
                        childData.Index, childData.IndexPath, data.Name, go.transform.name, go.transform.childCount);
                }
                else
                {
                    DeserializeGameObjectData(go.transform.GetChild(i).gameObject, childData);
                }
            }
        }

        private void DeserializeScriptableObject(ScriptableObjectData data)
        {
            var so = mAssetLookup.GetAsset(data.Id) as ScriptableObject;
            if (so == null)
            {
                Debug.LogFormat("ScriptableObject asset not found: {0} ({1})", data.Id, data.Name);
            }
            else
            {
#if LIMINAL_SERIALIZE_VERBOSE
                Debug.LogFormat(so, "DeserializeScriptableObject {0} {1} {2}", so.name, data.Id, data.Name);
#endif
                DeserializeFields(so, data.Fields);
            }
        }

        private void DeserializeGameObjectData(GameObject go, GameObjectData data)
        {
            DeserializeComponents(go, data.Components);

            for (int i = 0; i < data.Children.Count; ++i)
            {
                var childData = data.Children[i];
                var childIndex = childData.Index;
                if (childIndex < 0 || childIndex >= go.transform.childCount)
                {
                    Debug.LogErrorFormat(go, "Child not found at index: {0} (path={1}, name={2}). Transform {3} has {4} children.",
                        childData.Index, childData.IndexPath, data.NamePath, go.transform.name, go.transform.childCount);
                }
                else
                {
                    DeserializeGameObjectData(go.transform.GetChild(i).gameObject, childData);
                }
            }
        }

        private void DeserializeComponents(GameObject gameObject, List<ComponentData> componentDataList)
        {
            gameObject.GetComponents(mCachedComponentList);
            foreach (var componentData in componentDataList)
            { 
                if (componentData.Index < 0 || componentData.Index >= mCachedComponentList.Count)
                {
                    Debug.LogErrorFormat(gameObject, "Component index out of range: {0}::{1} index={2}", gameObject.name, componentData.Name, componentData.Index);
                }
                else
                {
                    var component = mCachedComponentList[componentData.Index];
                    if (component == null)
                    {
                        Debug.LogErrorFormat(gameObject, "Component not found: {0}::{1}", gameObject.name, componentData.Name);
                    }
                    else
                    {
                        DeserializeFields(component, componentData.Fields);
                    }
                }
            }

            mCachedComponentList.Clear();
        }

        private void DeserializeFields(object target, List<FieldData> fieldDataList)
        {
            if (target == null)
            {
                Debug.LogErrorFormat("AppDeserializer.DeserializeFields() target is null. Fields {0}", fieldDataList);
                return;
            }

            var type = target.GetType();
            const BindingFlags bindings = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (int i = 0; i < fieldDataList.Count; ++i)
            {
                var fieldData = fieldDataList[i];

                // Find the field in the type's inheritance chain
                var field = SerializationUtils.GetFieldFromHierarchy(type, fieldData.Name, bindings);
                if (field == null)
                {
                    Debug.LogErrorFormat(target as UnityEngine.Object, "Field not found: {0}::{1}", type, fieldData.Name);
                }
                else
                {
                    try
                    {
                        // Deserialize value and apply to the target
                        var value = JsonConvert.DeserializeObject(fieldData.Json, field.FieldType, mJsonSettings);
                        field.SetValue(target, value);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }
        }

#region Utilities

        private GameObject FindGameObjectByNamePath(string path)
        {
            // If the path starts with a /, remove it, otherwise we'll end up with an empty array element
            if (path[0] == '/')
                path = path.Substring(1);

            var pathParts = path.Split('/');
            var index = 0;

            var target = mRootGameObjects[int.Parse(pathParts[index++])];
            if (target != null)
            {
                while (index < pathParts.Length)
                {
                    var name = pathParts[index++];
                    for (int i = 0; i < target.transform.childCount; ++i)
                    {
                        var child = target.transform.GetChild(i).gameObject;
                        if (child.name == name)
                        {
                            target = child;
                            break;
                        }
                    }
                }
            }
            
            return target;
        }
        
        private GameObject FindGameObjectByIndexPath(string path, string namePath)
        {
            // If the path starts with a /, remove it, otherwise we'll end up with an empty array element
            if (path[0] == '/')
                path = path.Substring(1);

            var pathParts = path.Split('/');
            var index = 0;

            var root = mRootGameObjects[int.Parse(pathParts[index++])];
            var target = root;
            if (target != null)
            {
                while (index < pathParts.Length)
                {
                    var childIndex = int.Parse(pathParts[index++]);
                    if (childIndex < 0 || childIndex >= target.transform.childCount)
                    {
                        Debug.Log(string.Format("Child not found at index: {0} (path={1}, name={2})", childIndex, path, namePath), root);
                        return null;
                    }
                    
                    target = target.transform.GetChild(childIndex).gameObject;
                }
            }

            return target;
        }

#endregion
    }
}
