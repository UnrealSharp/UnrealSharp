#pragma once

struct FInvokeManagedMethodData;
struct GCHandleIntPtr;
struct FGCHandle;

class CSHARPFORUE_API FCSManagedCallbacks
{
	public:

	struct FManagedCallbacks
	{
		using ManagedCallbacks_CreateNewManagedObject = GCHandleIntPtr(__stdcall*)(void*, void*);
		using ManagedCallbacks_InvokeManagedEvent = int(__stdcall*)(GCHandleIntPtr, void*, void*, void*, void*);
		using ManagedCallbacks_LookupMethod = void*(__stdcall*)(void*, const TCHAR*);
		using ManagedCallbacks_LookupType = uint8*(__stdcall*)(GCHandleIntPtr, const TCHAR*, const TCHAR*);
		using ManagedCallbacks_Dispose = void(__stdcall*)(GCHandleIntPtr);
		
		ManagedCallbacks_CreateNewManagedObject CreateNewManagedObject;
		ManagedCallbacks_InvokeManagedEvent InvokeManagedMethod;
		ManagedCallbacks_LookupMethod LookupManagedMethod;
		ManagedCallbacks_LookupType LookupManagedType;

	private:
		
		//Only call these from GCHandles.
		friend FGCHandle;
		ManagedCallbacks_Dispose Dispose;
		
	};
	
	static inline FManagedCallbacks ManagedCallbacks;
	
};






