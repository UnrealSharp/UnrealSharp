#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_FSubsystemCollectionBaseRef)
{
    USubsystem* InitializeDependency(FSubsystemCollectionBase* Collection, UClass* SubsystemClass)
    {
        return Collection->InitializeDependency(SubsystemClass);
    }
    
    BIND_UNREALSHARP_FUNCTION(InitializeDependency)
}
