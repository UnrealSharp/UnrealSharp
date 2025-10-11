using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using UnrealSharp.Core.Interop;

namespace UnrealSharp.Core.Marshallers;

public class OptionMarshaller<T>(IntPtr nativeProperty, MarshallingDelegates<T>.ToNative toNative, MarshallingDelegates<T>.FromNative fromNative)
{
    public void ToNative(IntPtr nativeBuffer, int arrayIndex, Option<T> obj)
    {
        if (obj.IsSome)
        {
            var result = FOptionalPropertyExporter.CallMarkSetAndGetInitializedValuePointerToReplace(nativeProperty, nativeBuffer);
            toNative(result, 0, obj.ValueUnsafe());
        }
        else
        {
            FOptionalPropertyExporter.CallMarkUnset(nativeProperty, nativeBuffer);
        }
    }

    public Option<T> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        if (!FOptionalPropertyExporter.CallIsSet(nativeProperty, nativeBuffer).ToManagedBool()) return Option<T>.None;
        var result = FOptionalPropertyExporter.CallGetValuePointerForRead(nativeProperty, nativeBuffer);
        return fromNative(result, 0);
    }
    
    public void DestructInstance(IntPtr nativeBuffer, int arrayIndex)
    {
        FOptionalPropertyExporter.CallDestructInstance(nativeProperty, nativeBuffer);
    }
}