#include "CSFunction_NoParams.h"

void UCSFunction_NoParams::InvokeManagedMethod_NoParams(UObject* ObjectToInvokeOn, FFrame& Stack, RESULT_DECL)
{
	FString ProfilerEventName = FString::Printf(TEXT("UCSFunction_NoParams::InvokeManagedMethod_NoParams %s"), *Stack.CurrentNativeFunction->GetName());
	TRACE_CPUPROFILER_EVENT_SCOPE_TEXT(*ProfilerEventName);
	
	UCSFunctionBase* Function = static_cast<UCSFunctionBase*>(Stack.CurrentNativeFunction);
	InvokeManagedEvent(ObjectToInvokeOn, Stack, Function, nullptr, RESULT_PARAM);
}