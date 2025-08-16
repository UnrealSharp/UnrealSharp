#include "CSClassInfo.h"
#include "CSAssembly.h"
#include "TypeGenerator/Register/MetaData/CSClassMetaData.h"

UField* FCSClassInfo::StartBuildingManagedType()
{
	if (!Field.IsValid())
	{
		TSharedPtr<const FCSClassMetaData> ClassMetaData = GetTypeMetaData<FCSClassMetaData>();
		UClass* ParentClass = ClassMetaData->ParentClass.GetOwningClass();

		if (!IsValid(ParentClass))
		{
			OwningAssembly->AddPendingClass(ClassMetaData->ParentClass, this);
			return nullptr;
		}
	}

	return FCSManagedTypeInfo::StartBuildingManagedType();
}
