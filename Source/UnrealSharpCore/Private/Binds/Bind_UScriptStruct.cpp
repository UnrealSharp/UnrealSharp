#include "CSManager.h"
#include "Types/CSScriptStruct.h"

DECLARE_UNREALSHARP_BINDER(Bind_UScriptStruct)
{
	union FNativeStructData
	{
		std::array<std::byte, 64> SmallStorage;
		void* LargeStorage;
	};
	
	int GetNativeStructSize(const UScriptStruct* ScriptStruct)
	{
		if (const UScriptStruct::ICppStructOps* CppStructOps = ScriptStruct->GetCppStructOps(); CppStructOps != nullptr)
		{
			return CppStructOps->GetSize();
		}
		
		return ScriptStruct->GetStructureSize();
	}

	bool NativeCopy(const UScriptStruct* ScriptStruct, void* Src, void* Dest)
	{
		if (UScriptStruct::ICppStructOps* CppStructOps = ScriptStruct->GetCppStructOps(); CppStructOps != nullptr)
		{
			if (CppStructOps->HasCopy())
			{
				return CppStructOps->Copy(Dest, Src, 1);
			}
		    
	        FMemory::Memcpy(Dest, Src, CppStructOps->GetSize());
	        return true;
	    }
		
		return false;
	}

	bool NativeDestroy(const UScriptStruct* ScriptStruct, void* Struct)
	{
	    if (UScriptStruct::ICppStructOps* CppStructOps = ScriptStruct->GetCppStructOps(); CppStructOps != nullptr)
		{
			if (CppStructOps->HasDestructor())
			{
				CppStructOps->Destruct(Struct);
			}

	        return true;
		}
		
		return false;
	}

	void AllocateNativeStruct(FNativeStructData& Data, const UScriptStruct* ScriptStruct)
	{
	    if (const int32 NativeSize = GetNativeStructSize(ScriptStruct); NativeSize <= sizeof(FNativeStructData))
	    {
	        ScriptStruct->InitializeStruct(std::addressof(Data.SmallStorage));
	    }
	    else
	    {
	        Data.LargeStorage = FMemory::Malloc(NativeSize);
	        ScriptStruct->InitializeStruct(Data.LargeStorage);       
	    }
	}

	void DeallocateNativeStruct(FNativeStructData& Data, const UScriptStruct* ScriptStruct)
	{
	    if (const int32 NativeSize = GetNativeStructSize(ScriptStruct); NativeSize <= sizeof(FNativeStructData))
	    {
	        ScriptStruct->DestroyStruct(std::addressof(Data.SmallStorage));
	    }
	    else
	    {
	        ScriptStruct->DestroyStruct(Data.LargeStorage);    
	        FMemory::Free(Data.LargeStorage);   
	    }
	}

	void* GetStructLocation(FNativeStructData& Data, const UScriptStruct* ScriptStruct)
	{
	    if (const int32 NativeSize = GetNativeStructSize(ScriptStruct); NativeSize <= sizeof(FNativeStructData))
	    {
	        return std::addressof(Data.SmallStorage);
	    }
	    
	    return Data.LargeStorage;
	}

	FGCHandleIntPtr GetManagedStructType(UScriptStruct *ScriptStruct)
	{
	    if (const UCSScriptStruct* CSStruct = Cast<UCSScriptStruct>(ScriptStruct); CSStruct != nullptr)
	    {
	        return CSStruct->GetManagedTypeDefinition()->GetTypeGCHandle()->GetHandle();
	    }

	    const UCSManagedAssembly* Assembly = UCSManager::Get().FindOwningAssembly(ScriptStruct);
	    if (Assembly == nullptr)
	    {
	        return FGCHandleIntPtr();
	    }

	    const FCSFieldName FieldName(ScriptStruct);
	    const TSharedPtr<FCSManagedTypeDefinition> Info = Assembly->FindManagedTypeDefinition(FieldName);
	    if (!Info.IsValid())
	    {
	        return FGCHandleIntPtr();
	    }

	    return Info->GetTypeGCHandle()->GetHandle();   
	}

	BIND_UNREALSHARP_FUNCTION(GetNativeStructSize)
	BIND_UNREALSHARP_FUNCTION(NativeCopy)
	BIND_UNREALSHARP_FUNCTION(NativeDestroy)
	BIND_UNREALSHARP_FUNCTION(AllocateNativeStruct)
	BIND_UNREALSHARP_FUNCTION(DeallocateNativeStruct)
	BIND_UNREALSHARP_FUNCTION(GetStructLocation)
	BIND_UNREALSHARP_FUNCTION(GetManagedStructType)
}
