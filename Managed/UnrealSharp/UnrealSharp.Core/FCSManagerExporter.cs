using UnrealSharp.Binds;

namespace UnrealSharp.Core;

[NativeCallbacks]
public static unsafe partial class FCSManagerExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr> FindManagedObject;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr> FindOrCreateManagedInterfaceWrapper;
    public static delegate* unmanaged<IntPtr> GetCurrentWorldContext;
    public static delegate* unmanaged<IntPtr> GetCurrentWorldPtr;
    
    public static UnrealSharpObject WorldContextObject
    {
        get
        {
            IntPtr worldContextObject = CallGetCurrentWorldContext();
            IntPtr handle = CallFindManagedObject(worldContextObject);
            return GCHandleUtilities.GetObjectFromHandlePtr<UnrealSharpObject>(handle)!;
        }
    }
}