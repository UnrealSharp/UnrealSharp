#include "CSClassInfo.h"
#include "UnrealSharpCore/TypeGenerator/Register/CSTypeRegistry.h"
#include "UnrealSharpUtilities/UnrealSharpUtils.h"

FCSharpClassInfo::FCSharpClassInfo(const TSharedPtr<FJsonValue>& MetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly) : TCSharpTypeInfo(MetaData, InOwningAssembly)
{
	FCSTypeRegistry::Get().GetOnClassModifiedEvent().AddRaw(this, &FCSharpClassInfo::OnNewClassOrModified);
}

FCSharpClassInfo::FCSharpClassInfo(UClass* InField)
{
	Field = InField;
	bInitFromClass = true;

#if WITH_EDITOR
	UCSManager::Get().OnAssembliesLoadedEvent().AddRaw(this, &FCSharpClassInfo::OnAssembliesLoaded);
#endif
}

UClass* FCSharpClassInfo::InitializeBuilder()
{
	if (Field && Field->HasAllClassFlags(CLASS_Native))
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

void FCSharpClassInfo::TryUpdateTypeHandle()
{
	if (bDirtyHandle && bInitFromClass)
	{
		bDirtyHandle = false;
	}
}

void FCSharpClassInfo::OnAssembliesLoaded()
{
	bDirtyHandle = true;
}

void FCSharpClassInfo::OnNewClassOrModified(UClass* Class)
{
	if (Class == Field)
	{
		bDirtyHandle = true;
		bInitFromClass = true;
		TryUpdateTypeHandle();
		bInitFromClass = false;
	}
}

uint8* FCSharpClassInfo::GetHandle(UClass* Class)
{
	if (FCSGeneratedClassBuilder::IsManagedType(Class))
	{
		return UCSManager::Get().GetTypeHandle(*TypeMetaData);
	}
	else
	{
		return UCSManager::Get().GetTypeHandle(nullptr, FUnrealSharpUtils::GetNamespace(Class), Class->GetName());
	}
}

