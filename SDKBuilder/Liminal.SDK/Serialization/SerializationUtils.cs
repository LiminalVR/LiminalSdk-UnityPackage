using Liminal.SDK.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace Liminal.SDK.Serialization
{
    /// <summary>
    /// A collection of utilities to aid in serialization/deserialization of data from loaded experiences.
    /// </summary>
    public class SerializationUtils
    {
        private static readonly HashSet<Type> _serializableTypes = new HashSet<Type>();

        /// <summary>
        /// Adds serializable types from the specified loaded assembly to the global type list.
        /// </summary>
        /// <param name="asmName">The name of the loaded assembly to add serializable types from.</param>
        public static void AddGlobalSerializableTypes(string asmName)
        {
            foreach (var value in BuildSerializableTypeSet(asmName))
            {
                _serializableTypes.Add(value);
            }
        }
        
        /// <summary>
        /// Adds serializable types from the specified assembly to the global type list.
        /// </summary>
        /// <param name="asm">The Assembly to add serializable types from.</param>
        public static void AddGlobalSerializableTypes(Assembly asm)
        {
#if LIMINAL_SERIALIZE_VERBOSE
            Debug.Log("[Serialization] AddGlobalSerializableTypes");
#endif
            foreach (var value in BuildSerializableTypeSet(asm))
            {
#if LIMINAL_SERIALIZE_VERBOSE
                Debug.LogFormat("           Add {0}", value.FullName);
#endif
                _serializableTypes.Add(value);
            }
        }

        /// <summary>
        /// Clears all types from the global serializable types list.
        /// </summary>
        public static void ClearGlobalSerializableTypes()
        {
#if LIMINAL_SERIALIZE_VERBOSE
            Debug.Log("[Serialization] ClearGlobalSerializableTypes");
#endif
            _serializableTypes.Clear();
        }

        /// <summary>
        /// Gets a field from the hierarchy of the suppled type's inheritance chain.
        /// </summary>
        /// <param name="type">The type to retrieve the field from.</param>
        /// <param name="fieldName">The name of the field to retrieve.</param>
        /// <param name="bindingFlags">The binding flags of the fields to check.</param>
        /// <returns>The first field with the specified name from the type's inheritance chain, or null if no field with the supplied name exists.</returns>
        public static FieldInfo GetFieldFromHierarchy(Type type, string fieldName, BindingFlags bindingFlags)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            while (type != typeof(object))
            {
                var field = type.GetField(fieldName, bindingFlags);
                if (field != null)
                    return field;

                type = type.BaseType;
            }

            return null;
        }

        /// <summary>
        /// Gets all fields from the type and its inheritance graph. Fields defined in types later in the chain have priority.
        /// </summary>
        /// <param name="type">The type to retrieve the fields from./param>
        /// <returns>An enumerable collection of FieldInfo objects.</returns>
        public static IEnumerable<FieldInfo> GetFieldsHierarchy(Type type, BindingFlags bindings)
        {
            if (type == null)
                return Enumerable.Empty<FieldInfo>();
            
            return type
                .GetFields(bindings)
                .Concat(GetFieldsHierarchy(type.BaseType, bindings))
                .GroupBy(x => x.Name)
                .Select(x => x.First())
                ;
        }

        /// <summary>
        /// Gets all fields that are considered serializable by Unity from the type and its inheritance graph. Fields defined in types later in the chain have priority.
        /// </summary>
        /// <param name="type">The type to retrieve the fields from./param>
        /// <returns>An enumerable collection of FieldInfo objects.</returns>
        public static IEnumerable<FieldInfo> GetUnitySerializableFieldsHierarchy(Type type)
        {
            if (type == null)
                return Enumerable.Empty<FieldInfo>();

            var bindings = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            return type
                .GetFields(bindings)
                .Where(f => IsUnitySerializableField(f))
                .Concat(GetUnitySerializableFieldsHierarchy(type.BaseType))
                .GroupBy(x => x.Name)
                .Select(x => x.First());
        }

        /// <summary>
        /// Gets all fields that are considered serializable by Unity from the type.
        /// </summary>
        /// <param name="type">The type to retrieve the fields from./param>
        /// <returns>An enumerable collection of FieldInfo objects.</returns>
        public static IEnumerable<FieldInfo> GetUnitySerializableFields(Type type)
        {
            var bindings = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            return type
                .GetFields(bindings)
                .Where(f => IsUnitySerializableField(f));
        }

        /// <summary>
        /// Indicates if the specified field is considered serializable by Unity.
        /// </summary>
        /// <param name="field">The FieldInfo value.</param>
        /// <returns>A boolean indicating if the specified field is considered serializable by Unity.</returns>
        public static bool IsUnitySerializableField(FieldInfo field)
        {
            var fieldType = field.FieldType;
            
            // Untiy Events are always serializable
            // Events have internal values that may reference values that the Unity serialization may
            // not know about after the app is loaded, so we want to always fill them
            if (IsUnityEventType(fieldType))
                return true;
            
            if (!IsSerializable(field))
                return false;

            if (fieldType.IsArray)
            {
                // Array type
                // Check element type against serializable type set
                var elementType = fieldType.GetElementType();
                if (_serializableTypes.Contains(elementType) || IsUnityEventType(elementType))
                    return true;
            }
            else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                // List type
                // Check element type against serializable type set
                var elementType = fieldType.GetGenericArguments()[0];
                if (_serializableTypes.Contains(elementType) || IsUnityEventType(elementType))
                    return true;
            }

            return _serializableTypes.Contains(fieldType) || IsUnityEventType(fieldType);
        }

        /// <summary>
        /// Indicates if the type is considered serializable. Types are considered serializable if they explicitly declare <see cref="SerializableAttribute"/>.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>A boolean indicating if the type is considered serializable.</returns>
        public static bool HasSerializableAttribute(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            return Attribute.IsDefined(type, typeof(SerializableAttribute));
        }
        
        /// <summary>
        /// Indicates if the field is considered serializable. Public fields are always serializable, so long as they are not marked with <see cref="NonSerializedAttribute"/>.
        /// All other access levels are only serializable if the explicitly declare the <see cref="SerializeField"/> attribute.
        /// </summary>
        /// <param name="field">The field to check.</param>
        /// <returns>A boolean indicating if the field is considered serializable.</returns>
        public static bool IsSerializable(FieldInfo field)
        {
            if (field == null)
                throw new ArgumentNullException("field");

            if (field.IsPublic && !Attribute.IsDefined(field, typeof(NonSerializedAttribute)))
                return true;

            // Field must be public, or have SerializeField declared
            if (!field.IsPublic && Attribute.IsDefined(field, typeof(SerializeField)))
                return true;

            return false;
        }

        /// <summary>
        /// Indicates if the supplied type inherits from <see cref="UnityEventBase"/>.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>A boolean indicating if the supplied type inherits from <see cref="UnityEventBase"/>.</returns>
        public static bool IsUnityEventType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            return typeof(UnityEventBase).IsAssignableFrom(type);
        }

        /// <summary>
        /// Indicates if the supplied type is from the same module as UnityEventBase
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsSameModuleAsUnityEvent(Type type)
        {
            return IsSameModule(type, typeof(UnityEventBase));
        }

        /// <summary>
        /// Indicates if typeA and typeB comes from the same module
        /// </summary>
        /// <param name="typeA"></param>
        /// <param name="typeB"></param>
        /// <returns></returns>
        public static bool IsSameModule(Type typeA,Type typeB)
        {
            return typeA.Module == typeB.Module;
        }

        /// <summary>
        /// Indicates if the supplied type inherits from <see cref="UnityEngine.Object"/>.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>A boolean indicating if the supplied type inherits from <see cref="UnityEngine.Object"/>.</returns>
        public static bool IsUnityObjectType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            return typeof(UnityEngine.Object).IsAssignableFrom(type);
        }

        /// <summary>
        /// Indicates if the specified type is an Array or List of Unity objects.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>A boolean indicating if the specified type is an Array or List of Unity objects.</returns>
        public static bool IsUnityObjectCollectionType(Type type)
        {
            // Array types
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                return IsUnityObjectType(elementType);
            }

            // List types
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elementType = type.GetGenericArguments()[0];
                return IsUnityObjectType(elementType);
            }

            return false;
        }

        /// <summary>
        /// Indicates if the specified type is a Unity Object type, or an Array or List of Unity objects.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>A boolean indicating if the specified type is a Unity Object type, or an Array or List of Unity objects,</returns>
        public static bool IsUnityObjectTypeOrCollectionType(Type type)
        {
            return (IsUnityObjectType(type) || IsUnityObjectCollectionType(type));
        }
        
        /// <summary>
        /// Builds a <see cref="HashSet{T}"/> of <see cref="Type"/> objects from the specified loaded assembly that are either Unity objects or have the Serializable attribute declared.
        /// </summary>
        /// <param name="asmName">The name of the loaded assembly to build the hashset from.</param>
        /// <returns>The hashset of types that was created.</returns>
        public static HashSet<Type> BuildSerializableTypeSet(string asmName)
        {
            var asm = AppDomain.CurrentDomain.GetLoadedAssembly(asmName);
            if (asm == null)
            {
                Debug.LogFormat("Assembly not loaded: {0}", asmName);
                return new HashSet<Type>();
            }

            return BuildSerializableTypeSet(asm);
        }

        /// <summary>
        /// Builds a <see cref="HashSet{T}"/> of <see cref="Type"/> objects from the specified  assembly that are either Unity objects or have the Serializable attribute declared.
        /// </summary>
        /// <param name="asmName">The assembly to build the hashset from.</param>
        /// <returns>The hashset of types that was created.</returns>
        public static HashSet<Type> BuildSerializableTypeSet(Assembly asm)
        {
            if (asm == null)
            {
                Debug.LogError("Assembly is null");
                return new HashSet<Type>();
            }

            // Find all types in the assembly that are marked are serializable, but NOT UnityEngine.Object types
            var types = asm.GetTypes()
                .Where(t => !IsUnityObjectType(t) && HasSerializableAttribute(t));

            return new HashSet<Type>(types);
        }
    }
}
