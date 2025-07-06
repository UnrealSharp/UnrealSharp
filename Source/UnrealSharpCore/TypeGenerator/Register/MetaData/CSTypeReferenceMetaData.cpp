#include "CSTypeReferenceMetaData.h"

#include "CSManager.h"
#include "TypeGenerator/Register/CSMetaDataUtils.h"

FCSTypeReferenceMetaData::FCSTypeReferenceMetaData(): FieldName(NAME_None, NAME_None)
{
}

TSharedPtr<FCSAssembly> FCSTypeReferenceMetaData::GetOwningAssemblyChecked() const
{
	TSharedPtr<FCSAssembly> Assembly = UCSManager::Get().FindOrLoadAssembly(AssemblyName);
	check(Assembly.IsValid());
	return Assembly;
}

UClass* FCSTypeReferenceMetaData::GetOwningClass() const
{
	TSharedPtr<FCSAssembly> Assembly = GetOwningAssemblyChecked();
	return Assembly->FindClass(FieldName);
}

UScriptStruct* FCSTypeReferenceMetaData::GetOwningStruct() const
{
	TSharedPtr<FCSAssembly> Assembly = GetOwningAssemblyChecked();
	return Assembly->FindStruct(FieldName);
}

UEnum* FCSTypeReferenceMetaData::GetOwningEnum() const
{
	TSharedPtr<FCSAssembly> Assembly = GetOwningAssemblyChecked();
	return Assembly->FindEnum(FieldName);
}

UClass* FCSTypeReferenceMetaData::GetOwningInterface() const
{
	TSharedPtr<FCSAssembly> Assembly = GetOwningAssemblyChecked();
	if (!Assembly.IsValid())
	{
		return nullptr;
	}

	return Assembly->FindInterface(FieldName);
}

UDelegateFunction* FCSTypeReferenceMetaData::GetOwningDelegate() const
{
	TSharedPtr<FCSAssembly> Assembly = GetOwningAssemblyChecked();
	return Assembly->FindDelegate(FieldName);
}

UPackage* FCSTypeReferenceMetaData::GetOwningPackage() const
{
	TSharedPtr<FCSAssembly> Assembly = GetOwningAssemblyChecked();
	return Assembly->GetPackage(FieldName.GetNamespace());
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
