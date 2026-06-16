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

struct UNREALSHARPBINDS_API FCSBoundFunction
{
	FCSBoundFunction(const FName& OuterName, const FName& Name, void* InFunctionPointer, int32 InParameterSize);
	
	FName Name;
	int32 ParameterSize;
	void* FunctionPointer;
};
