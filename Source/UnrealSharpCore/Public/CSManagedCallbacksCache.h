#pragma once

#if !defined(_WIN32)
#define __stdcall
#endif

struct FScopedGCHandle;
struct FInvokeManagedMethodData;
struct FGCHandleIntPtr;
struct FGCHandle;

struct FCSManagedCallbacks
{
	using ManagedCallbacks_CreateNewManagedObject = FGCHandleIntPtr(__stdcall*)(const void*, void*, TCHAR**);
	using ManagedCallbacks_CreateNewManagedObjectWrapper = FGCHandleIntPtr(__stdcall*)(void*, void*);
	using ManagedCallbacks_InvokeManagedMethod = int(__stdcall*)(void*, void*, void*, void*, void*);
	using ManagedCallbacks_InvokeDelegate = int(__stdcall*)(FGCHandleIntPtr);
	using ManagedCallbacks_GetManagedMethod = uint8*(__stdcall*)(void*, const TCHAR*);
	using ManagedCallbacks_GetManagedTypeHandle = uint8*(__stdcall*)(uint8*, const TCHAR*);
	using ManagedCallbacks_InitializeStructure = void(__stdcall*)(FGCHandleIntPtr, void*);
	using ManagedCallbacks_Dispose = void(__stdcall*)(FGCHandleIntPtr, FGCHandleIntPtr);
	using ManagedCallbacks_FreeHandle = void(__stdcall*)(FGCHandleIntPtr);
		
	ManagedCallbacks_CreateNewManagedObject CreateNewManagedObject;
	ManagedCallbacks_CreateNewManagedObjectWrapper CreateNewManagedObjectWrapper;
		
	ManagedCallbacks_InvokeManagedMethod InvokeManagedMethod;
		
	ManagedCallbacks_InvokeDelegate InvokeDelegate;
	ManagedCallbacks_GetManagedMethod GetManagedMethod;
	ManagedCallbacks_GetManagedTypeHandle GetManagedTypeHandle;
	
	ManagedCallbacks_InitializeStructure InitializeStructure;
	
private:
	friend FGCHandle;
	friend FScopedGCHandle;
	
	ManagedCallbacks_Dispose Dispose;
	ManagedCallbacks_FreeHandle FreeHandle;
};

inline FCSManagedCallbacks& GetManagedCallbacks()
{
	static FCSManagedCallbacks Instance;
	return Instance;
}






