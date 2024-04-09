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
		ArrayDim = JsonObject->GetIntegerField("ArrayDim");
		PropertyType = static_cast<ECSPropertyType>(JsonObject->GetIntegerField("PropertyType"));
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
	Name = JsonObject->GetStringField("Name");
	Namespace = JsonObject->GetStringField("Namespace");
	AssemblyName = JsonObject->GetStringField("AssemblyName");

	FMetaDataHelper::SerializeFromJson(JsonObject, MetaData);
}

void FMemberMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	Name = *JsonObject->GetStringField("Name");
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
	
	ParentClass.SerializeFromJson(JsonObject->GetObjectField("ParentClass"));

	JsonObject->TryGetStringField("ConfigCategory", ClassConfigName);
	JsonObject->TryGetStringArrayField("Interfaces", Interfaces);

	const TArray<TSharedPtr<FJsonValue>>* FoundFunctions;
	if (JsonObject->TryGetArrayField("Functions", FoundFunctions))
	{
		CSharpMetaDataUtils::SerializeFunctions(*FoundFunctions, Functions);
	}
	
	const TArray<TSharedPtr<FJsonValue>>* FoundVirtualFunctions;
	if (JsonObject->TryGetArrayField("VirtualFunctions", FoundVirtualFunctions))
	{
		for (const TSharedPtr<FJsonValue>& VirtualFunction : *FoundVirtualFunctions)
		{
			VirtualFunctions.Add(VirtualFunction->AsObject()->GetStringField("Name"));
		}
	}

	const TArray<TSharedPtr<FJsonValue>>* FoundProperties;
	if (JsonObject->TryGetArrayField("Properties", FoundProperties))
	{
		CSharpMetaDataUtils::SerializeProperties(*FoundProperties, Properties);
	}
}

void FClassPropertyMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FUnrealType::SerializeFromJson(JsonObject);
	TypeRef.SerializeFromJson(JsonObject->GetObjectField("InnerType"));
}

void FStructPropertyMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FUnrealType::SerializeFromJson(JsonObject);
	TypeRef.SerializeFromJson(JsonObject->GetObjectField("InnerType"));
}

void FObjectMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FUnrealType::SerializeFromJson(JsonObject);
	InnerType.SerializeFromJson(JsonObject->GetObjectField("InnerType"));
}

//END ----------------------FClassMetaData----------------------------------------








//START ----------------------FStructMetaData----------------------------------------

void FStructMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FTypeReferenceMetaData::SerializeFromJson(JsonObject);
	CSharpMetaDataUtils::SerializeProperties(JsonObject->GetArrayField("Fields"), Properties);
	bIsDataTableStruct = JsonObject->GetBoolField("IsDataTableStruct");
}

//END ----------------------FStructMetaData----------------------------------------







//START ----------------------FEnumMetaData----------------------------------------

void FEnumPropertyMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FUnrealType::SerializeFromJson(JsonObject);
	InnerProperty.SerializeFromJson(JsonObject->GetObjectField("InnerProperty"));
}

void FEnumMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FTypeReferenceMetaData::SerializeFromJson(JsonObject);

	const TArray<TSharedPtr<FJsonValue>>* OutArray;
	JsonObject->TryGetArrayField("Items", OutArray);
	
	for (const TSharedPtr<FJsonValue>& Item : *OutArray)
	{
		Items.Add(Item->AsString());
	}
}

//END ----------------------FEnumMetaData----------------------------------------






//START ----------------------FFunctionMetaData----------------------------------------

void FFunctionMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FMemberMetaData::SerializeFromJson(JsonObject);

	const TArray<TSharedPtr<FJsonValue>>* ParametersArrayField;
	if (JsonObject->TryGetArrayField("Parameters", ParametersArrayField))
	{
		CSharpMetaDataUtils::SerializeProperties(*ParametersArrayField, Parameters);
	}

	const TSharedPtr<FJsonObject>* ReturnValueObject;
	if (JsonObject->TryGetObjectField("ReturnValue", ReturnValueObject))
	{
		CSharpMetaDataUtils::SerializeProperty(*ReturnValueObject, ReturnValue);
		
		//Since the return value has no name in the C# reflection. Just assign "ReturnValue" to it.
		ReturnValue.Name = "ReturnValue";
	}

	JsonObject->TryGetBoolField("IsVirtual", IsVirtual);
	FunctionFlags = CSharpMetaDataUtils::GetFlags<EFunctionFlags>(JsonObject,"FunctionFlags");
}

//END ----------------------FFunctionMetaData----------------------------------------





//START ----------------------FPropertyMetaData----------------------------------------

void FPropertyMetaData:: SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FMemberMetaData::SerializeFromJson(JsonObject);
	
	PropertyFlags = CSharpMetaDataUtils::GetFlags<EPropertyFlags>(JsonObject,"PropertyFlags");
	LifetimeCondition = CSharpMetaDataUtils::GetFlags<ELifetimeCondition>(JsonObject,"LifetimeCondition");

	JsonObject->TryGetStringField("BlueprintGetter", BlueprintGetter);
	JsonObject->TryGetStringField("BlueprintSetter", BlueprintSetter);
	JsonObject->TryGetStringField("RepNotifyFunctionName", RepNotifyFunctionName);
}

//END ----------------------FPropertyMetaData----------------------------------------





//START ----------------------FArrayMetaData----------------------------------------
void FArrayPropertyMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FUnrealType::SerializeFromJson(JsonObject);
	CSharpMetaDataUtils::SerializeProperty(JsonObject->GetObjectField("InnerProperty"), InnerProperty);
}
//END ----------------------FArrayMetaData----------------------------------------









//START ----------------------FDefaultComponentMetaData----------------------------------------
void FDefaultComponentMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FObjectMetaData::SerializeFromJson(JsonObject);
	JsonObject->TryGetBoolField("IsRootComponent", IsRootComponent);
	JsonObject->TryGetStringField("AttachmentComponent", AttachmentComponent);
	JsonObject->TryGetStringField("AttachmentSocket", AttachmentSocket);
}

void FDefaultComponentMetaData::OnPropertyCreated(FProperty* Property)
{
	UClass* Class = Property->Owner.GetOwnerClass();
	Class->ClassFlags |= CLASS_HasInstancedReference;
}

//END ----------------------FDefaultComponentMetaData----------------------------------------



void FDelegateMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FUnrealType::SerializeFromJson(JsonObject);
	SignatureFunction.SerializeFromJson(JsonObject->GetObjectField("Signature"));
	SignatureFunction.Name = "";
}

void FInterfaceMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FTypeReferenceMetaData::SerializeFromJson(JsonObject);
	CSharpMetaDataUtils::SerializeFunctions(JsonObject->GetArrayField("Functions"), Functions);
}
