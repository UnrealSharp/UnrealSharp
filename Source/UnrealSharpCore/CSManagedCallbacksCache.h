#pragma once

#if !defined(_WIN32)
#define __stdcall
#endif

struct FInvokeManagedMethodData;
struct FGCHandleIntPtr;
struct FGCHandle;

class UNREALSHARPCORE_API FCSManagedCallbacks
{
	public:

	struct FManagedCallbacks
	{
		using ManagedCallbacks_CreateNewManagedObject = FGCHandleIntPtr(__stdcall*)(void*, void*);
		using ManagedCallbacks_InvokeManagedEvent = int(__stdcall*)(void*, void*, void*, void*, void*);
		using ManagedCallbacks_InvokeDelegate = int(__stdcall*)(FGCHandleIntPtr);
		using ManagedCallbacks_LookupMethod = uint8*(__stdcall*)(void*, const TCHAR*);
		using ManagedCallbacks_LookupType = uint8*(__stdcall*)(uint8*, const TCHAR*);
		using ManagedCallbacks_Dispose = void(__stdcall*)(FGCHandleIntPtr, FGCHandleIntPtr);
		
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






