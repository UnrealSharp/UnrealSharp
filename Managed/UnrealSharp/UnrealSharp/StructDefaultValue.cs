using UnrealSharp.Core;
using UnrealSharp.Core.Marshallers;
using UnrealSharp.Interop;

namespace UnrealSharp;

/// <summary>Imports a struct parameter default using Unreal's native text format.</summary>
public static class StructDefaultValue
{
    public static unsafe T Parse<T>(Type ownerType, string ownerNativeName, string functionName,
        string parameterName, string text, MarshallingDelegates<T>.FromNative fromNative)
    {
        string parameterPath = $"{ownerNativeName}.{functionName}.{parameterName}";
        IntPtr nativeClass = NativeReflectionHelper.GetNativeField(ownerType, ownerNativeName);
        if (nativeClass == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Cannot resolve native owner for struct default {parameterPath}.");
        }

        IntPtr function = Bind_UClass.CallGetNativeFunctionFromClassAndName(nativeClass, functionName);
        if (function == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Cannot resolve native function for struct default {parameterPath}.");
        }

        IntPtr property = Bind_FProperty.CallGetNativePropertyFromName(function, parameterName);
        if (property == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Cannot resolve native parameter for struct default {parameterPath}.");
        }

        IntPtr value;
        fixed (char* textPointer = text)
        {
            value = Bind_FProperty.CallCreateStructDefaultValue(property, textPointer, text.Length);
        }

        if (value == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Cannot import struct default {parameterPath} from '{text}'.");
        }

        try
        {
            // Struct and blittable marshallers produce independent managed values.
            return fromNative(value, 0);
        }
        finally
        {
            Bind_FProperty.CallDestroyStructDefaultValue(property, value);
        }
    }
}
