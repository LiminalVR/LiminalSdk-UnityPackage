using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Liminal.SDK.Serialization
{
    /// <summary>
    /// Provides resolution for JSON contracts on Unity serializable fields.
    /// </summary>
    public class UnityJsonContractResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
    {
        /// <summary>
        /// The fields from UnityEventBase types that we want to force to be serialized. These values are required
        /// to reconstruct events when the objects are deserialized later.
        /// </summary>
        private static HashSet<string> _unityEventFieldNames = new HashSet<string>()
        {
            "m_Arguments",
            "m_PersistentCalls",
            "m_TypeName"
        };

        /// <summary>
        /// The fields from UnityEventBase types that we will be modified to match the assembly
        /// </summary>
        private static HashSet<string> _assemblyUnityEventFieldNames = new HashSet<string>()
        {
            "m_TypeName"
        };

        /// <summary>
        /// The collection of types that are fully serialized regardless of attribute settings. This is required because some
        /// Unity types are not supported with JSON conversion with JSON.NET.
        /// </summary>
        private static readonly HashSet<Type> _fullTypes = new HashSet<Type>()
        {
            typeof(AnimationCurve)
        };
        
        private AssetLookup mAssetLookup;
        private IAssemblyDataProvider mAssemblyDataProvider;

        /// <summary>
        /// Creates a new UnityJsonContractResolver using the specified asset lookup table.
        /// </summary>
        /// <param name="assemblyDataProvider">The assembly data provider for the project assembly.</param>
        /// <param name="assetLookup">The <see cref="AssetLookup"/> to use when reading or writing object references.</param>
        public UnityJsonContractResolver(IAssemblyDataProvider assemblyDataProvider, AssetLookup assetLookup)
        {
            mAssemblyDataProvider = assemblyDataProvider;
            mAssetLookup = assetLookup;
        }

        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
        {
            if (!_fullTypes.Contains(objectType))
            {
                // If not serializing the full object type, us
                var bindings = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var fields = SerializationUtils.GetFieldsHierarchy(objectType, bindings)
                    .Cast<MemberInfo>()
                    .ToList();

                return fields;
            }

            return base.GetSerializableMembers(objectType);
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var prop = base.CreateProperty(member, memberSerialization);

            // If the type is declared for full serialization, return the default property value
            // This will let Json.NET take care of the serialization

            if (_fullTypes.Contains(member.DeclaringType))
                return prop;


            // We only care about fields for unity types...
            if (member.MemberType != MemberTypes.Field)
            {
                prop.Writable = false;
                prop.Readable = false;
                return prop;
            }

            var field = (FieldInfo)member;
            if (
                IsUnityEventNestedField(field) && !_assemblyUnityEventFieldNames.Contains(member.Name)  || 
                (SerializationUtils.IsUnityEventType(field.DeclaringType) && _unityEventFieldNames.Contains(member.Name)) ||
                (field.FieldType.IsPublic && !Attribute.IsDefined(field.FieldType, typeof(NonSerializedAttribute))) ||
                (!field.FieldType.IsPublic && Attribute.IsDefined(field.FieldType, typeof(SerializeField)))
            )
            {

                prop.Writable = true;
                prop.Readable = true;
                prop.NullValueHandling = NullValueHandling.Ignore;

                if (SerializationUtils.IsUnityObjectType(field.FieldType))
                {
                    // Unity object reference
                    prop.PropertyType = typeof(int);
                    prop.ValueProvider = new UnityReferenceValueProvider(mAssetLookup, field);
                }
                else if (SerializationUtils.IsUnityObjectCollectionType(field.FieldType))
                {
                    // Collection of unity object references
                    prop.PropertyType = typeof(List<int>);
                    prop.ValueProvider = new UnityReferenceCollectionValueProvider(mAssetLookup, field);
                }
                else if (SerializationUtils.IsUnityEventType(field.DeclaringType))
                {
                    // Unity event
                    prop.ValueProvider = new UnityEventValueProvider(mAssemblyDataProvider, field);
                }
                else
                {
                    // Standard field
                    prop.ValueProvider = new UnityValueProvider(field);
                }
            }
            else
            {
                prop.Writable = false;
                prop.Readable = false;
            }

            return prop;
        }

        private bool IsSerializableField(FieldInfo fieldInfo)
        {
            return

                // Event declaring type and property is in event field list
                (SerializationUtils.IsUnityEventType(fieldInfo.DeclaringType) && _unityEventFieldNames.Contains(fieldInfo.Name)) || 

                // Public, or marked as [SerializeField]
                SerializationUtils.IsSerializable(fieldInfo)
            ;
        }

        //Indicates if the field is from _eventFieldNames & from the same Module
        private bool IsUnityEventNestedField(FieldInfo fieldInfo)
        {
            return SerializationUtils.IsSameModuleAsUnityEvent(fieldInfo.DeclaringType) && _unityEventFieldNames.Contains(fieldInfo.Name);
        }

    }
}
