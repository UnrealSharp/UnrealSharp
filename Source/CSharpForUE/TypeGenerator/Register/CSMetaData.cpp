#include "CSMetaData.h"
#include "Dom/JsonObject.h"
#include "UObject/UnrealType.h"
#include "CSharpForUE/TypeGenerator/Factories/CSMetaDataFactory.h"

void FMetaDataHelper::ApplyMetaData(const TMap<FString, FString>& MetaDataMap, UField* Field)
{
#if WITH_EDITOR
	for (const auto& MetaData : MetaDataMap)
	{
		Field->SetMetaData(*MetaData.Key, *MetaData.Value);
	}
#endif
}

void FMetaDataHelper::ApplyMetaData(const TMap<FString, FString>& MetaDataMap, FField* Field)
{
#if WITH_EDITOR
	for (const auto& MetaData : MetaDataMap)
	{
		Field->SetMetaData(*MetaData.Key, *MetaData.Value);
	}
#endif
}

void FUnrealType::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	if (!JsonObject->Values.IsEmpty())
	{
		ArrayDim = JsonObject->GetIntegerField(TEXT("ArrayDim"));
		PropertyType = static_cast<ECSPropertyType>(JsonObject->GetIntegerField(TEXT("PropertyType")));
	}
}

//START ----------------------CSharpMetaDataUtils----------------------------------------

template<typename FlagType>
FlagType CSharpMetaDataUtils::GetFlags(const TSharedPtr<FJsonObject>& PropertyInfo, const FString& StringField)
{
	uint64 FunctionFlagsInt;
	FString FoundStringField;
	PropertyInfo->TryGetStringField(StringField, FoundStringField);

	if (FoundStringField.IsEmpty())
	{
		return static_cast<FlagType>(0);
	}
	
	TTypeFromString<uint64>::FromString(FunctionFlagsInt, *FoundStringField);
	return static_cast<FlagType>(FunctionFlagsInt);
}

void CSharpMetaDataUtils::SerializeFunctions(const TArray<TSharedPtr<FJsonValue>>& FunctionsInfo, TArray<FFunctionMetaData>& FunctionMetaData)
{
	FunctionMetaData.Reserve(FunctionsInfo.Num());
	
	for (const TSharedPtr<FJsonValue>& FunctionInfo : FunctionsInfo)
	{
		FFunctionMetaData NewFunctionMetaData;
		NewFunctionMetaData.SerializeFromJson(FunctionInfo->AsObject());
		FunctionMetaData.Emplace(MoveTemp(NewFunctionMetaData));
	}
}

void CSharpMetaDataUtils::SerializeProperties(const TArray<TSharedPtr<FJsonValue>>& PropertiesInfo, TArray<FPropertyMetaData>& PropertiesMetaData, EPropertyFlags DefaultFlags)
{
	PropertiesMetaData.Reserve(PropertiesInfo.Num());
	
	for (const TSharedPtr<FJsonValue>& Property : PropertiesInfo)
	{
		FPropertyMetaData NewPropertyMetaData;
		SerializeProperty(Property->AsObject(), NewPropertyMetaData, DefaultFlags);
		PropertiesMetaData.Emplace(MoveTemp(NewPropertyMetaData));
	}
}

void CSharpMetaDataUtils::SerializeProperty(const TSharedPtr<FJsonObject>& PropertyMetaData, FPropertyMetaData& PropertiesMetaData, EPropertyFlags DefaultFlags)
{
	PropertiesMetaData.Type = CSMetaDataFactory::Create(PropertyMetaData);
	PropertiesMetaData.SerializeFromJson(PropertyMetaData);
}

//END ----------------------CSharpMetaDataUtils----------------------------------------







//START ----------------------FTypeMetaData----------------------------------------

void FTypeReferenceMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	Name = *JsonObject->GetStringField(TEXT("Name"));

	FString NamespaceStr;
	if (JsonObject->TryGetStringField(TEXT("Namespace"), NamespaceStr))
	{
		Namespace = *NamespaceStr;
	}

	FString AssemblyNameStr;
	if (JsonObject->TryGetStringField(TEXT("AssemblyName"), AssemblyNameStr))
	{
		AssemblyName = *AssemblyNameStr;
	}
	
	FMetaDataHelper::SerializeFromJson(JsonObject, MetaData);
}

void FMemberMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	Name = *JsonObject->GetStringField(TEXT("Name"));
	FMetaDataHelper::SerializeFromJson(JsonObject, MetaData);
}

void FMetaDataHelper::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject, TMap<FString, FString>& MetaDataMap)
{
	const TSharedPtr<FJsonObject>* MetaDataObjectPtr;
	if (JsonObject->TryGetObjectField(TEXT("MetaData"), MetaDataObjectPtr))
	{
		TSharedPtr<FJsonObject> MetaDataObject = *MetaDataObjectPtr;
		for (const auto& Pair : MetaDataObject->Values)
		{
			FString Key = Pair.Key;
			FString Value;
			
			MetaDataObject->TryGetStringField(Key, Value);

			MetaDataMap.Add(Key, Value);
		}
	}
}

//END ----------------------FTypeMetaData----------------------------------------









//START ----------------------FClassMetaData----------------------------------------

void FClassMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FTypeReferenceMetaData::SerializeFromJson(JsonObject);

	ClassFlags = CSharpMetaDataUtils::GetFlags<EClassFlags>(JsonObject,"ClassFlags");
	
	ParentClass.SerializeFromJson(JsonObject->GetObjectField(TEXT("ParentClass")));

	FString ClassConfigNameStr;
	if (JsonObject->TryGetStringField(TEXT("ConfigCategory"), ClassConfigNameStr))
	{
		ClassConfigName = *ClassConfigNameStr;
	}

	TArray<FString> InterfacesStr;
	if (JsonObject->TryGetStringArrayField(TEXT("Interfaces"), InterfacesStr))
	{
		for (const FString& Interface : InterfacesStr)
		{
			Interfaces.Add(*Interface);
		}
	}

	const TArray<TSharedPtr<FJsonValue>>* FoundFunctions;
	if (JsonObject->TryGetArrayField(TEXT("Functions"), FoundFunctions))
	{
		CSharpMetaDataUtils::SerializeFunctions(*FoundFunctions, Functions);
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
		CSharpMetaDataUtils::SerializeProperties(*FoundProperties, Properties);
	}
}

void FClassPropertyMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FUnrealType::SerializeFromJson(JsonObject);
	TypeRef.SerializeFromJson(JsonObject->GetObjectField(TEXT("InnerType")));
}

void FStructPropertyMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FUnrealType::SerializeFromJson(JsonObject);
	TypeRef.SerializeFromJson(JsonObject->GetObjectField(TEXT("InnerType")));
}

void FObjectMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FUnrealType::SerializeFromJson(JsonObject);
	InnerType.SerializeFromJson(JsonObject->GetObjectField(TEXT("InnerType")));
}

//END ----------------------FClassMetaData----------------------------------------








//START ----------------------FStructMetaData----------------------------------------

void FStructMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FTypeReferenceMetaData::SerializeFromJson(JsonObject);
	const TArray<TSharedPtr<FJsonValue>>* FoundProperties;
	if (JsonObject->TryGetArrayField(TEXT("Fields"), FoundProperties))
	{
		CSharpMetaDataUtils::SerializeProperties(*FoundProperties, Properties);
	}
}

//END ----------------------FStructMetaData----------------------------------------







//START ----------------------FEnumMetaData----------------------------------------

void FEnumPropertyMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FUnrealType::SerializeFromJson(JsonObject);
	InnerProperty.SerializeFromJson(JsonObject->GetObjectField(TEXT("InnerProperty")));
}

void FEnumMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FTypeReferenceMetaData::SerializeFromJson(JsonObject);

	const TArray<TSharedPtr<FJsonValue>>* EnumValues;
	if (JsonObject->TryGetArrayField(TEXT("Items"), EnumValues))
	{
		for (const TSharedPtr<FJsonValue>& Item : *EnumValues)
		{
			Items.Add(*Item->AsString());
		}
	}
}

//END ----------------------FEnumMetaData----------------------------------------






//START ----------------------FFunctionMetaData----------------------------------------

void FFunctionMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FMemberMetaData::SerializeFromJson(JsonObject);

	const TArray<TSharedPtr<FJsonValue>>* ParametersArrayField;
	if (JsonObject->TryGetArrayField(TEXT("Parameters"), ParametersArrayField))
	{
		CSharpMetaDataUtils::SerializeProperties(*ParametersArrayField, Parameters);
	}

	const TSharedPtr<FJsonObject>* ReturnValueObject;
	if (JsonObject->TryGetObjectField(TEXT("ReturnValue"), ReturnValueObject))
	{
		CSharpMetaDataUtils::SerializeProperty(*ReturnValueObject, ReturnValue);
		
		//Since the return value has no name in the C# reflection. Just assign "ReturnValue" to it.
		ReturnValue.Name = "ReturnValue";
	}

	JsonObject->TryGetBoolField(TEXT("IsVirtual"), IsVirtual);
	FunctionFlags = CSharpMetaDataUtils::GetFlags<EFunctionFlags>(JsonObject,"FunctionFlags");
}

//END ----------------------FFunctionMetaData----------------------------------------





//START ----------------------FPropertyMetaData----------------------------------------

void FPropertyMetaData:: SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FMemberMetaData::SerializeFromJson(JsonObject);
	
	PropertyFlags = CSharpMetaDataUtils::GetFlags<EPropertyFlags>(JsonObject,"PropertyFlags");
	LifetimeCondition = CSharpMetaDataUtils::GetFlags<ELifetimeCondition>(JsonObject,"LifetimeCondition");
	
	JsonObject->TryGetStringField(TEXT("BlueprintGetter"), BlueprintGetter);
	JsonObject->TryGetStringField(TEXT("BlueprintSetter"), BlueprintSetter);

	FString RepNotifyFunctionNameStr;
	if (JsonObject->TryGetStringField(TEXT("RepNotifyFunctionName"), RepNotifyFunctionNameStr))
	{
		RepNotifyFunctionName = *RepNotifyFunctionNameStr;
	}
}

//END ----------------------FPropertyMetaData----------------------------------------





//START ----------------------FArrayMetaData----------------------------------------
void FArrayPropertyMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FUnrealType::SerializeFromJson(JsonObject);
	CSharpMetaDataUtils::SerializeProperty(JsonObject->GetObjectField(TEXT("InnerProperty")), InnerProperty);
}
//END ----------------------FArrayMetaData----------------------------------------









//START ----------------------FDefaultComponentMetaData----------------------------------------
void FDefaultComponentMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FObjectMetaData::SerializeFromJson(JsonObject);
	JsonObject->TryGetBoolField(TEXT("IsRootComponent"), IsRootComponent);

	FString AttachmentComponentStr;
	if (JsonObject->TryGetStringField(TEXT("AttachmentComponent"), AttachmentComponentStr))
	{
		AttachmentComponent = *AttachmentComponentStr;
	}

	FString AttachmentSocketStr;
	if (JsonObject->TryGetStringField(TEXT("AttachmentSocket"), AttachmentSocketStr))
	{
		AttachmentSocket = *AttachmentSocketStr;
	}
}
//END ----------------------FDefaultComponentMetaData----------------------------------------



void FDelegateMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FUnrealType::SerializeFromJson(JsonObject);
	SignatureFunction.SerializeFromJson(JsonObject->GetObjectField(TEXT("Signature")));
	SignatureFunction.Name = "";
}

void FInterfaceMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FTypeReferenceMetaData::SerializeFromJson(JsonObject);
	CSharpMetaDataUtils::SerializeFunctions(JsonObject->GetArrayField(TEXT("Functions")), Functions);
}
