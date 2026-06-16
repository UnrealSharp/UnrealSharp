#pragma once

template <typename T>
struct TArgSize
{
	constexpr static size_t Size = sizeof(T);
};

template <typename T>
struct TArgSize<T&>
{
	constexpr static size_t Size = sizeof(T*);
};

template <typename T>
constexpr size_t ArgSize = TArgSize<T>::Size;

template <typename ReturnType, typename... Args>
constexpr size_t GetFunctionSize(ReturnType (*)(Args...))
{
	if constexpr (std::is_void_v<ReturnType>)
	{
		return (ArgSize<Args> + ... + 0);
	}
	else
	{
		return ArgSize<ReturnType> + (ArgSize<Args> + ... + 0);
	}
}

struct FCSBoundFunction
{
	FName FunctionName;
	int32 ParameterSize;
	void* FunctionPointer;
};

#define DECLARE_UNREALSHARP_BINDER(Name) \
namespace Name { static const FName UnrealSharpBinderName(#Name); } \
namespace Name

#define BIND_UNREALSHARP_FUNCTION(FunctionName) \
static const FCSBoundFunction ANONYMOUS_VARIABLE(ZUnrealSharpBind_) = FCSBindsRegistry::RegisterBoundFunction( \
UnrealSharpBinderName, \
FName(#FunctionName), \
(void*)&FunctionName, \
static_cast<int32>(GetFunctionSize(&FunctionName)));

class FCSBindsRegistry
{
public:
	UNREALSHARPBINDS_API static const FCSBoundFunction& RegisterBoundFunction(const FName& BinderName, const FName& FunctionName, void* FunctionPointer, int32 ParameterSize);
	UNREALSHARPBINDS_API static void* GetBoundFunction(const TCHAR* BinderName, const TCHAR* FunctionName, int32 ParameterSize);
	UNREALSHARPBINDS_API static const TMap<FName, TArray<FCSBoundFunction>>& GetBinderToFunctionsMap() { return BinderToFunctionsMap; }
private:
	static TMap<FName, TArray<FCSBoundFunction>> BinderToFunctionsMap;
};
