using Liminal.SDK.Serialization;
using UnityEngine;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public static class LiminalObject
{
    private static Instantiator _instantiator = new Instantiator();

    public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation) where T : Object
    {
        var instance = Object.Instantiate(original, position, rotation);
        _instantiator.Deserialize(original, instance);
        return instance;
    }

    public static T Instantiate<T>(T original, Transform parent, bool worldPositionStays) where T : Object
    {
        var instance = Object.Instantiate(original, parent, worldPositionStays);
        _instantiator.Deserialize(original, instance);
        return instance;
    }

    public static T Instantiate<T>(T original, Transform parent) where T : Object
    {
        var instance = Object.Instantiate(original, parent);
        _instantiator.Deserialize(original, instance);
        return instance;
    }

    public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation, Transform parent) where T : Object
    {
        var instance = Object.Instantiate(original, position, rotation, parent);
        _instantiator.Deserialize(original, instance);
        return instance;
    }

    public static T Instantiate<T>(T original) where T : Object
    {
        var instance = Object.Instantiate(original);
        _instantiator.Deserialize(original, instance);
        return instance;
    }

    public static Object Instantiate(Object original, Vector3 position, Quaternion rotation, Transform parent)
    {
        var instance = Object.Instantiate(original, position, rotation, parent);
        _instantiator.Deserialize(original, instance);
        return instance;
    }

    public static Object Instantiate(Object original, Transform parent)
    {
        var instance = Object.Instantiate(original, parent);
        _instantiator.Deserialize(original, instance);
        return instance;
    }

    public static Object Instantiate(Object original)
    {
        var instance = Object.Instantiate(original);
        _instantiator.Deserialize(original, instance);
        return instance;
    }

    public static Object Instantiate(Object original, Vector3 position, Quaternion rotation)
    {
        var instance = Object.Instantiate(original, position, rotation);
        _instantiator.Deserialize(original, instance);
        return instance;
    }

    public static Object Instantiate(Object original, Transform parent, bool instantiateInWorldSpace)
    {
        var instance = Object.Instantiate(original, parent, instantiateInWorldSpace);
        _instantiator.Deserialize(original, instance);
        return instance;
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member