#include "CSFunction_NoParams.h"

void UCSFunction_NoParams::InvokeManagedMethod_NoParams(UObject* ObjectToInvokeOn, FFrame& Stack, RESULT_DECL)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSFunction_NoParams::InvokeManagedMethod_NoParams);
	
	UCSFunctionBase* Function = static_cast<UCSFunctionBase*>(Stack.CurrentNativeFunction);
	InvokeManagedEvent(ObjectToInvokeOn, Stack, Function, nullptr, RESULT_PARAM);
}