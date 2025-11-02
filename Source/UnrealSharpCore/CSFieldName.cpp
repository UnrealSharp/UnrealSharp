#include "CSFieldName.h"
#include "UnrealSharpUtilities/UnrealSharpUtils.h"
#include "Utils/CSClassUtilities.h"

FCSFieldName::FCSFieldName(UField* NativeField)
{
	if (UClass* Class = Cast<UClass>(NativeField))
	{
		NativeField = FCSClassUtilities::GetFirstNativeClass(Class);
	}
	
	Name = NativeField->GetFName();
	Namespace = FCSUnrealSharpUtils::GetNamespace(NativeField);
}
