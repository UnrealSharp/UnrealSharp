#include "Compilers/CSManagedInterfaceCompiler.h"
#include "CSManager.h"
#include "Compilers/CSManagedClassCompiler.h"
#include "Types/CSInterface.h"
#include "Factories/CSFunctionFactory.h"

UCSManagedInterfaceCompiler::UCSManagedInterfaceCompiler()
{
	FieldType = UCSInterface::StaticClass();
}

void UCSManagedInterfaceCompiler::Recompile(UField* TypeToRecompile, const TSharedPtr<FCSManagedTypeDefinition>& ManagedTypeDefinition) const
{
	UCSInterface* Interface = static_cast<UCSInterface*>(TypeToRecompile);
	TSharedPtr<FCSClassBaseReflectionData> ClassReflectionData = ManagedTypeDefinition->GetReflectionData<FCSClassBaseReflectionData>();
	
	Interface->PurgeClass(true);

	UClass* ParentInterface = UInterface::StaticClass();
	
	Interface->SetSuperStruct(ParentInterface);
	Interface->ClassFlags |= CLASS_Interface;

	Interface->SetMetaData(FBlueprintMetadata::MD_AllowableBlueprintVariableType, TEXT("true"));
	
	FCSFunctionFactory::GenerateFunctions(Interface, ClassReflectionData->Functions);

	for (TFieldIterator<UFunction> It(Interface, EFieldIterationFlags::None); It; ++It)
	{
		UFunction* Function = *It;
		
		NotifyRegistrationEvent(*Function->GetOutermost()->GetName(),
		*Function->GetName(),
		ENotifyRegistrationType::NRT_Struct,
		ENotifyRegistrationPhase::NRP_Finished,
		nullptr,
		false,
		Function);
	}

	Interface->ClassConstructor = UInterface::StaticClass()->ClassConstructor;
	
	Interface->StaticLink(true);
	Interface->Bind();
	Interface->AssembleReferenceTokenStream();
	(void)Interface->GetDefaultObject();

	RegisterFieldToLoader(TypeToRecompile, ENotifyRegistrationType::NRT_Class);
	
#if WITH_EDITOR
	UCSManager::Get().OnNewInterfaceEvent().Broadcast(Interface);
	UCSManagedClassCompiler::RefreshClassActions(Interface);
#endif
}

TSharedPtr<FCSTypeReferenceReflectionData> UCSManagedInterfaceCompiler::CreateNewReflectionData() const
{
	return MakeShared<FCSClassBaseReflectionData>();
}
