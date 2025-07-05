// Fill out your copyright notice in the Description page of Project Settings.


#include "FOptionalPropertyExporter.h"

#include "UObject/PropertyOptional.h"


bool UFOptionalPropertyExporter::IsSet(FOptionalProperty* OptionalProperty, const void* ScriptValue)
{
	return OptionalProperty->IsSet(ScriptValue);
}

void* UFOptionalPropertyExporter::MarkSetAndGetInitializedValuePointerToReplace(FOptionalProperty* OptionalProperty, void* Data)
{
	return OptionalProperty->MarkSetAndGetInitializedValuePointerToReplace(Data);
}

void UFOptionalPropertyExporter::MarkUnset(FOptionalProperty* OptionalProperty, void* Data)
{
	return OptionalProperty->MarkUnset(Data);
}

const void* UFOptionalPropertyExporter::GetValuePointerForRead(FOptionalProperty* OptionalProperty, const void* Data)
{
	return OptionalProperty->GetValuePointerForRead(Data);
}

void* UFOptionalPropertyExporter::GetValuePointerForReplace(FOptionalProperty* OptionalProperty, void* Data)
{
	return OptionalProperty->GetValuePointerForReplace(Data);
}

const void* UFOptionalPropertyExporter::GetValuePointerForReadIfSet(FOptionalProperty* OptionalProperty, const void* Data)
{
	return OptionalProperty->GetValuePointerForReadIfSet(Data);
}

void* UFOptionalPropertyExporter::GetValuePointerForReplaceIfSet(FOptionalProperty* OptionalProperty, void* Data)
{
	return OptionalProperty->GetValuePointerForReplaceIfSet(Data);
}

void* UFOptionalPropertyExporter::GetValuePointerForReadOrReplace(FOptionalProperty* OptionalProperty, void* Data)
{
	return OptionalProperty->GetValuePointerForReadOrReplace(Data);
}

void* UFOptionalPropertyExporter::GetValuePointerForReadOrReplaceIfSet(FOptionalProperty* OptionalProperty, void* Data)
{
	return OptionalProperty->GetValuePointerForReadOrReplaceIfSet(Data);
}

int32 UFOptionalPropertyExporter::CalcSize(FOptionalProperty* OptionalProperty)
{
	return OptionalProperty->CalcSize();
}
