#include "CSPropertyFactory.h"

#include "INotifyFieldValueChanged.h"
#include "Properties/CSPropertyGeneratorManager.h"
#include "PropertyGenerators/CSPropertyGenerator.h"
#include "UObject/UnrealType.h"
#include "UObject/Class.h"
#include "Utilities/CSMetaDataUtils.h"
#include "UnrealSharpUtilities/UnrealSharpUtils.h"

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

FProperty* FCSPropertyFactory::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(FCSPropertyFactory::CreateProperty);
	
	UCSPropertyGenerator* PropertyGenerator = GetPropertyGenerator(PropertyMetaData.Type->PropertyType);
	FProperty* NewProperty = PropertyGenerator->CreateProperty(Outer, PropertyMetaData);

	NewProperty->SetPropertyFlags(PropertyMetaData.Flags);
	NewProperty->SetBlueprintReplicationCondition(PropertyMetaData.LifetimeCondition);

#if WITH_EDITOR
	if (!PropertyMetaData.BlueprintSetter.IsEmpty())
	{
		NewProperty->SetMetaData("BlueprintSetter", *PropertyMetaData.BlueprintSetter);

		if (UFunction* BlueprintSetterFunction = CastChecked<UClass>(Outer)->FindFunctionByName(*PropertyMetaData.BlueprintSetter))
		{
			BlueprintSetterFunction->SetMetaData("BlueprintInternalUseOnly", TEXT("true"));
		}
	}

	if (!PropertyMetaData.BlueprintGetter.IsEmpty())
	{
		NewProperty->SetMetaData("BlueprintGetter", *PropertyMetaData.BlueprintGetter);
			
		if (UFunction* BlueprintGetterFunction = CastChecked<UClass>(Outer)->FindFunctionByName(*PropertyMetaData.BlueprintGetter))
		{
			BlueprintGetterFunction->SetMetaData("BlueprintInternalUseOnly", TEXT("true"));
		}
	}
#endif

	FCSMetaDataUtils::ApplyMetaData(PropertyMetaData.MetaData, NewProperty);
	
	if (UBlueprintGeneratedClass* OwningClass = Cast<UBlueprintGeneratedClass>(Outer))
	{
		if (NewProperty->HasAnyPropertyFlags(CPF_Net))
		{
			++OwningClass->NumReplicatedProperties;
			
			if (!PropertyMetaData.RepNotifyFunctionName.IsNone())
			{
				NewProperty->RepNotifyFunc = PropertyMetaData.RepNotifyFunctionName;
			}
		}

		TryAddPropertyAsFieldNotify(PropertyMetaData, OwningClass);
	}

	NewProperty->SetFlags(RF_LoadCompleted);
	return NewProperty;
}

void FCSPropertyFactory::CreateAndAssignProperties(UField* Outer, const TArray<FCSPropertyMetaData>& PropertyMetaData, const TFunction<void(FProperty*)>& OnPropertyCreated)
{
	for (int32 i = PropertyMetaData.Num() - 1; i >= 0; --i)
	{
		const FCSPropertyMetaData& Property = PropertyMetaData[i];
		FProperty* NewProperty = CreateAndAssignProperty(Outer, Property);

		if (OnPropertyCreated)
		{
			OnPropertyCreated(NewProperty);
		}
	}
}

void FCSPropertyFactory::TryAddPropertyAsFieldNotify(const FCSPropertyMetaData& PropertyMetaData, UBlueprintGeneratedClass* Class)
{
	bool bImplementsInterface = Class->ImplementsInterface(UNotifyFieldValueChanged::StaticClass());
	bool bHasFieldNotifyMetaData = PropertyMetaData.HasMetaData(TEXT("FieldNotify"));
	
	if (!bImplementsInterface || !bHasFieldNotifyMetaData)
	{
		return;
	}
	
	Class->FieldNotifies.Add(FFieldNotificationId(PropertyMetaData.GetName()));
}



