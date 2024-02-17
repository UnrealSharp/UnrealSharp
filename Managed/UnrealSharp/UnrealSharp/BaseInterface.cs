using System.Runtime.InteropServices;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential)]
struct InterfaceData
{
    public IntPtr ObjectPointer;
    public IntPtr InterfacePointer;
}

public static class InterfaceMarshaller
{
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner, IBaseInterface obj)
    {
        if (obj is CoreUObject.Object unrealInterface)
        {
            InterfaceData interfaceData = BlittableMarshaller<InterfaceData>.FromNative(nativeBuffer, arrayIndex, owner);
        }
    }

    public static IBaseInterface FromNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner)
    {
        InterfaceData interfaceData = BlittableMarshaller<InterfaceData>.FromNative(nativeBuffer, arrayIndex, owner);
        CoreUObject.Object unrealObject = ObjectMarshaller<CoreUObject.Object>.FromNative(interfaceData.ObjectPointer, 0, owner);
        return unrealObject as IBaseInterface;
    }
}

public interface IBaseInterface
{
 
}