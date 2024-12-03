#include "CSPropertyFactory.h"
#include "PropertyGenerators/CSPropertyGenerator.h"
#include "UObject/UnrealType.h"
#include "UObject/Class.h"
#include "UnrealSharpCore/TypeGenerator/Register/CSMetaDataUtils.h"
#include "TypeGenerator/Register/MetaData/CSDelegateMetaData.h"
#include "UnrealSharpUtilities/UnrealSharpUtils.h"

TArray<TWeakObjectPtr<UCSPropertyGenerator>> FCSPropertyFactory::PropertyGenerators;

void FCSPropertyFactory::Initialize()
{
	TArray<UCSPropertyGenerator*> FoundPropertyGenerators;
	FUnrealSharpUtils::GetAllCDOsOfClass<UCSPropertyGenerator>(FoundPropertyGenerators);

	PropertyGenerators.Reserve(FoundPropertyGenerators.Num());
	for (UCSPropertyGenerator* PropertyGenerator : FoundPropertyGenerators)
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
	
	if (NewProperty->HasAnyPropertyFlags(CPF_Net))
	{
		UBlueprintGeneratedClass* OwnerClass = CastChecked<UBlueprintGeneratedClass>(Outer);
		++OwnerClass->NumReplicatedProperties;
			
		if (!PropertyMetaData.RepNotifyFunctionName.IsNone())
		{
			NewProperty->RepNotifyFunc = PropertyMetaData.RepNotifyFunctionName;
		}
	}
		
	FCSMetaDataUtils::ApplyMetaData(PropertyMetaData.MetaData, NewProperty);
		
	return NewProperty;
}

FProperty* FCSPropertyFactory::CreateAndAssignProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FProperty* Property = CreateProperty(Outer, PropertyMetaData);
	Outer->AddCppProperty(Property);
	PropertyMetaData.Type->OnPropertyCreated(Property);
	return Property;
}

void FCSPropertyFactory::CreateAndAssignProperties(UField* Outer, const TArray<FCSPropertyMetaData>& PropertyMetaData)
{
	for (const FCSPropertyMetaData& Property : PropertyMetaData)
	{
		CreateAndAssignProperty(Outer, Property);
	}
}

UCSPropertyGenerator* FCSPropertyFactory::FindPropertyGenerator(ECSPropertyType PropertyType)
{
	for (TWeakObjectPtr<UCSPropertyGenerator>& PropertyGenerator : PropertyGenerators)
	{
		UCSPropertyGenerator* PropertyGeneratorPtr = PropertyGenerator.Get();
		if (!PropertyGeneratorPtr->SupportsPropertyType(PropertyType))
		{
			continue;
		}

		return PropertyGeneratorPtr;
	}
	
	return nullptr;
}


