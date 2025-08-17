#include "CSTypeReferenceMetaData.h"
#include "CSManager.h"
#include "TypeGenerator/Register/CSMetaDataUtils.h"

FCSTypeReferenceMetaData::FCSTypeReferenceMetaData(): FieldName(NAME_None, NAME_None)
{
}

UCSAssembly* FCSTypeReferenceMetaData::GetOwningAssemblyChecked() const
{
	UCSAssembly* Assembly = UCSManager::Get().FindOrLoadAssembly(AssemblyName);
	check(::IsValid(Assembly));
	return Assembly;
}

UClass* FCSTypeReferenceMetaData::GetOwningClass() const
{
	return GetOwningAssemblyChecked()->FindType<UClass>(FieldName);
}

UScriptStruct* FCSTypeReferenceMetaData::GetOwningStruct() const
{
	return GetOwningAssemblyChecked()->FindType<UScriptStruct>(FieldName);
}

UEnum* FCSTypeReferenceMetaData::GetOwningEnum() const
{
	return GetOwningAssemblyChecked()->FindType<UEnum>(FieldName);
}

UClass* FCSTypeReferenceMetaData::GetOwningInterface() const
{
	return GetOwningAssemblyChecked()->FindType<UClass>(FieldName);
}

UDelegateFunction* FCSTypeReferenceMetaData::GetOwningDelegate() const
{
	return GetOwningAssemblyChecked()->FindType<UDelegateFunction>(FieldName);
}

UPackage* FCSTypeReferenceMetaData::GetOwningPackage() const
{
	return UCSManager::Get().GetPackage(FieldName.GetNamespace());
}

void FCSTypeReferenceMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FString TypeName = JsonObject->GetStringField(TEXT("Name"));
	FString Namespace = JsonObject->GetStringField(TEXT("Namespace"));
	FieldName = FCSFieldName(*TypeName, *Namespace);

	FString AssemblyNameStr;
	if (JsonObject->TryGetStringField(TEXT("AssemblyName"), AssemblyNameStr))
	{
		AssemblyName = *AssemblyNameStr;
	}
	
	FCSMetaDataUtils::SerializeFromJson(JsonObject, MetaData);
}
