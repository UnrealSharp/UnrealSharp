#include "CSClassInfo.h"

#include "CSAssembly.h"

FCSharpClassInfo::FCSharpClassInfo(const TSharedPtr<FJsonValue>& MetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly) : TCSharpTypeInfo(MetaData, InOwningAssembly)
{
	TypeHandle = InOwningAssembly->TryFindTypeHandle(TypeMetaData->FieldName);
}

FCSharpClassInfo::FCSharpClassInfo(UClass* InField, const TSharedPtr<FCSAssembly>& InOwningAssembly, const TSharedPtr<FGCHandle>& InTypeHandle)
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

TSharedPtr<FGCHandle> FCSharpClassInfo::GetTypeHandle()
{
#if WITH_EDITOR
	if (!TypeHandle.IsValid() || TypeHandle->IsNull())
	{
		// Lazy load the type handle in editor. Gets null during hot reload.
		TypeHandle = OwningAssembly->TryFindTypeHandle(TypeMetaData->FieldName);
	}
#endif

	return TypeHandle;
}

