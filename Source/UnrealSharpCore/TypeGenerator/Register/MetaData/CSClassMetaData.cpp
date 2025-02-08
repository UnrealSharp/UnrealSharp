#include "CSClassMetaData.h"

#include "TypeGenerator/Register/CSMetaDataUtils.h"

void FCSClassMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FCSTypeReferenceMetaData::SerializeFromJson(JsonObject);

	ClassFlags = FCSMetaDataUtils::GetFlags<EClassFlags>(JsonObject,"ClassFlags");
	
	ParentClass.SerializeFromJson(JsonObject->GetObjectField(TEXT("ParentClass")));

	FString ClassConfigNameStr;
	if (JsonObject->TryGetStringField(TEXT("ConfigCategory"), ClassConfigNameStr))
	{
		ClassConfigName = *ClassConfigNameStr;
	}

	const TArray<TSharedPtr<FJsonValue>>* FoundInterfaces;
	if (JsonObject->TryGetArrayField(TEXT("Interfaces"), FoundInterfaces))
	{
		for (const TSharedPtr<FJsonValue>& Interface : *FoundInterfaces)
		{
			FCSTypeReferenceMetaData& InterfaceMetaData = Interfaces.AddDefaulted_GetRef();
			InterfaceMetaData.SerializeFromJson(Interface->AsObject());
		}
	}

	const TArray<TSharedPtr<FJsonValue>>* FoundFunctions;
	if (JsonObject->TryGetArrayField(TEXT("Functions"), FoundFunctions))
	{
		FCSMetaDataUtils::SerializeFunctions(*FoundFunctions, Functions);
	}
	
	const TArray<TSharedPtr<FJsonValue>>* FoundVirtualFunctions;
	if (JsonObject->TryGetArrayField(TEXT("VirtualFunctions"), FoundVirtualFunctions))
	{
		for (const TSharedPtr<FJsonValue>& VirtualFunction : *FoundVirtualFunctions)
		{
			VirtualFunctions.Add(*VirtualFunction->AsObject()->GetStringField(TEXT("Name")));
		}
	}

	const TArray<TSharedPtr<FJsonValue>>* FoundProperties;
	if (JsonObject->TryGetArrayField(TEXT("Properties"), FoundProperties))
	{
		FCSMetaDataUtils::SerializeProperties(*FoundProperties, Properties);
	}
}
