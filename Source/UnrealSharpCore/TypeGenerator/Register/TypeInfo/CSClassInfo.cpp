#include "CSClassInfo.h"
#include "CSAssembly.h"

FCSharpClassInfo::FCSharpClassInfo(const TSharedPtr<FJsonValue>& MetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly) : TCSharpTypeInfo(MetaData, InOwningAssembly)
{
	ManagedTypeHandle = InOwningAssembly->TryFindTypeHandle(TypeMetaData->FieldName);
}

FCSharpClassInfo::FCSharpClassInfo(UClass* InField, const TSharedPtr<FCSAssembly>& InOwningAssembly, const TSharedPtr<FGCHandle>& InTypeHandle)
{
	Field = InField;
	OwningAssembly = InOwningAssembly;
	ManagedTypeHandle = InTypeHandle;
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

TSharedPtr<FGCHandle> FCSharpClassInfo::GetManagedTypeHandle()
{
#if WITH_EDITOR
	if (!ManagedTypeHandle.IsValid() || ManagedTypeHandle->IsNull())
	{
		// Lazy load the type handle in editor. Gets null during hot reload.
		if (FCSGeneratedClassBuilder::IsManagedType(Field))
		{
			ManagedTypeHandle = OwningAssembly->TryFindTypeHandle(TypeMetaData->FieldName);
		}
		else
		{
			ManagedTypeHandle = OwningAssembly->TryFindTypeHandle(Field);
		}
	}
#endif

	ensureMsgf(ManagedTypeHandle.IsValid(), TEXT("Failed to find managed type handle for %s"), *TypeMetaData->FieldName.GetName());
	return ManagedTypeHandle;
}

