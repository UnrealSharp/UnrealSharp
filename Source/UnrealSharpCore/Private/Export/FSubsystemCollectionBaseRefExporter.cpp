#include "CSBindsManager.h"

DECLARE_UNREALSHARP_EXPORTER(FSubsystemCollectionBaseRefExporter)
{
    USubsystem* InitializeDependency(FSubsystemCollectionBase* Collection, UClass* SubsystemClass)
    {
        return Collection->InitializeDependency(SubsystemClass);
    }
    
    EXPORT_UNREALSHARP_FUNCTION(InitializeDependency)
}
