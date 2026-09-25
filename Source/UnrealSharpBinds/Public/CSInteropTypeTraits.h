#pragma once

#include <type_traits>

// Rules for types that cross the native/managed boundary by value.
//
// .NET treats a blittable managed struct as a plain value and passes it according to the platform C ABI.
// C++ does the same only for trivially copyable types: a class with a user-provided copy constructor or
// destructor (TArray, FString, TMap, ...) is always passed and returned through a hidden pointer.
//
// On Windows x64 every struct larger than 8 bytes is passed by hidden pointer anyway, so such a mismatch
// usually goes unnoticed. On SysV x86-64 (Linux, macOS) and AArch64, structs of up to 16 bytes are passed
// in registers, and the mismatch silently corrupts arguments.
//
// A type that crosses the boundary by value must therefore be trivially copyable. Pass anything else by
// pointer or reference, or through a trivially copyable view such as FCSUnmanagedArrayView.
template <typename T>
inline constexpr bool TIsInteropSafeType = std::is_void_v<T> || std::is_reference_v<T> || std::is_trivially_copyable_v<T>;

template <typename FunctionType>
struct TIsInteropSafeFunction : std::false_type
{
};

template <typename ReturnType, typename... ArgTypes>
struct TIsInteropSafeFunction<ReturnType (*)(ArgTypes...)>
	: std::bool_constant<TIsInteropSafeType<ReturnType> && (TIsInteropSafeType<ArgTypes> && ...)>
{
};

// Fails the build if a function pointer type that is called across the boundary passes or returns
// a non-trivially-copyable type by value.
#define CS_ASSERT_INTEROP_SAFE_FUNCTION(FunctionType) \
	static_assert(TIsInteropSafeFunction<FunctionType>::value, \
		#FunctionType " passes or returns a non-trivially-copyable type by value across the native/managed boundary. " \
		"Pass it by pointer or reference instead (see CSInteropTypeTraits.h).")

// Fails the build if a struct that is shared with managed code by value is not trivially copyable.
#define CS_ASSERT_INTEROP_SAFE_TYPE(Type) \
	static_assert(std::is_trivially_copyable_v<Type>, \
		#Type " crosses the native/managed boundary by value and must be trivially copyable (see CSInteropTypeTraits.h).")
