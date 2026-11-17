#include "CSBindsRegistry.h"
#include "Containers/Utf8String.h"

DECLARE_UNREALSHARP_BINDER(Bind_FUtf8String)
{
    void Assign(FUtf8String* Destination, const UTF8CHAR* Bytes, int32 Length)
    {
        *Destination = Length == 0 ? FUtf8String() : FUtf8String::ConstructFromPtrSize(Bytes, Length);
    }

    void Initialize(FUtf8String* Value)
    {
        new (Value) FUtf8String();
    }

    void Destroy(FUtf8String* Value)
    {
        Value->~FUtf8String();
    }

    BIND_UNREALSHARP_FUNCTION(Assign)
    BIND_UNREALSHARP_FUNCTION(Initialize)
    BIND_UNREALSHARP_FUNCTION(Destroy)
}
