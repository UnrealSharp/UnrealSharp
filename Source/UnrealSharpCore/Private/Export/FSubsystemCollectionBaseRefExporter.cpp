#include "Export/FSubsystemCollectionBaseRefExporter.h"

USubsystem* UFSubsystemCollectionBaseRefExporter::InitializeDependency(FSubsystemCollectionBase* Collection, UClass* SubsystemClass)
{
    return Collection->InitializeDependency(SubsystemClass);
}
