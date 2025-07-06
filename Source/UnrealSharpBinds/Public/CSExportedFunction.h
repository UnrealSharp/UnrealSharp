#pragma once

/**
 * Thin wrapper around sizeof(T) used for getting the size of a function's arguments.
 * @tparam T The type we want the size of
 */
template <typename T>
struct TArgSize
{
	constexpr static size_t Size = sizeof(T);
};

/**
 * Specialization for reference qualified types so we can get the size of the pointer instead of the object itself.
 * @tparam T The type we want the size of
 */
template <typename T>
struct TArgSize<T&>
{
	constexpr static size_t Size = sizeof(T*);
};

/**
 * Constant expression for the size of an argument
 * @tparam T The type we want the size of
 */
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

struct UNREALSHARPBINDS_API FCSExportedFunction
{
	FName Name;
	void* FunctionPointer;
	int32 Size;

	FCSExportedFunction(const FName& OuterName, const FName& Name, void* InFunctionPointer, int32 InSize);
};
