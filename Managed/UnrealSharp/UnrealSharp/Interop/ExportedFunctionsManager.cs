using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnrealSharp.Binds;

namespace UnrealSharp.Interop;

public static class ExportedFunctionsManager
{
    private static readonly Dictionary<string, FieldInfo> UnmanagedDelegates = new();

    public static unsafe void Initialize(IntPtr nativeExportFunctionsPtr)
    {
        try
        {
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (Type type in types)
            {
                if (!Attribute.IsDefined(type, typeof(NativeCallbacksAttribute)))
                {
                    continue;
                }

                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                                                           BindingFlags.Static))
                {
                    if (!field.IsStatic || !field.FieldType.IsUnmanagedFunctionPointer)
                    {
                        continue;
                    }
                        
                    UnmanagedDelegates.TryAdd(type.Name + "." + field.Name, field);
                }
            }

            
            delegate* unmanaged[Cdecl]<IntPtr, void> registerFunctions = (delegate* unmanaged[Cdecl]<IntPtr, void>)nativeExportFunctionsPtr;
            IntPtr registerFunctionsCallback = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, char*, void>)&RegisterFunctions;

            registerFunctions(registerFunctionsCallback);
                
            foreach (KeyValuePair<string, FieldInfo> unmanagedDelegate in UnmanagedDelegates)
            {
                if (unmanagedDelegate.Value.GetValue(null) == null)
                {
                    LogUnrealSharp.LogWarning($"Failed to initialize {unmanagedDelegate.Key}.");
                }
            }
        }
        catch (Exception ex)
        {
            LogUnrealSharp.LogError($"Failed to initialize native functions: {ex}");
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void RegisterFunctions(IntPtr nativeFunctionPtr, char* nativeFunctionName)
    {
        string nativeFunctionNameString = new string(nativeFunctionName);
        
        try
        {
            if (!UnmanagedDelegates.TryGetValue(nativeFunctionNameString, out FieldInfo? unmanagedDelegate))
            {
                throw new Exception($"Failed to find {nativeFunctionNameString} in {nameof(UnmanagedDelegates)}.");
            }
            
            unmanagedDelegate.SetValue(null, nativeFunctionPtr);
        }
        catch (Exception e)
        {
            LogUnrealSharp.Log($"Failed to register native function \"{nativeFunctionNameString}\" exception: {e}");
        }
    }
}