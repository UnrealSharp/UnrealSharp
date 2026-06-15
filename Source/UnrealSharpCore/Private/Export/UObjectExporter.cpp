#include "CSManager.h"
#include "UObject/UObjectGlobals.h"

DECLARE_UNREALSHARP_EXPORTER(UObjectExporter)
{
	void* CreateNewObject(UObject* Outer, UClass* Class, UObject* Template)
	{
		if (!IsValid(Outer))
		{
			return nullptr;
		}
		
		UObject* NewCSharpObject = NewObject<UObject>(Outer, Class, NAME_None, RF_NoFlags, Template);
		return UCSManager::Get().FindManagedObject(NewCSharpObject);
	}

	void* GetTransientPackage()
	{
		UPackage* TransientPackage = ::GetTransientPackage();

		if (!IsValid(TransientPackage))
		{
			return nullptr;
		}

		return UCSManager::Get().FindManagedObject(TransientPackage);
	}

	void NativeGetName(UObject* Object, FName* OutName)
	{
		*OutName = IsValid(Object) ? Object->GetFName() : NAME_None;
	}
	
	void InvokeNativeFunctionOutParms(UObject* NativeObject, UFunction* NativeFunction, uint8* Params, uint8* ReturnValueAddress)
	{
		TRACE_CPUPROFILER_EVENT_SCOPE(InvokeNativeFunctionOutParms);

		//choose a most fast path. the assumption is that functions defined in U# which are not overridden in BP code can choose a fast path.
		//if the function calls into BP code we need dedicated stack memory to not risk a stack corruption for BP to use.
		if (!NativeFunction->HasAnyFunctionFlags(FUNC_Native) && NativeFunction->HasAnyFunctionFlags(FUNC_Event))
		{
			NativeObject->ProcessEvent(NativeFunction, Params);
		}
		else
		{
			FFrame NewStack(NativeObject, NativeFunction, Params, nullptr, NativeFunction->ChildProperties);
			FOutParmRec** LastOut = &NewStack.OutParms;

			for (TFieldIterator<FProperty> PropIt(NativeFunction); PropIt; ++PropIt)
			{
				FProperty* Property = *PropIt;

				if (!Property->HasAllPropertyFlags(CPF_OutParm))
				{
					continue;
				}

				FOutParmRec* Out = static_cast<FOutParmRec*>(UE_VSTACK_ALLOC(VirtualStackAllocator, sizeof(FOutParmRec)));

				Out->PropAddr = Property->ContainerPtrToValuePtr<uint8>(Params);
				Out->Property = Property;

				if (*LastOut)
				{
					(*LastOut)->NextOutParm = Out;
					LastOut = &(*LastOut)->NextOutParm;
				}
				else
				{
					*LastOut = Out;
				}
			}

			if (*LastOut)
			{
				(*LastOut)->NextOutParm = nullptr;
			}

			NativeFunction->Invoke(NativeObject, NewStack, ReturnValueAddress);
		}
	}

	void EvaluateInvokePath(UObject* NativeObject, UFunction* NativeFunction, uint8* Params, uint8* ReturnValueAddress)
	{
		if (NativeFunction->HasAllFunctionFlags(FUNC_HasOutParms))
		{
			InvokeNativeFunctionOutParms(NativeObject, NativeFunction, Params, ReturnValueAddress);
		}
	    //if the function is an event and not native we would go through UObject::ProcessEvent to avoid stack corruption since it will call into BP code
		else if (!NativeFunction->HasAnyFunctionFlags(FUNC_Native) && NativeFunction->HasAnyFunctionFlags(FUNC_Event))
		{
			NativeObject->ProcessEvent(NativeFunction, Params);
		}
	    //if the function is native we can go through the fast path. it could also contain the event flag which is common for u# functions
		else
		{
			FFrame NewStack(NativeObject, NativeFunction, Params, nullptr, NativeFunction->ChildProperties);
			NativeFunction->Invoke(NativeObject, NewStack, ReturnValueAddress);
		}
	}

	void InvokeNativeFunction(UObject* NativeObject, UFunction* NativeFunction, uint8* Params, uint8* ReturnValueAddress)
	{
		TRACE_CPUPROFILER_EVENT_SCOPE(InvokeNativeFunction);

	    EvaluateInvokePath(NativeObject, NativeFunction, Params, ReturnValueAddress);
	}

	void InvokeNativeStaticFunction(UClass* NativeClass, UFunction* NativeFunction, uint8* Params, uint8* ReturnValueAddress)
	{
		TRACE_CPUPROFILER_EVENT_SCOPE(InvokeNativeStaticFunction);

		UObject* ClassDefaultObject = NativeClass->GetDefaultObject();
		EvaluateInvokePath(ClassDefaultObject, NativeFunction, Params, ReturnValueAddress);
	}

	void InvokeNativeNetFunction(UObject* NativeObject, UFunction* NativeFunction, uint8* Params, uint8* ReturnValueAddress)
	{
		TRACE_CPUPROFILER_EVENT_SCOPE(InvokeNativeNetFunction);

		int32 FunctionCallspace = NativeObject->GetFunctionCallspace(NativeFunction, nullptr);

		if (FunctionCallspace & FunctionCallspace::Remote)
		{
			NativeObject->CallRemoteFunction(NativeFunction, Params, nullptr, nullptr);
		}

		if ((FunctionCallspace & FunctionCallspace::Local) == 0)
		{
			return;
		}

		if (FunctionCallspace & FunctionCallspace::Absorbed)
		{
			return;
		}

		EvaluateInvokePath(NativeObject, NativeFunction, Params, ReturnValueAddress);
	}

	bool NativeIsValid(UObject* Object)
	{
		return IsValid(Object);
	}

	void* GetWorld_Internal(UObject* Object)
	{
		if (!IsValid(Object))
		{
			UE_LOG(LogUnrealSharp, Warning, TEXT("GetWorld_Internal called with invalid object"));
			return nullptr;
		}

		UWorld* World = Object->GetWorld();
		return UCSManager::Get().FindManagedObject(World);
	}

	bool IsA(const UObject* Object, UClass* Class)
	{
	    return Object->IsA(Class);
	}

	uint32 GetUniqueID(UObject* Object)
	{
		return Object->GetUniqueID();
	}

	void* GetOuter(UObject* Object)
	{
		if (!IsValid(Object))
		{
			UE_LOG(LogUnrealSharp, Warning, TEXT("GetOuter called with invalid object"));
			return nullptr;
		}

		UObject* Outer = Object->GetOuter();
		return UCSManager::Get().FindManagedObject(Outer);
	}

	void* StaticLoadClass(UClass* BaseClass, UObject* InOuter, const char* Name)
	{
		if (Name == nullptr)
		{
			return nullptr;
		}

		UClass* Loaded = ::StaticLoadClass(BaseClass, InOuter, UTF8_TO_TCHAR(Name));
		if (!IsValid(Loaded))
		{
			return nullptr;
		}
		return UCSManager::Get().FindManagedObject(Loaded);
	}

	void* StaticLoadObject(UClass* BaseClass, UObject* InOuter, const char* Name)
	{
		if (Name == nullptr)
		{
			return nullptr;
		}

		UObject* LoadedObj = ::StaticLoadObject(BaseClass, InOuter, UTF8_TO_TCHAR(Name));
		if (!IsValid(LoadedObj))
		{
			return nullptr;
		}
		return UCSManager::Get().FindManagedObject(LoadedObj);
	}

	bool ImplementsInterface(UObject* Object, UClass* InterfaceClass)
	{
		return IsValid(Object)
			&& IsValid(InterfaceClass)
			&& Object->GetClass()->ImplementsInterface(InterfaceClass);
	}
	
	EXPORT_UNREALSHARP_FUNCTION(CreateNewObject)
	EXPORT_UNREALSHARP_FUNCTION(GetTransientPackage)
	EXPORT_UNREALSHARP_FUNCTION(NativeGetName)
	EXPORT_UNREALSHARP_FUNCTION(InvokeNativeFunction)
	EXPORT_UNREALSHARP_FUNCTION(InvokeNativeStaticFunction)
	EXPORT_UNREALSHARP_FUNCTION(InvokeNativeNetFunction)
	EXPORT_UNREALSHARP_FUNCTION(InvokeNativeFunctionOutParms)
	EXPORT_UNREALSHARP_FUNCTION(NativeIsValid)
	EXPORT_UNREALSHARP_FUNCTION(GetWorld_Internal)
	EXPORT_UNREALSHARP_FUNCTION(IsA)
	EXPORT_UNREALSHARP_FUNCTION(GetUniqueID)
	EXPORT_UNREALSHARP_FUNCTION(GetOuter)
	EXPORT_UNREALSHARP_FUNCTION(StaticLoadClass)
	EXPORT_UNREALSHARP_FUNCTION(StaticLoadObject)
	EXPORT_UNREALSHARP_FUNCTION(ImplementsInterface)
}
