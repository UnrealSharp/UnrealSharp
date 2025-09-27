#include "UScriptStructExporter.h"
#include "CSManager.h"
#include "TypeGenerator/CSScriptStruct.h"

int UUScriptStructExporter::GetNativeStructSize(const UScriptStruct* ScriptStruct)
{
	if (const UScriptStruct::ICppStructOps* CppStructOps = ScriptStruct->GetCppStructOps(); CppStructOps != nullptr)
	{
		return CppStructOps->GetSize();
	}
	
	return ScriptStruct->GetStructureSize();
}

bool UUScriptStructExporter::NativeCopy(const UScriptStruct* ScriptStruct, void* Src, void* Dest)
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

bool UUScriptStructExporter::NativeDestroy(const UScriptStruct* ScriptStruct, void* Struct)
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

void UUScriptStructExporter::AllocateNativeStruct(FNativeStructData& Data, const UScriptStruct* ScriptStruct)
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

void UUScriptStructExporter::DeallocateNativeStruct(FNativeStructData& Data, const UScriptStruct* ScriptStruct)
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

void* UUScriptStructExporter::GetStructLocation(FNativeStructData& Data, const UScriptStruct* ScriptStruct)
{
    if (const int32 NativeSize = GetNativeStructSize(ScriptStruct); NativeSize <= sizeof(FNativeStructData))
    {
        return std::addressof(Data.SmallStorage);
    }
    
    return Data.LargeStorage;
}

FGCHandleIntPtr UUScriptStructExporter::GetManagedStructType(UScriptStruct *ScriptStruct)
{
    if (const UCSScriptStruct* CSStruct = Cast<UCSScriptStruct>(ScriptStruct); CSStruct != nullptr)
    {
        return CSStruct->GetManagedTypeInfo<FCSManagedTypeInfo>()->GetManagedTypeHandle()->GetHandle();
    }

    const UCSAssembly* Assembly = UCSManager::Get().FindOwningAssembly(ScriptStruct);
    if (Assembly == nullptr)
    {
        return FGCHandleIntPtr();
    }

    const FCSFieldName FieldName(ScriptStruct);
    const TSharedPtr<FCSManagedTypeInfo> Info = Assembly->FindTypeInfo<FCSManagedTypeInfo>(FieldName);
    if (!Info.IsValid())
    {
        return FGCHandleIntPtr();
    }

    return Info->GetManagedTypeHandle()->GetHandle();   
}

