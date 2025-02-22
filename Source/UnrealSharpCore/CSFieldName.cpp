#include "CSFieldName.h"

#include "UnrealSharpUtilities/UnrealSharpUtils.h"

FCSFieldName::FCSFieldName(FName Name, FName Namespace): Name(Name), Namespace(Namespace)
{
}

FCSFieldName::FCSFieldName(const UClass* Class)
{
	if (Class->HasAllClassFlags(CLASS_CompiledFromBlueprint))
	{
		FString ClassName = Class->GetName();
		ClassName.RemoveFromEnd(TEXT("_C"));
		Name = *ClassName;
	}
	else
	{
		Name = Class->GetFName();
	}
	
	Namespace = FUnrealSharpUtils::GetNamespace(Class);
}
