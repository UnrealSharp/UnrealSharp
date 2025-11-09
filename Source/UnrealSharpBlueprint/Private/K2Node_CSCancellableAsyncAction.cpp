#include "K2Node_CSCancellableAsyncAction.h"
#include "Extensions/BlueprintActions/CSCancellableAsyncAction.h"

UK2Node_CSCancellableAsyncAction::UK2Node_CSCancellableAsyncAction()
{
	ProxyActivateFunctionName = GET_FUNCTION_NAME_CHECKED(UCSCancellableAsyncAction, Activate);
	ProxyFactoryClass = UCSCancellableAsyncAction::StaticClass();
	ProxyClass = UCSCancellableAsyncAction::StaticClass();
}
