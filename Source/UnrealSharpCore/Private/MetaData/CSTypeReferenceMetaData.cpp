#include "MetaData/CSTypeReferenceMetaData.h"
#include "CSManager.h"

bool FCSTypeReferenceMetaData::Serialize(TSharedPtr<FJsonObject> JsonObject)
{
	START_JSON_SERIALIZE
	
	JSON_READ_STRING(AssemblyName, IS_REQUIRED);
	CALL_SERIALIZE(FieldName.Serialize(JsonObject));
	JSON_PARSE_OBJECT_ARRAY(SourceGeneratorDependencies, IS_OPTIONAL);

	JSON_PARSE_OBJECT_ARRAY_CUSTOM(MetaData, [](FCSMetaDataEntry& Dest, const TSharedPtr<FJsonObject>& Object)
	{
		START_JSON_SERIALIZE
		
		FString Key;
		JSON_READ_STRING(Object, Key, IS_REQUIRED);

		FString Value;
		JSON_READ_STRING(Object, Value, IS_OPTIONAL);

		Dest = FCSMetaDataEntry(Key, Value);
		
		END_JSON_SERIALIZE
	}, IS_OPTIONAL);

	END_JSON_SERIALIZE
}

UCSAssembly* FCSTypeReferenceMetaData::GetOwningAssemblyChecked() const
{
	UCSAssembly* Assembly = UCSManager::Get().FindOrLoadAssembly(AssemblyName);
	check(::IsValid(Assembly));
	return Assembly;
}

UClass* FCSTypeReferenceMetaData::GetAsClass() const
{
	UClass* Class = GetOwningAssemblyChecked()->FindType<UClass>(FieldName);
	ensure(Class);
	return Class;
}

UScriptStruct* FCSTypeReferenceMetaData::GetAsStruct() const
{
	UScriptStruct* Struct = GetOwningAssemblyChecked()->FindType<UScriptStruct>(FieldName);
	ensure(Struct);
	return Struct;
}

UEnum* FCSTypeReferenceMetaData::GetAsEnum() const
{
	return GetOwningAssemblyChecked()->FindType<UEnum>(FieldName);
}

UClass* FCSTypeReferenceMetaData::GetAsInterface() const
{
	return GetOwningAssemblyChecked()->FindType<UClass>(FieldName);
}

UDelegateFunction* FCSTypeReferenceMetaData::GetAsDelegate() const
{
	return GetOwningAssemblyChecked()->FindType<UDelegateFunction>(FieldName);
}

UPackage* FCSTypeReferenceMetaData::GetAsPackage() const
{
	return UCSManager::Get().GetPackage(FieldName.GetNamespace());
}
