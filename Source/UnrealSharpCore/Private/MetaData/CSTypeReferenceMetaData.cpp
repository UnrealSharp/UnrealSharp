#include "MetaData/CSTypeReferenceMetaData.h"
#include "CSManager.h"

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
