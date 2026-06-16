using UnrealSharp.Core;
using UnrealSharp.Core.Marshallers;

namespace UnrealSharp.Interop.Properties;

internal class NativeProperty
{
    internal NativeProperty(IntPtr property)
    {
        Property = property;
    }
    
    internal readonly IntPtr Property;
    internal int Offset => Bind_FProperty.CallGetPropertyOffset(Property);
    internal int Size => Bind_FProperty.CallGetSize(Property);
    internal int ArrayDim => Bind_FProperty.CallGetArrayDim(Property);
    
    internal void DestroyValue(IntPtr valueAddress) => Bind_FProperty.CallDestroyValue(Property, valueAddress);
    internal void DestroyValue_Container(IntPtr valueAddress) => Bind_FProperty.CallDestroyValue_InContainer(Property, valueAddress);
    internal void InitializeValue(IntPtr valueAddress) => Bind_FProperty.CallInitializeValue(Property, valueAddress);
    internal IntPtr ValueAddress(IntPtr baseAddress) => baseAddress + Offset;
    internal bool Identical(IntPtr value1, IntPtr value2) => Bind_FProperty.CallIdentical(Property, value1, value2).ToManagedBool();
    internal uint GetValueTypeHash(IntPtr source) => Bind_FProperty.CallGetValueTypeHash(Property, source);
    internal bool HasAnyPropertyFlags(NativePropertyFlags flags) => Bind_FProperty.CallHasAnyPropertyFlags(Property, flags).ToManagedBool();
    internal bool HasAllPropertyFlags(NativePropertyFlags flags) => Bind_FProperty.CallHasAllPropertyFlags(Property, flags).ToManagedBool();
    internal void CopySingleValue(IntPtr dest, IntPtr src) => Bind_FProperty.CallCopySingleValue(Property, dest, src);
}