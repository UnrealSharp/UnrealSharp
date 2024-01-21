using System.Runtime.InteropServices;
using UnrealSharp.Interop;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential)]
struct FText
{
    public IntPtr ObjectPtr;
    public IntPtr SharedReferenceCount;
    public uint Flags;
}

public class Text : IEquatable<Text>, IComparable<Text>
{
    readonly UnrealSharpObject _owner;
    readonly IntPtr _nativeInstance;
    readonly SharedPtrTheadSafe _data;

    internal static unsafe int NativeSize => sizeof(FText);
    
    internal Text(IntPtr nativeInstance)
    {
        _nativeInstance = nativeInstance;
        _owner = null;
        _data = new SharedPtrTheadSafe(nativeInstance);
    }

    internal Text(UnrealSharpObject ownerSharp, IntPtr nativeBuffer)
    {
        _owner = ownerSharp;
        CheckOwnerObject();
        _nativeInstance = nativeBuffer;
        
        //Don't own a reference we're referring to a property
        _data = ownerSharp != null ? SharedPtrTheadSafe.NonReferenceOwningSharedPtr(_nativeInstance) : new SharedPtrTheadSafe(_nativeInstance);
    }
    
    internal Text(UnrealSharpObject ownerSharp, int propertyOffset)
    {
        _owner = ownerSharp;
        CheckOwnerObject();
        _nativeInstance = IntPtr.Add(ownerSharp.NativeObject, propertyOffset);
        _data = SharedPtrTheadSafe.NonReferenceOwningSharedPtr(_nativeInstance);
    }
    
    private Text()
    {
        _nativeInstance = Marshal.AllocHGlobal(NativeSize);
        _data = SharedPtrTheadSafe.NewNulledSharedPtr(_nativeInstance);
    }
    
    public Text(string text) : this()
    {
        FTextExporter.CallFromString(_nativeInstance, text);
    }
    
    public Text(Name text) : this(text.ToString()) {}
    
    ~Text()
    {
        if (_owner == null || _nativeInstance == IntPtr.Zero)
        {
            return;
        }
        
        _data.Dispose();
        Marshal.FreeHGlobal(_nativeInstance);
    }
    
    public static Text GetEmpty()
    {
        Text result = new Text();
        FTextExporter.CallCreateEmptyText(result._nativeInstance);
        return result;
    }
    
    public override string ToString() 
    {
        unsafe
        {
            CheckOwnerObject(); 
            return new string(FTextExporter.CallToString(_nativeInstance));
        }
    }
    
    public bool Equals(Text other)
    {
        return CompareTo(other) == 0;
    }
    
    public int CompareTo(Text other)
    {
        CheckOwnerObject(); 
        return (byte) FTextExporter.CallCompare(_nativeInstance, other._nativeInstance);
    }
    
    public bool IsEmpty
    {
        get 
        {
            CheckOwnerObject();
            return FTextExporter.CallIsEmpty(_nativeInstance).ToManagedBool();
        }
    }
    
    private void CheckOwnerObject()
    {
        if (_owner != null && _owner.NativeObject == IntPtr.Zero)
        { 
            throw new UnrealObjectDestroyedException("Trying to access Text UProperty on destroyed object of type " + _owner.GetType().ToString());
        }
    }
    public unsafe void CopyFrom(Text other)
    {
        other.CheckOwnerObject();
        _data.CopyFrom(other._data);

        ((FText*)_nativeInstance)->Flags = ((FText*)other._nativeInstance)->Flags;
    }
    public Text Clone()
    {
        Text result = new Text();
        result.CopyFrom(this);
        return result;
    }
}

public class TextMarshaller(int length)
{
    private readonly Text[] _wrapper = new Text[length];

    public void ToNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner, Text obj)
    {
        _wrapper[arrayIndex] ??= new Text(owner, nativeBuffer + arrayIndex * Text.NativeSize);
        _wrapper[arrayIndex].CopyFrom(obj);
    }

    public Text FromNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner)
    {
        return _wrapper[arrayIndex] ?? (_wrapper[arrayIndex] = new Text(owner, nativeBuffer + arrayIndex * Text.NativeSize));
    }
}