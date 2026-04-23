using Newtonsoft.Json.Serialization;
using System;
using System.Reflection;

namespace Liminal.SDK.Serialization
{
    internal class UnityReferenceValueProvider : IValueProvider
    {
        private readonly AssetLookup mAssetLookup;
        private readonly FieldInfo mField;

        public UnityReferenceValueProvider(AssetLookup assetLookup, FieldInfo field)
        {
            mAssetLookup = assetLookup;
            mField = field;
        }

        public object GetValue(object target)
        {
#if UNITY_EDITOR
            var asset = mField.GetValue(target) as UnityEngine.Object;
            if (asset == null)
                return null;
                
            return mAssetLookup.GetId(asset);
#else
            throw new NotImplementedException();
#endif
        }

        public void SetValue(object target, object value)
        {
            var id = Convert.ToInt32(value);
            var asset = mAssetLookup.GetAsset(id);
            mField.SetValue(target, asset);
        }
    }
}

