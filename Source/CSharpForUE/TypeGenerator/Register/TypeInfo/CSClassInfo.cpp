#include "CSClassInfo.h"
#include "CSharpForUE/TypeGenerator/Register/CSTypeRegistry.h"

UClass* FCSharpClassInfo::InitializeBuilder()
{
	if (Field)
	{
		return Field;
	}
	
	FName ParentClassName = TypeMetaData->ParentClass.Name;
	UClass* ParentClass = FCSTypeRegistry::GetClassFromName(ParentClassName);

	if (!ParentClass)
	{
		FCSTypeRegistry::Get().AddPendingClass(ParentClassName, this);
		return nullptr;
	}
	
	return TCSharpTypeInfo::InitializeBuilder();
}

