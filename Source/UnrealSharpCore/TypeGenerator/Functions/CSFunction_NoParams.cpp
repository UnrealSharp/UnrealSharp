#include "CSFunction_NoParams.h"

void UCSFunction_NoParams::InvokeManagedMethod_NoParams(UObject* ObjectToInvokeOn, FFrame& Stack, RESULT_DECL)
{
	TRACE_CPUPROFILER_EVENT_SCOPE_TEXT(*FString(TEXT("UCSFunction_NoParams::InvokeManagedMethod_NoParams ") + Stack.CurrentNativeFunction->GetName()));
	
	UCSFunctionBase* Function = static_cast<UCSFunctionBase*>(Stack.CurrentNativeFunction);
	InvokeManagedEvent(ObjectToInvokeOn, Stack, Function, nullptr, RESULT_PARAM);
}