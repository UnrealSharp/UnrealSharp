#include "Factories/CSPropertyFactory.h"
#include "INotifyFieldValueChanged.h"
#include "Factories/PropertyGenerators/CSPropertyGenerator.h"
#include "Properties/CSPropertyGeneratorManager.h"
#include "UObject/UnrealType.h"
#include "UObject/Class.h"
#include "Utilities/CSMetaDataUtils.h"
#include "UnrealSharpUtils.h"

TArray<TObjectPtr<UCSPropertyGenerator>> FCSPropertyFactory::PropertyGenerators;
TMap<uint32, UCSPropertyGenerator*> FCSPropertyFactory::PropertyGeneratorMap;

void FCSPropertyFactory::Initialize()
{
	if (PropertyGenerators.Num() > 0)
	{
		return;
	}

	FCSPropertyGeneratorManager::Initialize();
	
	TArray<UCSPropertyGenerator*> FoundPropertyGeneratorClasses;
	FCSUnrealSharpUtils::GetAllCDOsOfClass<UCSPropertyGenerator>(FoundPropertyGeneratorClasses);
	
	PropertyGenerators.Reserve(FoundPropertyGeneratorClasses.Num());
	
	for (UCSPropertyGenerator* PropertyGenerator : FoundPropertyGeneratorClasses)
	{
		PropertyGenerators.Add(PropertyGenerator);
	}

	int64 MaxValue = StaticEnum<ECSPropertyType>()->GetMaxEnumValue();
	PropertyGeneratorMap.Reserve(MaxValue);
	
	for (int64 i = 0; i < MaxValue; ++i)
	{
		ECSPropertyType PropertyType = static_cast<ECSPropertyType>(i);

		UCSPropertyGenerator* FoundGenerator = nullptr;
		for (UCSPropertyGenerator* PropertyGenerator : PropertyGenerators)
		{
			if (PropertyGenerator->SupportsPropertyType(PropertyType))
			{
				FoundGenerator = PropertyGenerator;
				break;
			}
		}
		
		if (!IsValid(FoundGenerator))
		{
			continue;
		}

		uint32 Hash = static_cast<uint32>(PropertyType);
		PropertyGeneratorMap.AddByHash(Hash, Hash, FoundGenerator);
	}
}

FProperty* FCSPropertyFactory::CreateProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(FCSPropertyFactory::CreateProperty);
	
	UCSPropertyGenerator* PropertyGenerator = GetPropertyGenerator(PropertyReflectionData.InnerType->PropertyType);
	FProperty* NewProperty = PropertyGenerator->CreateProperty(Outer, PropertyReflectionData);

	NewProperty->SetPropertyFlags(PropertyReflectionData.PropertyFlags);
	NewProperty->SetBlueprintReplicationCondition(PropertyReflectionData.LifetimeCondition);

	FCSMetaDataUtils::ApplyMetaData(PropertyReflectionData.MetaData, NewProperty);
	
	if (UBlueprintGeneratedClass* OwningClass = Cast<UBlueprintGeneratedClass>(Outer))
	{
		if (NewProperty->HasAnyPropertyFlags(CPF_Net))
		{
			++OwningClass->NumReplicatedProperties;
			
			if (!PropertyReflectionData.RepNotifyFunctionName.IsNone())
			{
				NewProperty->RepNotifyFunc = PropertyReflectionData.RepNotifyFunctionName;
			}
		}

		TryAddPropertyAsFieldNotify(PropertyReflectionData, OwningClass);
	}

	NewProperty->SetFlags(RF_LoadCompleted);
	return NewProperty;
}

void FCSPropertyFactory::CreateAndAssignProperties(UField* Outer, const TArray<FCSPropertyReflectionData>& PropertyReflectionData, const TFunction<void(FProperty*)>& OnPropertyCreated)
{
	for (int32 i = PropertyReflectionData.Num() - 1; i >= 0; --i)
	{
		const FCSPropertyReflectionData& Property = PropertyReflectionData[i];
		FProperty* NewProperty = CreateAndAssignProperty(Outer, Property);

		if (OnPropertyCreated)
		{
			OnPropertyCreated(NewProperty);
		}
	}
}

void FCSPropertyFactory::TryAddPropertyAsFieldNotify(const FCSPropertyReflectionData& PropertyReflectionData, UBlueprintGeneratedClass* Class)
{
	bool bImplementsInterface = Class->ImplementsInterface(UNotifyFieldValueChanged::StaticClass());
	bool bHasFieldNotifyMetaData = PropertyReflectionData.HasMetaData(TEXT("FieldNotify"));
	
	if (!bImplementsInterface || !bHasFieldNotifyMetaData)
	{
		return;
	}
	
	Class->FieldNotifies.Add(FFieldNotificationId(PropertyReflectionData.GetName()));
}



