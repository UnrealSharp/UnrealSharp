#include "SubsystemUtils.h"

bool CSSubsystemUtils::IsReinstancingClass(const UClass* Class)
{
	FString ClassName = Class->GetName();
	return ClassName.StartsWith(TEXT("REINST_"));
}
