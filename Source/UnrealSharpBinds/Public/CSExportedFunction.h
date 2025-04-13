#pragma once

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
	FName Name;
	void* FunctionPointer;
	int32 Size;

	FCSExportedFunction(const FName& OuterName, const FName& Name, void* InFunctionPointer, int32 InSize);
};