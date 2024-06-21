using UnrealSharp.Interop;

namespace UnrealSharp;

internal class NativeProperty(IntPtr property, IntPtr? address = null)
{
    internal IntPtr Property = property;
    internal IntPtr? Address = address;
    
    public int Offset => FPropertyExporter.CallGetPropertyOffset(Property);
    public int Size => FPropertyExporter.CallGetSize(Property);
    public int ArrayDim => FPropertyExporter.CallGetArrayDim(Property);

    public NativeProperty? GetInnerField(int index)
    {
        List<NativeProperty> innerFields = GetInnerFields();
        return index < innerFields.Count ? innerFields[index] : null;
    }
    
    public List<NativeProperty> GetInnerFields()
    {
        List<NativeProperty> innerFields = [];
        UnmanagedArray fields = new();
        FPropertyExporter.CallGetInnerFields(Property, ref fields);
        for (int i = 0; i < fields.ArrayNum; i++)
        {
            IntPtr innerField = BlittableMarshaller<IntPtr>.FromNative(fields.Data, i);
            if (innerField != IntPtr.Zero)
            {
                innerFields.Add(new NativeProperty(innerField));
            }
        }
        return innerFields;
    }
    
    public void DestroyValue(IntPtr valueAddress) => FPropertyExporter.CallDestroyValue(Property, valueAddress);
    public void DestroyValue()
    {
        if (!Address.HasValue)
        {
            return;
        }
        
        FPropertyExporter.CallDestroyValue(Property, Address.Value);
        Address = null;
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