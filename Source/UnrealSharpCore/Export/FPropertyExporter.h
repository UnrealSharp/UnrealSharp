// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "FPropertyExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class UNREALSHARPCORE_API UFPropertyExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:
	
	static FProperty* GetNativePropertyFromName(UStruct* Struct, const char* PropertyName);
	static int32 GetPropertyOffsetFromName(UStruct* InStruct, const char* InPropertyName);
	static int32 GetPropertyArrayDimFromName(UStruct* InStruct, const char* PropertyName);
	static int32 GetPropertyOffset(FProperty* Property);
	static int32 GetSize(FProperty* Property);
	static int32 GetArrayDim(FProperty* Property);
	static void DestroyValue(FProperty* Property, void* Value);
	static void DestroyValue_InContainer(FProperty* Property, void* Value);
	static void InitializeValue(FProperty* Property, void* Value);
	static bool Identical(const FProperty* Property, void* ValueA, void* ValueB);
	static void GetInnerFields(FProperty* SetProperty, TArray<FField*>* OutFields);
	static uint32 GetValueTypeHash(FProperty* Property, void* Source);
	static bool HasAnyPropertyFlags(FProperty* Property, EPropertyFlags FlagsToCheck);
	static bool HasAllPropertyFlags(FProperty* Property, EPropertyFlags FlagsToCheck);
	static void CopySingleValue(FProperty* Property, void* Dest, void* Src);
	static void GetValue_InContainer(FProperty* Property, void* Container, void* OutValue);
	static void SetValue_InContainer(FProperty* Property, void* Container, void* Value);
	static uint8 GetBoolPropertyFieldMaskFromName(UStruct* InStruct, const char* InPropertyName);
};
