using System.Diagnostics;
using System.Runtime.InteropServices;
using UnrealSharp.Core;
using UnrealSharp.Core.Marshallers;
using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential)]
public struct FFieldPathUnsafe
{
    internal IntPtr ResolvedField;
#if !PACKAGE
    internal IntPtr InitialFieldClass;
    internal int FieldPathSerialNumber;
#endif
    internal TWeakObjectPtr<UStruct> ResolvedOwner;
    internal UnmanagedArray Path;
}

[StructLayout(LayoutKind.Sequential)]
public struct FFieldPath : IEquatable<FFieldPath>
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal FFieldPathUnsafe PathUnsafe;

    public bool IsValid => FFieldPathExporter.CallIsValid(ref PathUnsafe).ToManagedBool();
    public bool IsStale => FFieldPathExporter.CallIsStale(ref PathUnsafe).ToManagedBool();

    public FFieldPath(FFieldPathUnsafe pathUnsafe)
    {
        PathUnsafe = pathUnsafe;
    }

    public override string ToString()
    {
        unsafe
        {
            UnmanagedArray buffer = new();
            try
            {
                FFieldPathExporter.CallFieldPathToString(ref PathUnsafe, ref buffer);
                return new string((char*)buffer.Data);
            }
            finally
            {
                buffer.Destroy();
            }
        }
    }

    public bool Equals(FFieldPath other)
    {
        return FFieldPathExporter.CallFieldPathsEqual(ref PathUnsafe, ref other.PathUnsafe).ToManagedBool();
    }

    public override bool Equals(object obj)
    {
        return obj is FFieldPath path && Equals(path);
    }

    public static bool operator ==(FFieldPath lhs, FFieldPath rhs)
    {
        return lhs.Equals(rhs);
    }

    public static bool operator !=(FFieldPath lhs, FFieldPath rhs)
    {
        return !lhs.Equals(rhs);
    }

    public override int GetHashCode()
    {
        return FFieldPathExporter.CallGetFieldPathHashCode(ref PathUnsafe);
    }
}

public static class FieldPathMarshaller
{
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, FFieldPath obj)
    {
        BlittableMarshaller<FFieldPathUnsafe>.ToNative(nativeBuffer, arrayIndex, obj.PathUnsafe);
    }

    public static FFieldPath FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        var fieldPathUnsafe = BlittableMarshaller<FFieldPathUnsafe>.FromNative(nativeBuffer, arrayIndex);
        return new FFieldPath(fieldPathUnsafe);
    }
}