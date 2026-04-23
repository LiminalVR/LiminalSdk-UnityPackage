using Newtonsoft.Json.Serialization;
using System;
using System.Reflection;

namespace Liminal.SDK.Serialization
{
    internal class UnityValueProvider : IValueProvider
    {
        private readonly FieldInfo mField;

        public UnityValueProvider(FieldInfo field)
        {
            mField = field;
        }

        public object GetValue(object target)
        {
            return mField.GetValue(target);
        }

        public void SetValue(object target, object value)
        {
            mField.SetValue(target, value);
        }
    }
}

