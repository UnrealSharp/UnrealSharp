using UnrealSharp.Core;
using UnrealSharp.Interop;

namespace UnrealSharp.CoreUObject;

public partial class UEnum
{
    public Type? ManagedType
    {
        get
        {
            IntPtr managedStruct = UEnumExporter.CallGetManagedEnumType(NativeObject);
            return GCHandleUtilities.GetObjectFromHandlePtr<Type>(managedStruct);
        }
    }
}