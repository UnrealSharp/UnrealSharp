#include "Types/CSScriptStruct.h"

#include "UnrealSharpUtils.h"
#include "UserDefinedStructure/UserDefinedStructEditorData.h"

void UCSScriptStruct::Initialize()
{
#if WITH_EDITORONLY_DATA
	PrimaryStruct = this;
#endif
	
	InitializeStructDefaults();
	DefaultStructInstance = FUserStructOnScopeIgnoreDefaults(this, StructDefaults.Get());
	UpdateStructFlags();
	PopulateEditorData();
}

void UCSScriptStruct::InitializeStruct(void* Dest, int32 ArrayDim) const
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSScriptStruct::InitializeStruct);
	
	int32 Size = GetStructureSize();
	uint8* DefaultData = StructDefaults.Get();
	
	if (Size <= 0 || !DefaultData)
	{
		return;
	}
	
	for (FProperty* Property = PropertyLink; Property; Property = Property->PropertyLinkNext)
	{
		auto CalculateOffset = [](FProperty* Property, int32 Index, int32 Size, void* Data) -> uint8*
		{
			return static_cast<uint8*>(Data) + Index * Size + Property->GetOffset_ForInternal();
		};
		
		for (int32 Index = 0; Index < ArrayDim; Index++)
		{
			uint8* DestPtr = CalculateOffset(Property, Index, Size, Dest);
			uint8* SrcPtr = CalculateOffset(Property, Index, Size, DefaultData);
			Property->CopyCompleteValue(DestPtr, SrcPtr);
		}
	}
}

void UCSScriptStruct::InitializeStructDefaults()
{
	int32 Size = GetStructureSize();
	
	if (Size <= 0)
	{
		return;
	}
	
	StructDefaults = MakeUnique<uint8[]>(Size);
	FCSManagedCallbacks::ManagedCallbacks.InitializeStructure(GetTypeGCHandle()->GetHandle(), StructDefaults.Get());
}

void UCSScriptStruct::PopulateEditorData()
{
	if (!IsValid(EditorData))
	{
		EditorData = NewObject<UUserDefinedStructEditorData>(this, NAME_None, RF_Transactional);
	}
	
	UUserDefinedStructEditorData* EditorDataInstance = static_cast<UUserDefinedStructEditorData*>(EditorData);
	EditorDataInstance->VariablesDescriptions.Reset();
	
	for (TFieldIterator<FProperty> It(this); It; ++It)
	{
		FProperty* Property = *It;
		
		FStructVariableDescription& VarDesc = EditorDataInstance->VariablesDescriptions.AddDefaulted_GetRef();
		VarDesc.VarName = Property->GetFName();
		VarDesc.VarGuid = FCSUnrealSharpUtils::ConstructGUIDFromName(Property->GetFName());
		
		FString DefaultValue;
		if (!Property->ExportText_InContainer(0, DefaultValue, StructDefaults.Get(), StructDefaults.Get(), this, PPF_None))
		{
			continue;
		}
		
		VarDesc.DefaultValue = DefaultValue;
		Property->SetMetaData(TEXT("MakeStructureDefaultValue"), *VarDesc.DefaultValue);
	}
}
