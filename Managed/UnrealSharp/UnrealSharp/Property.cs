using UnrealSharp.Interop;

namespace UnrealSharp;

internal class NativeProperty
{
    public NativeProperty(IntPtr property, IntPtr? address = null)
    {
        Property = property;
        Address = address;
    }
    
    internal IntPtr Property;
    internal IntPtr? Address;
    
    public int Offset => FPropertyExporter.CallGetPropertyOffset(Property);
    public int Size => FPropertyExporter.CallGetSize(Property);
    public int ArrayDim => FPropertyExporter.CallGetArrayDim(Property);
    
    public void DestroyValue(IntPtr valueAddress) => FPropertyExporter.CallDestroyValue(Property, valueAddress);
    public void DestroyValue()
    {
        if (Address.HasValue)
        {
            FPropertyExporter.CallDestroyValue(Property, Address.Value);
            Address = null;
        }
    }

    public void InitializeValue(IntPtr valueAddress) => FPropertyExporter.CallInitializeValue(Property, valueAddress);
    public void InitializeValue()
    {
        if (Address.HasValue)
        {
            FPropertyExporter.CallInitializeValue(Property, Address.Value);
        }
    }
    
    public IntPtr ValueAddress(IntPtr baseAddress) => baseAddress + Offset;
}