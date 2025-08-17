#include "CSFieldName.h"
#include "UnrealSharpUtilities/UnrealSharpUtils.h"
#include "Utils/CSClassUtilities.h"

FCSFieldName::FCSFieldName(UField* Field)
{
	if (UClass* Class = Cast<UClass>(Field))
	{
		Field = FCSClassUtilities::GetFirstNativeClass(Class);
	}
	
	Name = Field->GetFName();
	Namespace = FCSUnrealSharpUtils::GetNamespace(Field);
}
