using System;
using System.Collections.Generic;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Liminal.SDK.Serialization
{
    [Serializable]
    public class SerializedData
    {
        public List<GameObjectData> SceneGameObjects;
        public List<PrefabGameObjectData> Prefabs;
        public List<ScriptableObjectData> ScriptableObjects;
    }

    [Serializable]
    public class PrefabGameObjectData
    {
        public int Id;
        public string Name;
        public List<ComponentData> Components = new List<ComponentData>();
        public List<GameObjectData> Children = new List<GameObjectData>();
    }

    [Serializable]
    public class ScriptableObjectData
    {
        public int Id;
        public string Name;
        public List<FieldData> Fields = new List<FieldData>();
    }

    [Serializable]
    public class GameObjectData
    {
        public string Name;
        public int Index;
        public string NamePath;
        public string IndexPath;
        public List<ComponentData> Components = new List<ComponentData>();
        public List<GameObjectData> Children = new List<GameObjectData>();
    }

    [Serializable]
    public class ComponentData
    {
        public int Index;
        public string Name;
        public List<FieldData> Fields = new List<FieldData>();
    }

    [Serializable]
    public class FieldData
    {
        public string Name;
        public string Json;
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
