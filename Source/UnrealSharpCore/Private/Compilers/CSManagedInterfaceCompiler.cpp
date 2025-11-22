#include "Compilers/CSManagedInterfaceCompiler.h"
#include "CSManager.h"
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

	UClass* ParentInterface;
	if (ClassReflectionData->ParentClass.IsValid())
	{
		ParentInterface = ClassReflectionData->ParentClass.GetAsInterface();
	}
	else
	{
		ParentInterface = UInterface::StaticClass();
	}
	
	Interface->SetSuperStruct(ParentInterface);
	Interface->ClassFlags |= CLASS_Interface;
	
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
	Interface->GetDefaultObject();

#if WITH_EDITOR
	UCSManager::Get().OnNewInterfaceEvent().Broadcast(Interface);
#endif

	RegisterFieldToLoader(TypeToRecompile, ENotifyRegistrationType::NRT_Class);
}
