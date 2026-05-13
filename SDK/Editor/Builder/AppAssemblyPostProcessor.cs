using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Liminal.Cecil.Mono.Cecil;
using Liminal.Cecil.Mono.Cecil.Cil;

namespace Liminal.SDK.Editor.Build
{
    /// <summary>
    /// Build-time validation applied to a limapp's compiled DLL before it is packed into the AppPack.
    /// Currently checks that the limapp invokes <c>Liminal.SDK.Core.ExperienceApp.End()</c> somewhere —
    /// limapps without an End() call would never terminate at runtime and won't be approved.
    /// </summary>
    internal class AppAssemblyPostProcessor
    {
        public void PostProcess(string dllPath)
        {
            if (!File.Exists(dllPath))
                throw new FileNotFoundException("Cannot validate assembly; file not found.", dllPath);

            using var asmDef = AssemblyDefinition.ReadAssembly(dllPath, new ReaderParameters { InMemory = true });

            if (!HasExperienceAppEndCall(asmDef))
            {
                throw new Exception(
                    "No method call to Liminal.SDK.Core.ExperienceApp.End() was found. " +
                    "Your app will never end. Your app will not be approved without a call to End().");
            }
        }

        private static bool HasExperienceAppEndCall(AssemblyDefinition asmDef)
        {
            asmDef.MainModule.TryGetTypeReference("Liminal.SDK.Core.ExperienceApp", out var appTypeRef);
            if (appTypeRef == null)
                return false;

            foreach (var methodDef in EnumerateMethods(asmDef))
            {
                if (!methodDef.HasBody)
                    continue;

                foreach (var instruction in methodDef.Body.Instructions)
                {
                    if (instruction.OpCode != OpCodes.Call)
                        continue;

                    if (instruction.Operand is MethodReference mRef &&
                        mRef.Name == "End" && mRef.DeclaringType == appTypeRef)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static IEnumerable<MethodDefinition> EnumerateMethods(AssemblyDefinition asmDef)
        {
            return asmDef.Modules
                .SelectMany(m => m.Types
                    .Concat(m.Types.SelectMany(x => x.NestedTypes))
                    .SelectMany(x => x.Methods));
        }
    }
}
