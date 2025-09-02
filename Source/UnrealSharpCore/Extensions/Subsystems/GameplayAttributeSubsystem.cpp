// Fill out your copyright notice in the Description page of Project Settings.


#include "GameplayAttributeSubsystem.h"

void UGameplayAttributeSubsystem::Initialize(FSubsystemCollectionBase& Collection)
{
	Super::Initialize(Collection);
	CacheAllGameplayAttributes();
}

UGameplayAttributeSubsystem* UGameplayAttributeSubsystem::Get()
{
	if (GEngine)
	{
		return GEngine->GetEngineSubsystem<UGameplayAttributeSubsystem>();
	}
	return nullptr;
}

FGameplayAttribute UGameplayAttributeSubsystem::FindGameplayAttributeByName(const FString& AttributeSetClassName, const FString& PropertyName)
{
	UGameplayAttributeSubsystem* Subsystem = Get();
	if (!Subsystem)
	{
		return FGameplayAttribute(); // Invalid attribute
	}

	FString Key = FString::Printf(TEXT("%s.%s"), *AttributeSetClassName, *PropertyName);
	if (FGameplayAttribute* FoundAttribute = Subsystem->CachedAttributes.Find(Key))
	{
		return *FoundAttribute;
	}

	return FGameplayAttribute(); // Invalid attribute
}

void UGameplayAttributeSubsystem::GetCachedAttributeNamesForClass(const FString& AttributeSetClassName, TArray<FString>& OutAttributeNames) const
{
	OutAttributeNames.Empty();

	FString ClassPrefix = AttributeSetClassName + TEXT(".");

	for (const auto& Pair : CachedAttributes)
	{
		if (Pair.Key.StartsWith(ClassPrefix))
		{
			// Extract property name after "ClassName."
			FString PropertyName = Pair.Key.Mid(ClassPrefix.Len());
			OutAttributeNames.Add(PropertyName);
		}
	}
}

void UGameplayAttributeSubsystem::CacheAllGameplayAttributes()
{
	CachedAttributes.Empty();

	// Iterate through all AttributeSet classes
	for (TObjectIterator<UClass> It; It; ++It)
	{
		UClass* Class = *It;
		if (!Class || !Class->IsChildOf(UAttributeSet::StaticClass()))
		{
			continue;
		}
		if (Class->HasAnyClassFlags(CLASS_Abstract) || Class->ClassGeneratedBy)
		{
			continue;
		}

		// Get all attribute properties for this class
		TArray<FProperty*> AttributeProperties;
		GetAllAttributeProperties(Class, AttributeProperties);

		// Cache each attribute with "ClassName.PropertyName" as key
		for (FProperty* Property : AttributeProperties)
		{
			if (Property)
			{
				FString Key = FString::Printf(TEXT("%s.%s"), *Class->GetName(), *Property->GetName());
				FGameplayAttribute Attribute(Property);
				CachedAttributes.Add(Key, Attribute);
			}
		}
	}

	UE_LOG(LogTemp, Log, TEXT("GameplayAttributeSubsystem: Cached %d gameplay attributes"), CachedAttributes.Num());
}

void UGameplayAttributeSubsystem::GetAllAttributeProperties(UClass* AttributeSetClass, TArray<FProperty*>& OutProperties)
{
	if (!AttributeSetClass)
	{
		return;
	}

	// Use the existing AttributeSet method to get all attribute properties
	FGameplayAttribute::GetAllAttributeProperties(OutProperties, FString(), true);

	// Filter properties to only include those from the specified class
	OutProperties.RemoveAll([AttributeSetClass](const FProperty* Property)
	{
		return Property->GetOwnerStruct() != AttributeSetClass;
	});
}