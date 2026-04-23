using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Liminal.SDK.Serialization
{
    internal class UnityReferenceCollectionValueProvider : IValueProvider
    {
        private readonly AssetLookup mAssetLookup;
        private readonly FieldInfo mField;

        public UnityReferenceCollectionValueProvider(AssetLookup assetLookup, FieldInfo field)
        {
            mAssetLookup = assetLookup;
            mField = field;
        }

        public object GetValue(object target)
        {
#if UNITY_EDITOR
            var collection = (IEnumerable)mField.GetValue(target);
            var list = new List<int>();

            foreach (UnityEngine.Object asset in collection)
            {
                list.Add(mAssetLookup.GetId(asset));
            }

            return list;
#else
            return mField.GetValue(target);
#endif
        }

        public void SetValue(object target, object value)
        {
            var idList = (List<int>)value;

            var fieldType = mField.FieldType;
            if (fieldType.IsArray)
            {
                // Array
                var array = Array.CreateInstance(fieldType.GetElementType(), idList.Count);
                for (int i = 0; i < idList.Count; ++i)
                {
                    array.SetValue(mAssetLookup.GetAsset(idList[i]), i);
                }

                mField.SetValue(target, array);
            }
            else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                // List
                var listType = fieldType.MakeGenericType(fieldType.GetGenericArguments()[0]);
                var list = (IList)Activator.CreateInstance(listType, idList.Count);
                for (int i = 0; i < idList.Count; ++i)
                {
                    list.Add(mAssetLookup.GetAsset(idList[i]));
                }

                mField.SetValue(target, list);
            }
        }
    }
}
