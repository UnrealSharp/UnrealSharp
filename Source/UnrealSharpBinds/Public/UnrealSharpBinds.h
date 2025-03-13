#pragma once

#include "CoreMinimal.h"
#include "Modules/ModuleManager.h"

#define UNREALSHARP_FUNCTION()

template <typename ReturnType, typename... Args>
constexpr size_t GetFunctionSize(ReturnType (*)(Args...))
{
    if constexpr (std::is_void_v<ReturnType>)
    {
        return (sizeof(Args) + ... + 0);
    }
    else
    {
        return sizeof(ReturnType) + (sizeof(Args) + ... + 0);
    }
}

struct UNREALSHARPBINDS_API FCSExportedFunction
{
    FName FunctionName;
    void* FunctionPointer;
    int32 Size;

    FCSExportedFunction(const FName& OuterName, const FName& FunctionName, void* InFunctionPointer, int32 InSize);
};

class FUnrealSharpBindsModule : public IModuleInterface
{
public:
    
    // IModuleInterface interface
    virtual void StartupModule() override;
    virtual void ShutdownModule() override;
    // End of IModuleInterface interface
};
