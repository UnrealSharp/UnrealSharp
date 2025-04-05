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
    internal int Offset => FPropertyExporter.CallGetPropertyOffset(Property);
    internal int Size => FPropertyExporter.CallGetSize(Property);
    internal int ArrayDim => FPropertyExporter.CallGetArrayDim(Property);
    
    internal void DestroyValue(IntPtr valueAddress) => FPropertyExporter.CallDestroyValue(Property, valueAddress);
    internal void DestroyValue_Container(IntPtr valueAddress) => FPropertyExporter.CallDestroyValue_InContainer(Property, valueAddress);
    internal void InitializeValue(IntPtr valueAddress) => FPropertyExporter.CallInitializeValue(Property, valueAddress);
    internal IntPtr ValueAddress(IntPtr baseAddress) => baseAddress + Offset;
    internal bool Identical(IntPtr value1, IntPtr value2) => FPropertyExporter.CallIdentical(Property, value1, value2).ToManagedBool();
    internal uint GetValueTypeHash(IntPtr source) => FPropertyExporter.CallGetValueTypeHash(Property, source);
    internal bool HasAnyPropertyFlags(NativePropertyFlags flags) => FPropertyExporter.CallHasAnyPropertyFlags(Property, flags).ToManagedBool();
    internal bool HasAllPropertyFlags(NativePropertyFlags flags) => FPropertyExporter.CallHasAllPropertyFlags(Property, flags).ToManagedBool();
    internal void CopySingleValue(IntPtr dest, IntPtr src) => FPropertyExporter.CallCopySingleValue(Property, dest, src);
}