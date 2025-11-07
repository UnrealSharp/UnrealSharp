// Fill out your copyright notice in the Description page of Project Settings.


#include "UEnumExporter.h"

#include "CSManager.h"
#include "Types/CSEnum.h"

FGCHandleIntPtr UUEnumExporter::GetManagedEnumType(UEnum* ScriptEnum)
{
	if (const UCSEnum* CSEnum = Cast<UCSEnum>(ScriptEnum); CSEnum != nullptr)
	{
		return CSEnum->GetManagedTypeInfo()->GetTypeHandle()->GetHandle();
	}

	const UCSAssembly* Assembly = UCSManager::Get().FindOwningAssembly(ScriptEnum);
	if (Assembly == nullptr)
	{
		return FGCHandleIntPtr();
	}

	const FCSFieldName FieldName(ScriptEnum);
	const TSharedPtr<FCSManagedTypeInfo> Info = Assembly->FindTypeInfo(FieldName);
	if (!Info.IsValid())
	{
		return FGCHandleIntPtr();
	}

	return Info->GetTypeHandle()->GetHandle();   
}
