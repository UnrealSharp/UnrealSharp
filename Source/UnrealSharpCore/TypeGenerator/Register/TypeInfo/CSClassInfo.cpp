#include "CSClassInfo.h"
#include "CSAssembly.h"
#include "Utils/CSClassUtilities.h"

FCSClassInfo::FCSClassInfo(const TSharedPtr<FJsonValue>& MetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly) : TCSTypeInfo(MetaData, InOwningAssembly)
{
	ManagedTypeHandle = InOwningAssembly->TryFindTypeHandle(TypeMetaData->FieldName);
}

FCSClassInfo::FCSClassInfo(UClass* InField, const TSharedPtr<FCSAssembly>& InOwningAssembly, const TSharedPtr<FGCHandle>& InTypeHandle)
{
	Field = InField;
	OwningAssembly = InOwningAssembly;
	ManagedTypeHandle = InTypeHandle;
}

UClass* FCSClassInfo::InitializeBuilder()
{
	if (Field && Field->HasAllClassFlags(CLASS_Native))
	{
		return Field;
	}

	if (!IsValid(Field) || !IsValid(Field->GetSuperClass()))
	{
		UClass* ParentClass = TypeMetaData->ParentClass.GetOwningClass();

		if (!ParentClass)
		{
			OwningAssembly->AddPendingClass(TypeMetaData->ParentClass, this);
			return nullptr;
		}
	}

	return TCSTypeInfo::InitializeBuilder();
}

