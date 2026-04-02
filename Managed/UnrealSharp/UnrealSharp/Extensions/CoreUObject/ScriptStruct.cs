using UnrealSharp.Core;
using UnrealSharp.Interop;

namespace UnrealSharp.CoreUObject;

public partial class UScriptStruct
{
    public Type? ManagedType
    {
        get
        {
            IntPtr managedStruct = UScriptStructExporter.CallGetManagedStructType(NativeObject);
            return GCHandleUtilities.GetObjectFromHandlePtr<Type>(managedStruct);
        }
    }
    
    public static UScriptStruct GetScriptStruct<T>() where T : MarshalledStruct<T>
    {
        IntPtr structPtr = T.GetNativeClassPtr();
        IntPtr handle = FCSManagerExporter.CallFindManagedObject(structPtr);
        return GCHandleUtilities.GetObjectFromHandlePtrFast<UScriptStruct>(handle)!;
    }
}