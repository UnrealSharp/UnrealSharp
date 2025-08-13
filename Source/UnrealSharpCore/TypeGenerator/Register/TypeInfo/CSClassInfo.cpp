#include "CSClassInfo.h"

#include "CSAssembly.h"
#include "TypeGenerator/CSClass.h"
#include "TypeGenerator/Register/MetaData/CSClassMetaData.h"

FCSClassInfo::FCSClassInfo(const TSharedPtr<FCSTypeReferenceMetaData>& MetaData, UCSAssembly* InOwningAssembly, UClass* InClass)
: FCSManagedTypeInfo(MetaData, InOwningAssembly, InClass)
{
	
}

FCSClassInfo::FCSClassInfo(UClass* InField, UCSAssembly* InOwningAssembly, const TSharedPtr<FGCHandle>& TypeHandle)
: FCSManagedTypeInfo(nullptr, InOwningAssembly, UCSClass::StaticClass())
{
	Field = InField;
	OwningAssembly = InOwningAssembly;
	ManagedTypeHandle = TypeHandle;
}

UField* FCSClassInfo::InitializeBuilder()
{
	UClass* Class = GetField<UClass>();
	
	if (Class && Class->HasAllClassFlags(CLASS_Native))
	{
		return Field;
	}

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

	return FCSManagedTypeInfo::InitializeBuilder();
}
