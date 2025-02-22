#include "CSFieldName.h"

#include "UnrealSharpUtilities/UnrealSharpUtils.h"

FCSFieldName::FCSFieldName(FName Name, FName Namespace): Name(Name), Namespace(Namespace)
{
}

FCSFieldName::FCSFieldName(const UClass* Class)
{
	FString ClassName = Class->GetName();
	if (Class->HasAllClassFlags(CLASS_CompiledFromBlueprint))
	{
		ClassName.RemoveFromEnd(TEXT("_C"));
	}
		
	Name = *ClassName;
	Namespace = FUnrealSharpUtils::GetNamespace(Class);
}
