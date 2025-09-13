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
}