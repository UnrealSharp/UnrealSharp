#pragma once

#include "CSFieldName.h"

#if !defined(_WIN32)
#define __stdcall
#endif

struct FInvokeManagedMethodData;
struct GCHandleIntPtr;
struct FGCHandle;

class UNREALSHARPCORE_API FCSManagedCallbacks
{
	public:

	struct FManagedCallbacks
	{
		using ManagedCallbacks_CreateNewManagedObject = GCHandleIntPtr(__stdcall*)(void*, void*);
		using ManagedCallbacks_InvokeManagedEvent = int(__stdcall*)(GCHandleIntPtr, void*, void*, void*, void*);
		using ManagedCallbacks_InvokeDelegate = int(__stdcall*)(GCHandleIntPtr);
		using ManagedCallbacks_LookupMethod = uint8*(__stdcall*)(void*, const TCHAR*);
		using ManagedCallbacks_LookupType = uint8*(__stdcall*)(uint8*, const TCHAR*);
		using ManagedCallbacks_Dispose = void(__stdcall*)(GCHandleIntPtr);
		
		ManagedCallbacks_CreateNewManagedObject CreateNewManagedObject;
		ManagedCallbacks_InvokeManagedEvent InvokeManagedMethod;
		ManagedCallbacks_InvokeDelegate InvokeDelegate;
		ManagedCallbacks_LookupMethod LookupManagedMethod;
		ManagedCallbacks_LookupType LookupManagedType;

	private:
		
		//Only call these from GCHandles.
		friend FGCHandle;
		ManagedCallbacks_Dispose Dispose;
		
	};
	
	static inline FManagedCallbacks ManagedCallbacks;
	
};






