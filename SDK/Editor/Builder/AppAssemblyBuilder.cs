using System;

namespace Liminal.SDK.Editor.Build
{
    /// <summary>
    /// Legacy Roslyn-based assembly compiler. Replaced by the asmdef-driven pipeline in
    /// <see cref="AppBuilder"/> together with <see cref="AppAssemblyPostProcessor"/>.
    /// </summary>
    [Obsolete("AppAssemblyBuilder has been retired. Limapps now compile via a project-root asmdef; run 'Liminal > Setup Limapp Build' to create one. Cecil post-processing lives in AppAssemblyPostProcessor.", error: false)]
    internal class AppAssemblyBuilder
    {
    }
}
