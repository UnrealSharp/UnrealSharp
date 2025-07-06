#pragma once

#include "CSTypeInfo.h"
#include "TypeGenerator/Register/MetaData/CSClassMetaData.h"
#include "UnrealSharpCore/TypeGenerator/Register/CSGeneratedClassBuilder.h"

struct UNREALSHARPCORE_API FCSClassInfo : TCSTypeInfo<FCSClassMetaData, UClass, FCSGeneratedClassBuilder>
{
	FCSClassInfo(const TSharedPtr<FJsonValue>& MetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly);
	FCSClassInfo(UClass* InField, const TSharedPtr<FCSAssembly>& InOwningAssembly, const TSharedPtr<FGCHandle>& TypeHandle);

	// TCharpTypeInfo interface implementation
	virtual UClass* InitializeBuilder() override;
	// End of implementation

	TSharedPtr<FGCHandle> GetManagedTypeHandle()
	{
#if WITH_EDITOR
		if (!ManagedTypeHandle.IsValid() || ManagedTypeHandle->IsNull())
		{
			// Lazy load the type handle in editor. Gets null during hot reload.
			FCSFieldName FieldName = FCSClassUtilities::IsManagedType(Field) ? TypeMetaData->FieldName : FCSFieldName(Field);
			ManagedTypeHandle = OwningAssembly->TryFindTypeHandle(FieldName);

			if (!ManagedTypeHandle.IsValid() || ManagedTypeHandle->IsNull())
			{
				UE_LOGFMT(LogUnrealSharp, Error, "Failed to find type handle for class: {0}", *FieldName.GetFullName().ToString());
				return nullptr;
			}
		}
#endif
		return ManagedTypeHandle;
	}

private:
	friend struct FCSAssembly;
	TSharedPtr<FGCHandle> ManagedTypeHandle;
};
