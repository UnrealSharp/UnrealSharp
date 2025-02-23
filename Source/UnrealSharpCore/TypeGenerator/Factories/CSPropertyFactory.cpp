#include "CSPropertyFactory.h"
#include "PropertyGenerators/CSPropertyGenerator.h"
#include "UObject/UnrealType.h"
#include "UObject/Class.h"
#include "UnrealSharpCore/TypeGenerator/Register/CSMetaDataUtils.h"
#include "TypeGenerator/Register/MetaData/CSDelegateMetaData.h"
#include "UnrealSharpUtilities/UnrealSharpUtils.h"

TArray<TObjectPtr<UCSPropertyGenerator>> FCSPropertyFactory::PropertyGenerators;

void FCSPropertyFactory::Initialize()
{
	if (PropertyGenerators.Num() > 0)
	{
		return;
	}
	
	TArray<UCSPropertyGenerator*> FoundPropertyGeneratorClasses;
	FUnrealSharpUtils::GetAllCDOsOfClass<UCSPropertyGenerator>(FoundPropertyGeneratorClasses);
	
	PropertyGenerators.Reserve(FoundPropertyGeneratorClasses.Num());
	
	for (UCSPropertyGenerator* PropertyGenerator : FoundPropertyGeneratorClasses)
	{
		PropertyGenerators.Add(PropertyGenerator);
	}
}

FProperty* FCSPropertyFactory::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	UCSPropertyGenerator* PropertyGenerator = FindPropertyGenerator(PropertyMetaData.Type->PropertyType);
	FProperty* NewProperty = PropertyGenerator->CreateProperty(Outer, PropertyMetaData);

	NewProperty->SetPropertyFlags(PropertyMetaData.PropertyFlags);
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
	
	if (NewProperty->HasAnyPropertyFlags(CPF_Net) && Outer->IsA<UBlueprintGeneratedClass>())
	{
		UBlueprintGeneratedClass* OwnerClass = static_cast<UBlueprintGeneratedClass*>(Outer);
		++OwnerClass->NumReplicatedProperties;
			
		if (!PropertyMetaData.RepNotifyFunctionName.IsNone())
		{
			NewProperty->RepNotifyFunc = PropertyMetaData.RepNotifyFunctionName;
		}
	}
		
	FCSMetaDataUtils::ApplyMetaData(PropertyMetaData.MetaData, NewProperty);

	NewProperty->SetFlags(RF_LoadCompleted);
	return NewProperty;
}

FProperty* FCSPropertyFactory::CreateAndAssignProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FProperty* Property = CreateProperty(Outer, PropertyMetaData);
	Outer->AddCppProperty(Property);
	return Property;
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

TSharedPtr<FCSUnrealType> FCSPropertyFactory::CreateTypeMetaData(const TSharedPtr<FJsonObject>& PropertyMetaData)
{
	const TSharedPtr<FJsonObject>& PropertyTypeObject = PropertyMetaData->GetObjectField(TEXT("PropertyDataType"));
	ECSPropertyType PropertyType = static_cast<ECSPropertyType>(PropertyTypeObject->GetIntegerField(TEXT("PropertyType")));
	
	UCSPropertyGenerator* PropertyGenerator = FindPropertyGenerator(PropertyType);
	TSharedPtr<FCSUnrealType> PropertiesMetaData = PropertyGenerator->CreateTypeMetaData(PropertyType);
	
	PropertiesMetaData->SerializeFromJson(PropertyTypeObject);
	return PropertiesMetaData;
}

UCSPropertyGenerator* FCSPropertyFactory::FindPropertyGenerator(ECSPropertyType PropertyType)
{
	for (TObjectPtr<UCSPropertyGenerator>& PropertyGenerator : PropertyGenerators)
	{
		if (!PropertyGenerator->SupportsPropertyType(PropertyType))
		{
			continue;
		}

		return PropertyGenerator;
	}
	
	return nullptr;
}


