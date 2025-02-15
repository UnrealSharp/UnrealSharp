#include "CSTypeReferenceMetaData.h"

#include "CSManager.h"
#include "TypeGenerator/Register/CSMetaDataUtils.h"

TSharedPtr<FCSAssembly> FCSTypeReferenceMetaData::GetOwningAssemblyChecked() const
{
	TSharedPtr<FCSAssembly> Assembly = UCSManager::Get().FindOrLoadAssembly(AssemblyName);
	check(Assembly.IsValid());
	return Assembly;
}

UClass* FCSTypeReferenceMetaData::GetOwningClass() const
{
	TSharedPtr<FCSAssembly> Assembly = GetOwningAssemblyChecked();
	return Assembly->FindClass(Name);
}

UScriptStruct* FCSTypeReferenceMetaData::GetOwningStruct() const
{
	TSharedPtr<FCSAssembly> Assembly = GetOwningAssemblyChecked();
	return Assembly->FindStruct(Name);
}

UEnum* FCSTypeReferenceMetaData::GetOwningEnum() const
{
	TSharedPtr<FCSAssembly> Assembly = GetOwningAssemblyChecked();
	return Assembly->FindEnum(Name);
}

UClass* FCSTypeReferenceMetaData::GetOwningInterface() const
{
	TSharedPtr<FCSAssembly> Assembly = GetOwningAssemblyChecked();
	if (!Assembly.IsValid())
	{
		return nullptr;
	}

	return Assembly->FindInterface(Name);
}

UPackage* FCSTypeReferenceMetaData::GetOwningPackage() const
{
	TSharedPtr<FCSAssembly> Assembly = GetOwningAssemblyChecked();
	return Assembly->GetPackage(Namespace);
}

void FCSTypeReferenceMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
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
	
	FCSMetaDataUtils::SerializeFromJson(JsonObject, MetaData);
}
