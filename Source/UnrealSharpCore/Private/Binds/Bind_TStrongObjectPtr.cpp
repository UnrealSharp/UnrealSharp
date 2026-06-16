#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_TStrongObjectPtr)
{
    void ConstructStrongObjectPtr(TStrongObjectPtr<UObject>* Ptr, UObject* Object)
    {
        static_assert(sizeof(TStrongObjectPtr<UObject>) == sizeof(UObject*), "TStrongObjectPtr<UObject> must be the same size as UObject*");
        check(Ptr != nullptr);
        std::construct_at(Ptr, Object);
    }

    void DestroyStrongObjectPtr(TStrongObjectPtr<UObject>* Ptr)
    {
        static_assert(sizeof(TStrongObjectPtr<UObject>) == sizeof(UObject*), "TStrongObjectPtr<UObject> must be the same size as UObject*");
        check(Ptr != nullptr);
        std::destroy_at(Ptr);
    }
    
    BIND_UNREALSHARP_FUNCTION(ConstructStrongObjectPtr)
    BIND_UNREALSHARP_FUNCTION(DestroyStrongObjectPtr)
}
