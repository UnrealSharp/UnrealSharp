#include "CSClassInfo.h"
#include "UnrealSharpCore/TypeGenerator/Register/CSTypeRegistry.h"
#include "UnrealSharpUtilities/UnrealSharpUtils.h"

FCSharpClassInfo::FCSharpClassInfo(const TSharedPtr<FJsonValue>& MetaData): TCSharpTypeInfo(MetaData)
{
	TypeHandle = UCSManager::Get().GetTypeHandle(*TypeMetaData);
}

FCSharpClassInfo::FCSharpClassInfo(UClass* InField)
{
	TypeHandle = GetHandle(InField);
	Field = InField;
	bInitFromClass = true;

#if WITH_EDITOR
	UCSManager::Get().OnAssembliesLoadedEvent().AddRaw(this, &FCSharpClassInfo::OnAssembliesLoaded);
#endif
}

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

void FCSharpClassInfo::TryUpdateTypeHandle()
{
	if (bDirtyHandle && bInitFromClass)
	{
		TypeHandle = GetHandle(Field);
		bDirtyHandle = false;
	}
}

void FCSharpClassInfo::OnAssembliesLoaded()
{
	bDirtyHandle = true;
}

uint8* FCSharpClassInfo::GetHandle(UClass* Class)
{
	return UCSManager::Get().GetTypeHandle(nullptr, FUnrealSharpUtils::GetNamespace(Class), Class->GetName());
}

