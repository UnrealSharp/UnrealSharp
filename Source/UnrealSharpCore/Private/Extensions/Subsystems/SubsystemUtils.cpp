#include "Extensions/Subsystems/SubsystemUtils.h"

bool FCSSubsystemUtils::IsReinstancingClass(const UClass* Class)
{
	FString ClassName = Class->GetName();
	return ClassName.StartsWith(TEXT("REINST_"));
}