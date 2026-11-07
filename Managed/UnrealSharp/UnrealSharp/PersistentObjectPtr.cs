using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using UnrealSharp.Core;
using UnrealSharp.Core.Interop;
using UnrealSharp.Core.Marshallers;
using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential)]
public struct FPersistentObjectPtrData<ObjectId> where ObjectId : struct
{ 
    public WeakObjectData _weakPtr;
    public ObjectId _objectId;
}

[StructLayout(LayoutKind.Sequential)]
public struct FSoftObjectPathUnsafe
{
    public FTopLevelAssetPath AssetPath;
    public UnmanagedArray SubPathString;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct FPersistentObjectPtr : IEquatable<FPersistentObjectPtr>
{
    private FSoftObjectPath _path;
    private WeakObjectData _weak;

    internal FSoftObjectPath Path => _path;

    public FPersistentObjectPtr(UObject obj)
    {
        if (!obj.IsValid())
        {
            return;
        }

        _path = ReadPathFromObject(obj.NativeObject);
    }

    internal FPersistentObjectPtr(FSoftObjectPath path)
    {
        _path = path;
    }

    internal static FPersistentObjectPtr FromObject(IntPtr obj)
    {
        return new FPersistentObjectPtr(ReadPathFromObject(obj));
    }

    internal IntPtr GetNativePointer(bool load = false)
    {
        return ResolvePath(Path, ref _weak, load);
    }

    public FSoftObjectPath GetUniqueId()
    {
        return Path;
    }

    public UObject? Get()
    {
        return GetManagedObject(Path, ref _weak);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not FPersistentObjectPtr other)
        {
            return false;
        }

        return GetNativePointer() == other.GetNativePointer();
    }

    public bool Equals(FPersistentObjectPtr other)
    {
        return Equals((object)other);
    }

    public override int GetHashCode()
    {
        return GetPathHash(Path);
    }

    public static bool operator ==(FPersistentObjectPtr left, FPersistentObjectPtr right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(FPersistentObjectPtr left, FPersistentObjectPtr right)
    {
        return !(left == right);
    }

    internal static FSoftObjectPath ReadPathFromNative(IntPtr source, int index)
    {
        return FSoftObjectPath.FromNative(Bind_FSoftObjectPtr.CallReadSoft(source, index));
    }

    private static FSoftObjectPath ReadPathFromObject(IntPtr source)
    {
        FTopLevelAssetPath assetPath = default;
        UnmanagedArray subPath;
        Bind_FUtf8String.CallInitialize(&subPath);
        try
        {
            Bind_FSoftObjectPtr.CallFromObject(source, ref assetPath, ref subPath);
            return new FSoftObjectPath(assetPath, Utf8StringMarshaller.FromNative((IntPtr)(&subPath), 0));
        }
        finally
        {
            Utf8StringMarshaller.DestructInstance((IntPtr)(&subPath), 0);
        }
    }

    internal static void WritePathToNative(IntPtr destination, int index, FSoftObjectPath path)
    {
        path.ToNative(Bind_FSoftObjectPtr.CallWriteSoft(destination, index));
    }

    private static IntPtr ResolvePath(FSoftObjectPath path, ref WeakObjectData weak, bool load = false)
    {
        FTopLevelAssetPath assetPath = path.AssetPathValue;
        ReadOnlySpan<byte> subPath = path.SubPathValue.Bytes;
        fixed (byte* text = subPath)
            return Bind_FSoftObjectPtr.CallResolve(ref weak, ref assetPath, text, subPath.Length, load ? (byte)1 : (byte)0);
    }

    private static UObject? GetManagedObject(FSoftObjectPath path, ref WeakObjectData weak)
    {
        FTopLevelAssetPath assetPath = path.AssetPathValue;
        ReadOnlySpan<byte> subPath = path.SubPathValue.Bytes;
        fixed (byte* text = subPath)
        {
            IntPtr handle = Bind_FSoftObjectPtr.CallGet(ref weak, ref assetPath, text, subPath.Length);
            return GCHandleUtilities.GetObjectFromHandlePtr<UObject>(handle);
        }
    }

    private static int GetPathHash(FSoftObjectPath path)
    {
        FTopLevelAssetPath assetPath = path.AssetPathValue;
        ReadOnlySpan<byte> subPath = path.SubPathValue.Bytes;
        fixed (byte* text = subPath)
            return Bind_FSoftObjectPtr.CallPathHash(ref assetPath, text, subPath.Length);
    }
}
