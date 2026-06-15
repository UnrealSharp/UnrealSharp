#include "CSBindsManager.h"

DECLARE_UNREALSHARP_EXPORTER(TStrongObjectPtrExporter)
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
    
    EXPORT_UNREALSHARP_FUNCTION(ConstructStrongObjectPtr)
    EXPORT_UNREALSHARP_FUNCTION(DestroyStrongObjectPtr)
}
