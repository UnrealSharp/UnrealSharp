#include "CSFieldName.h"

#include "UnrealSharpUtils.h"
#include "Utilities/CSClassUtilities.h"

FCSFieldName::FCSFieldName(UField* NativeField)
{
	if (UClass* Class = Cast<UClass>(NativeField))
	{
		NativeField = FCSClassUtilities::GetFirstNativeClass(Class);
	}
	
	Name = NativeField->GetFName();
	Namespace = FCSUnrealSharpUtils::GetNamespace(NativeField);
}
