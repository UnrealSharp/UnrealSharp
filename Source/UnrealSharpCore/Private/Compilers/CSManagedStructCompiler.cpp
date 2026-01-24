#include "Compilers/CSManagedStructCompiler.h"
#include "CSManager.h"
#include "ReflectionData/CSStructReflectionData.h"
#include "Types/CSScriptStruct.h"
#include "Factories/CSPropertyFactory.h"
#include "UnrealSharpUtils.h"

#if WITH_EDITOR
#include "UserDefinedStructure/UserDefinedStructEditorData.h"
#endif

UCSManagedStructCompiler::UCSManagedStructCompiler()
{
	FieldType = UCSScriptStruct::StaticClass();
}

void UCSManagedStructCompiler::Recompile(UField* TypeToRecompile, const TSharedPtr<FCSManagedTypeDefinition>& ManagedTypeDefinition) const
{
	UCSScriptStruct* Struct = static_cast<UCSScriptStruct*>(TypeToRecompile);
	TSharedPtr<FCSStructReflectionData> StructReflectionData = ManagedTypeDefinition->GetReflectionData<FCSStructReflectionData>();
	
	PurgeStruct(Struct);
	
	FCSPropertyFactory::CreateAndAssignProperties(Struct, StructReflectionData->Properties);
    
	Struct->Status = UDSS_UpToDate;
	if (!Struct->Guid.IsValid())
	{
		Struct->Guid = FCSUnrealSharpUtils::ConstructGUIDFromString(Struct->GetName());
	}
	
	Struct->Bind();
	Struct->StaticLink(true);
	Struct->Initialize();
	
	RegisterFieldToLoader(TypeToRecompile, ENotifyRegistrationType::NRT_Struct);

#if WITH_EDITOR
	UCSManager::Get().OnNewStructEvent().Broadcast(Struct);
#endif
}

TSharedPtr<FCSTypeReferenceReflectionData> UCSManagedStructCompiler::CreateNewReflectionData() const
{
	return MakeShared<FCSStructReflectionData>();
}

void UCSManagedStructCompiler::PurgeStruct(UCSScriptStruct* Field)
{
	FCSUnrealSharpUtils::PurgeStruct(Field);
#if WITH_EDITORONLY_DATA
	Field->ErrorMessage.Empty();
	Field->PrimaryStruct = nullptr;
	Field->EditorData = nullptr;
#endif
}
