#include "CSClassInfo.h"
#include "CSAssembly.h"
#include "TypeGenerator/Register/MetaData/CSClassMetaData.h"

UField* FCSClassInfo::StartBuildingType()
{
	UClass* Class = GetField<UClass>();
	if (!IsValid(Class) || !IsValid(Class->GetSuperClass()))
	{
		TSharedPtr<const FCSClassMetaData> ClassMetaData = GetTypeMetaData<FCSClassMetaData>();
		UClass* ParentClass = ClassMetaData->ParentClass.GetOwningClass();

		if (!ParentClass)
		{
			OwningAssembly->AddPendingClass(ClassMetaData->ParentClass, this);
			return nullptr;
		}
	}

	return FCSManagedTypeInfo::StartBuildingType();
}
