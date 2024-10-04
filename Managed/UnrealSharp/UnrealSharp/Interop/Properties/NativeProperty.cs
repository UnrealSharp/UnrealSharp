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
    internal List<NativeProperty> InnerFields => GetInnerFields();

    internal NativeProperty? GetInnerField(int index)
    {
        List<NativeProperty> innerFields = GetInnerFields();
        return index < innerFields.Count ? innerFields[index] : null;
    }
    
    internal List<NativeProperty> GetInnerFields()
    {
        List<NativeProperty> innerFields = [];
        UnmanagedArray fields = new();
        FPropertyExporter.CallGetInnerFields(Property, ref fields);
        for (int i = 0; i < fields.ArrayNum; i++)
        {
            IntPtr innerField = BlittableMarshaller<IntPtr>.FromNative(fields.Data, i);
            if (innerField == IntPtr.Zero)
            {
                continue;
            }
            innerFields.Add(new NativeProperty(innerField));
        }
        return innerFields;
    }
    
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