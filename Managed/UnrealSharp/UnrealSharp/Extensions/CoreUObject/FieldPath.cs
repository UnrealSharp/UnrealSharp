using System.Diagnostics;
using System.Runtime.InteropServices;
using UnrealSharp.Core;
using UnrealSharp.Core.Marshallers;
using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;

namespace UnrealSharp.CoreUObject;

[StructLayout(LayoutKind.Sequential)]
public struct FFieldPathUnsafe
{
    internal IntPtr ResolvedField;
#if WITH_EDITOR
    internal IntPtr InitialFieldClass;
    internal int FieldPathSerialNumber;
#endif
    internal TWeakObjectPtr<UStruct> ResolvedOwner;
    internal UnmanagedArray Path;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct FFieldPath : IEquatable<FFieldPath>
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal FFieldPathUnsafe PathUnsafe;
    // Managed copies share this immutable snapshot, never the native array allocation.
    internal readonly FName[]? Path;

    public bool IsValid
    {
        get
        {
            fixed (FName* path = Path)
            {
                FFieldPathUnsafe view = GetNativeView(path);
                bool result = Bind_FFieldPath.CallIsValid(ref view).ToManagedBool();
                UpdateCache(view);
                return result;
            }
        }
    }

    public bool IsStale
    {
        get
        {
            fixed (FName* path = Path)
            {
                FFieldPathUnsafe view = GetNativeView(path);
                return Bind_FFieldPath.CallIsStale(ref view).ToManagedBool();
            }
        }
    }

    public FFieldPath(FFieldPathUnsafe pathUnsafe)
    {
        Path = new ReadOnlySpan<FName>((void*)pathUnsafe.Path.Data, pathUnsafe.Path.ArrayNum).ToArray();
        PathUnsafe = pathUnsafe;
        PathUnsafe.Path = default;
    }

    internal FFieldPathUnsafe GetNativeView(FName* path)
    {
        // The caller pins Path; this borrowed view must not be destroyed by native code.
        FFieldPathUnsafe view = PathUnsafe;
        view.Path = new UnmanagedArray
        {
            Data = (IntPtr)path,
            ArrayNum = Path?.Length ?? 0,
            ArrayMax = Path?.Length ?? 0
        };
        return view;
    }

    private void UpdateCache(FFieldPathUnsafe view)
    {
        PathUnsafe = view;
        PathUnsafe.Path = default;
    }

    public override string ToString()
    {
        fixed (FName* path = Path)
        {
            FFieldPathUnsafe view = GetNativeView(path);
            UnmanagedArray buffer = new();
            try
            {
                Bind_FFieldPath.CallFieldPathToString(ref view, ref buffer);
                UpdateCache(view);
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
        fixed (FName* path = Path)
        fixed (FName* otherPath = other.Path)
        {
            FFieldPathUnsafe view = GetNativeView(path);
            FFieldPathUnsafe otherView = other.GetNativeView(otherPath);
            return Bind_FFieldPath.CallFieldPathsEqual(ref view, ref otherView).ToManagedBool();
        }
    }

    public override bool Equals(object? obj)
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
        fixed (FName* path = Path)
        {
            FFieldPathUnsafe view = GetNativeView(path);
            return Bind_FFieldPath.CallGetFieldPathHashCode(ref view);
        }
    }
}

public static unsafe class FieldPathMarshaller
{
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, FFieldPath obj)
    {
        fixed (FName* path = obj.Path)
        {
            FFieldPathUnsafe view = obj.GetNativeView(path);
            Bind_FFieldPath.CallAssign(nativeBuffer, arrayIndex, ref view);
        }
    }

    public static FFieldPath FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        var fieldPathUnsafe = BlittableMarshaller<FFieldPathUnsafe>.FromNative(nativeBuffer, arrayIndex);
        return new FFieldPath(fieldPathUnsafe);
    }
}
