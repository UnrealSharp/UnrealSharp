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
	return GetOwningAssemblyChecked()->FindClass(FieldName);
}

UScriptStruct* FCSTypeReferenceMetaData::GetOwningStruct() const
{
	return GetOwningAssemblyChecked()->FindStruct(FieldName);
}

UEnum* FCSTypeReferenceMetaData::GetOwningEnum() const
{
	return GetOwningAssemblyChecked()->FindEnum(FieldName);
}

UClass* FCSTypeReferenceMetaData::GetOwningInterface() const
{
	return GetOwningAssemblyChecked()->FindInterface(FieldName);
}

UDelegateFunction* FCSTypeReferenceMetaData::GetOwningDelegate() const
{
	return GetOwningAssemblyChecked()->FindDelegate(FieldName);
}

UPackage* FCSTypeReferenceMetaData::GetOwningPackage() const
{
	return GetOwningAssemblyChecked()->GetPackage(FieldName.GetNamespace());
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
