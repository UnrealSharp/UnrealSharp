#include "CSClassInfo.h"
#include "CSAssembly.h"
#include "MetaData/CSClassMetaData.h"

void FCSClassInfo::OnStructureChanged()
{
	FCSManagedTypeInfo::OnStructureChanged();

	if (!IsValid(Field))
	{
		// Happens the first time a class is created, no need to propagate changes as there are no derived classes yet.
		return;
	}

	// When a class changes, all derived classes must also be marked as changed.
	// Blueprint compiler will handle the Blueprints that are affected.
	TArray<UClass*> DerivedClasses;
	UClass* Class = GetFieldChecked<UClass>();
	GetDerivedClasses(Class, DerivedClasses, false);

	for (UClass* DerivedClass : DerivedClasses)
	{
		if (!FCSClassUtilities::IsManagedClass(DerivedClass))
		{
			continue;
		}
		
		UCSClass* ManagedClass = static_cast<UCSClass*>(DerivedClass);
		TSharedPtr<FCSClassInfo> DerivedClassInfo = ManagedClass->GetManagedTypeInfo<FCSClassInfo>();
		DerivedClassInfo->OnStructureChanged();
	}
}

UField* FCSClassInfo::StartBuildingType()
{
	if (!IsValid(Field))
	{
		TSharedPtr<const FCSClassMetaData> ClassMetaData = GetTypeMetaData<FCSClassMetaData>();
		UClass* ParentClass = ClassMetaData->ParentClass.GetAsClass();

		if (!IsValid(ParentClass))
		{
			OwningAssembly->AddPendingClass(ClassMetaData->ParentClass, this);
			return nullptr;
		}
	}

	return FCSManagedTypeInfo::StartBuildingType();
}
