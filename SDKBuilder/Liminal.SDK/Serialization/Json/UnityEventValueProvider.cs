using Newtonsoft.Json.Serialization;
using System;
using System.Reflection;

namespace Liminal.SDK.Serialization
{
    internal class UnityEventValueProvider : IValueProvider
    {
        private const string ProjectAssemblyFullName = "Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";

        private readonly IAssemblyDataProvider mAssemblyDataProvider;
        private readonly FieldInfo mField;

        public UnityEventValueProvider(IAssemblyDataProvider assemblyDataProvider, FieldInfo field)
        {
            mAssemblyDataProvider = assemblyDataProvider;
            mField = field;
        }

        public object GetValue(object target)
        {
            var value = mField.GetValue(target);

#if UNITY_EDITOR
            // Any events compiled into the project assembly need to have their m_TypeName value changed to use the assembly data from the provider
            if (mField.Name == "m_TypeName")
            {
                return ((string)value).Replace(ProjectAssemblyFullName, mAssemblyDataProvider.FullName);
            }
#endif
            return value;
        }

        public void SetValue(object target, object value)
        {
            mField.SetValue(target, value);
        }
    }

}
