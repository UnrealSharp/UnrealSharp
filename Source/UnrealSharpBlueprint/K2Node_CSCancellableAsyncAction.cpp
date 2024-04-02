#include "K2Node_CSCancellableAsyncAction.h"

#include "BlueprintActionDatabaseRegistrar.h"
#include "BlueprintFunctionNodeSpawner.h"
#include "BlueprintNodeSpawner.h"
#include "Delegates/Delegate.h"
#include "EdGraph/EdGraphNode.h"
#include "HAL/Platform.h"
#include "Kismet/BlueprintAsyncActionBase.h"
#include "Misc/AssertionMacros.h"
#include "Templates/Casts.h"
#include "Templates/SubclassOf.h"
#include "UObject/Class.h"
#include "UObject/Field.h"
#include "UObject/NameTypes.h"
#include "UObject/ObjectPtr.h"
#include "UObject/UnrealType.h"
#include "UObject/WeakObjectPtrTemplates.h"

#include "Extensions/CSCancellableAsyncAction.h"

#define LOCTEXT_NAMESPACE "K2Node"

UK2Node_CSCancellableAsyncAction::UK2Node_CSCancellableAsyncAction()
{
	ProxyActivateFunctionName = GET_FUNCTION_NAME_CHECKED(UCSCancellableAsyncAction, Activate);
}

void UK2Node_CSCancellableAsyncAction::GetMenuActions(FBlueprintActionDatabaseRegistrar& ActionRegistrar) const
{
	struct GetMenuActions_Utils
	{
		static bool IsFactoryMethod(const UFunction* Function, const UClass* InTargetType)
		{
			if (!Function->HasAnyFunctionFlags(FUNC_Static))
			{
				return false;
			}

			if (!Function->GetOwnerClass()->HasAnyClassFlags(CLASS_Deprecated | CLASS_NewerVersionExists))
			{
				FObjectProperty* ReturnProperty = CastField<FObjectProperty>(Function->GetReturnProperty());
				// see if the function is a static factory method
				bool const bIsFactoryMethod = (ReturnProperty != nullptr) && (ReturnProperty->PropertyClass != nullptr) &&
					ReturnProperty->PropertyClass->IsChildOf(InTargetType);

				return bIsFactoryMethod;
			}
			else
			{
				return false;
			}
		}

		static void SetNodeFunc(UEdGraphNode* NewNode, bool /*bIsTemplateNode*/, TWeakObjectPtr<UFunction> FunctionPtr)
		{
			UK2Node_CSCancellableAsyncAction* AsyncTaskNode = CastChecked<UK2Node_CSCancellableAsyncAction>(NewNode);
			if (FunctionPtr.IsValid())
			{
				UFunction* Func = FunctionPtr.Get();
				FObjectProperty* ReturnProp = CastFieldChecked<FObjectProperty>(Func->GetReturnProperty());
						
				AsyncTaskNode->ProxyFactoryFunctionName = Func->GetFName();
				AsyncTaskNode->ProxyFactoryClass        = Func->GetOuterUClass();
				AsyncTaskNode->ProxyClass               = ReturnProp->PropertyClass;
			}
		}

		static UBlueprintNodeSpawner* MakeAction(UClass* NodeClass, const UFunction* FactoryFunc)
		{
			UClass* FactoryClass = FactoryFunc ? FactoryFunc->GetOwnerClass() : nullptr;
			if (FactoryClass && FactoryClass->HasMetaData(TEXT("HasDedicatedAsyncNode")))
			{
				// Wants to use a more specific blueprint node to handle the async action
				return nullptr;
			}

			UBlueprintNodeSpawner* NodeSpawner = UBlueprintFunctionNodeSpawner::Create(FactoryFunc);
			check(NodeSpawner != nullptr);
			NodeSpawner->NodeClass = NodeClass;

			TWeakObjectPtr<UFunction> FunctionPtr = MakeWeakObjectPtr(const_cast<UFunction*>(FactoryFunc));
			NodeSpawner->CustomizeNodeDelegate = UBlueprintNodeSpawner::FCustomizeNodeDelegate::CreateStatic(GetMenuActions_Utils::SetNodeFunc, FunctionPtr);

			return NodeSpawner;
		}
	};

	UClass* NodeClass = GetClass();
	UClass* TargetType = UCSCancellableAsyncAction::StaticClass();

	for (TObjectIterator<UClass> ClassIt; ClassIt; ++ClassIt)
	{
		UClass* Class = *ClassIt;
		if (Class->HasAnyClassFlags(CLASS_Abstract) || !Class->IsChildOf(TargetType))
		{
			continue;
		}

		for (TFieldIterator<UFunction> FuncIt(Class, EFieldIteratorFlags::ExcludeSuper); FuncIt; ++FuncIt)
		{
			UFunction* Function = *FuncIt;
			if (!GetMenuActions_Utils::IsFactoryMethod(Function, TargetType))
			{
				continue;
			}
			else if (UBlueprintNodeSpawner* NewAction = GetMenuActions_Utils::MakeAction(NodeClass, Function))
			{
				ActionRegistrar.AddBlueprintAction(Class, NewAction);
			}
		}
	}
}

#undef LOCTEXT_NAMESPACE
