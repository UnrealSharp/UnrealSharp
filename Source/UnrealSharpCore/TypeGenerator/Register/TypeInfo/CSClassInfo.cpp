#include "CSClassInfo.h"

#include "CSAssembly.h"

FCSharpClassInfo::FCSharpClassInfo(const TSharedPtr<FJsonValue>& MetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly) : TCSharpTypeInfo(MetaData, InOwningAssembly)
{
	TypeHandle = InOwningAssembly->TryFindTypeHandle(TypeMetaData->FieldName);
}

FCSharpClassInfo::FCSharpClassInfo(UClass* InField, const TSharedPtr<FCSAssembly>& InOwningAssembly, const TWeakPtr<FGCHandle>& InTypeHandle)
{
	Field = InField;
	OwningAssembly = InOwningAssembly;
	TypeHandle = InTypeHandle;
}

UClass* FCSharpClassInfo::InitializeBuilder()
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

	return TCSharpTypeInfo::InitializeBuilder();
}

TWeakPtr<FGCHandle> FCSharpClassInfo::GetTypeHandle()
{
	if (!TypeHandle.IsValid())
	{
		TypeHandle = OwningAssembly->TryFindTypeHandle(TypeMetaData->FieldName);
	}

	return TypeHandle;
}

