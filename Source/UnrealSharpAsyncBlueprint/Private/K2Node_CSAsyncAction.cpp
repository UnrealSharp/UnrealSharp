#include "K2Node_CSAsyncAction.h"
#include "BlueprintActionDatabaseRegistrar.h"
#include "BlueprintFunctionNodeSpawner.h"
#include "Extensions/BlueprintActions/CSBlueprintAsyncActionBase.h"
#include "Utilities/CSClassUtilities.h"

#define LOCTEXT_NAMESPACE "K2Node"

bool FCSGetMenuActionsUtilities::IsFactoryMethod(const UFunction* Function, const UClass* InTargetType)
{
	if (!Function->HasAnyFunctionFlags(FUNC_Static))
	{
		return false;
	}

	if (Function->GetOwnerClass()->HasAnyClassFlags(CLASS_Deprecated | CLASS_NewerVersionExists))
	{
		return false;
	}
	
	FObjectProperty* ReturnProperty = CastField<FObjectProperty>(Function->GetReturnProperty());
	bool const bIsFactoryMethod = ReturnProperty != nullptr && ReturnProperty->PropertyClass != nullptr && ReturnProperty->PropertyClass->IsChildOf(InTargetType);
	return bIsFactoryMethod;
}

void FCSGetMenuActionsUtilities::SetNodeFunc(UEdGraphNode* NewNode, bool, TWeakObjectPtr<UFunction> FunctionPtr)
{
	UK2Node_CSAsyncAction* AsyncTaskNode = CastChecked<UK2Node_CSAsyncAction>(NewNode);
	
	if (!FunctionPtr.IsValid())
	{
		return;
	}

	UFunction* Function = FunctionPtr.Get();
	FObjectProperty* ReturnProp = CastFieldChecked<FObjectProperty>(Function->GetReturnProperty());
						
	AsyncTaskNode->ProxyFactoryFunctionName = Function->GetFName();
	AsyncTaskNode->ProxyFactoryClass = Function->GetOuterUClass();
	AsyncTaskNode->ProxyClass = ReturnProp->PropertyClass;
}

UBlueprintNodeSpawner* FCSGetMenuActionsUtilities::MakeAction(UClass* NodeClass, const UFunction* FactoryFunc)
{
	UBlueprintNodeSpawner* NodeSpawner = UBlueprintFunctionNodeSpawner::Create(FactoryFunc);
	NodeSpawner->NodeClass = NodeClass;

	TWeakObjectPtr<UFunction> FunctionPtr = MakeWeakObjectPtr(const_cast<UFunction*>(FactoryFunc));
	NodeSpawner->CustomizeNodeDelegate = UBlueprintNodeSpawner::FCustomizeNodeDelegate::CreateStatic(SetNodeFunc, FunctionPtr);
	
	return NodeSpawner;
}

UK2Node_CSAsyncAction::UK2Node_CSAsyncAction()
{
	ProxyActivateFunctionName = GET_FUNCTION_NAME_CHECKED(UCSBlueprintAsyncActionBase, Activate);
	ProxyFactoryClass = UCSBlueprintAsyncActionBase::StaticClass();
	ProxyClass = UCSBlueprintAsyncActionBase::StaticClass();
}

void UK2Node_CSAsyncAction::GetMenuActions(FBlueprintActionDatabaseRegistrar& ActionRegistrar) const
{
	for (TObjectIterator<UCSClass> ClassIt; ClassIt; ++ClassIt)
	{
		UCSClass* ManagedClass = *ClassIt;
		
		if (ManagedClass->HasAnyClassFlags(CLASS_Abstract) || !ManagedClass->IsChildOf(ProxyFactoryClass) || FCSClassUtilities::IsSkeletonType(ManagedClass))
		{
			continue;
		}

		for (TFieldIterator<UFunction> FunctionIterator(ManagedClass, EFieldIteratorFlags::ExcludeSuper); FunctionIterator; ++FunctionIterator)
		{
			UFunction* Function = *FunctionIterator;

			if (!FCSGetMenuActionsUtilities::IsFactoryMethod(Function, ManagedClass))
			{
				continue;
			}

			UBlueprintNodeSpawner* NewAction = FCSGetMenuActionsUtilities::MakeAction(GetClass(), Function);
			if (!IsValid(NewAction))
			{
				continue;
			}

			ActionRegistrar.AddBlueprintAction(ManagedClass, NewAction);
		}
	}
}

void UK2Node_CSAsyncAction::ExpandNode(class FKismetCompilerContext& CompilerContext, UEdGraph* SourceGraph)
{
	if (ProxyClass->bLayoutChanging)
	{
		// Don't compile while the async wrapper class is being hot reloaded. Will be compiled later.
		return;
	}
	
	Super::ExpandNode(CompilerContext, SourceGraph);
}

#undef LOCTEXT_NAMESPACE
