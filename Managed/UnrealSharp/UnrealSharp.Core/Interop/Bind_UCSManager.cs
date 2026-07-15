using UnrealSharp.Binds;

namespace UnrealSharp.Core.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_UCSManager
{
    public static delegate* unmanaged<IntPtr, IntPtr> FindManagedObject;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr> FindOrCreateManagedInterfaceWrapper;
    public static delegate* unmanaged<IntPtr> GetCurrentWorldContext;
    public static delegate* unmanaged<IntPtr> GetCurrentWorldPtr;
    public static delegate* unmanaged<IntPtr, void> PushWorldContext;
    public static delegate* unmanaged<void> PopWorldContext;
    
    public static UnrealSharpObject? WorldContextObject
    {
        get
        {
            IntPtr worldContextObject = CallGetCurrentWorldContext();
            if (worldContextObject == IntPtr.Zero)
            {
                return null;
            }

            IntPtr handle = CallFindManagedObject(worldContextObject);
            return GCHandleUtilities.GetObjectFromHandlePtr<UnrealSharpObject>(handle);
        }
    }
}