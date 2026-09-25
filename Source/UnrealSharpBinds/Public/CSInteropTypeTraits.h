#pragma once

#include <type_traits>

class UObjectBase;
class FField;

// Rules for types that cross the native/managed boundary.
//
// .NET treats a blittable managed struct as a plain value and passes it according to the platform C ABI.
// C++ does the same only for trivially copyable types: a class with a user-provided copy constructor or
// destructor (TArray, FString, TMap, ...) is always passed and returned through a hidden pointer.
//
// On Windows x64 every struct larger than 8 bytes is passed by hidden pointer anyway, so such a mismatch
// usually goes unnoticed. On SysV x86-64 (Linux, macOS) and AArch64, structs of up to 16 bytes are passed
// in registers, and the mismatch silently corrupts arguments.
//
// 1. A type that crosses the boundary by value must be complete and trivially copyable. Pass anything else by
//    pointer or reference, or through a trivially copyable view such as FCSUnmanagedArrayView.
//
// 2. Managed memory is only guaranteed to be pointer-aligned. An over-aligned type such as FMatrix, FQuat or
//    FTransform (alignas(16)) must not be passed by value, and not by pointer or reference either when the
//    pointee may live in managed memory: the compiler may use aligned SIMD loads/stores on it, which fault on
//    an 8-byte-aligned managed local. Pass such values as void* and copy them with FMemory::Memcpy.
//    UObjects and FFields are always allocated natively, so pointers to them are fine whatever their alignment
//    (e.g. USceneComponent, which embeds an FTransform).
//    A pointer to an incomplete type is accepted: code that only sees the forward declaration cannot access
//    the pointee, aligned or not.
//
// These checks are necessary but not sufficient for MSVC, which decides register vs. hidden-pointer returns with
// stricter rules than "trivially copyable" (x64: no user-declared constructors for 8-byte returns; ARM64: only
// aggregates are HFAs). The by-value returns in use today (FGCHandleIntPtr, an 8-byte aggregate; FVector, 24
// bytes and returned through memory on Win x64) are unaffected.
namespace UnrealSharp::Interop::Private
{
	template <typename T, typename = void>
	struct TIsComplete : std::false_type
	{
	};

	template <typename T>
	struct TIsComplete<T, std::void_t<decltype(sizeof(T))>> : std::true_type
	{
	};

	template <typename T>
	constexpr bool IsOverAligned()
	{
		if constexpr (TIsComplete<T>::value)
		{
			return alignof(T) > alignof(void*);
		}
		else
		{
			return false;
		}
	}

	template <typename T>
	constexpr bool IsAlwaysNativeAllocated()
	{
		if constexpr (TIsComplete<T>::value && std::is_class_v<T>)
		{
			return std::is_base_of_v<UObjectBase, T> || std::is_base_of_v<FField, T>;
		}
		else
		{
			return false;
		}
	}
}

template <typename T>
constexpr bool IsInteropSafeByValue()
{
	using namespace UnrealSharp::Interop::Private;

	if constexpr (!TIsComplete<T>::value)
	{
		return false;
	}
	else
	{
		return std::is_trivially_copyable_v<T> && !IsOverAligned<T>();
	}
}

template <typename T>
constexpr bool IsInteropSafeType()
{
	using namespace UnrealSharp::Interop::Private;

	if constexpr (std::is_void_v<T>)
	{
		return true;
	}
	else if constexpr (std::is_reference_v<T> || std::is_pointer_v<T>)
	{
		using FPointee = std::remove_cv_t<std::remove_pointer_t<std::remove_reference_t<T>>>;
		return !std::is_object_v<FPointee> || !IsOverAligned<FPointee>() || IsAlwaysNativeAllocated<FPointee>();
	}
	else
	{
		return IsInteropSafeByValue<T>();
	}
}

template <typename T>
inline constexpr bool TIsInteropSafeType = IsInteropSafeType<T>();

template <typename FunctionType>
struct TIsInteropFunctionPointer : std::false_type
{
};

template <typename ReturnType, typename... ArgTypes>
struct TIsInteropFunctionPointer<ReturnType (*)(ArgTypes...)> : std::true_type
{
};

template <typename ReturnType, typename... ArgTypes>
struct TIsInteropFunctionPointer<ReturnType (*)(ArgTypes...) noexcept> : std::true_type
{
};

template <typename FunctionType>
struct TIsInteropSafeFunction : std::false_type
{
};

template <typename ReturnType, typename... ArgTypes>
struct TIsInteropSafeFunction<ReturnType (*)(ArgTypes...)>
	: std::bool_constant<TIsInteropSafeType<ReturnType> && (TIsInteropSafeType<ArgTypes> && ...)>
{
};

template <typename ReturnType, typename... ArgTypes>
struct TIsInteropSafeFunction<ReturnType (*)(ArgTypes...) noexcept> : TIsInteropSafeFunction<ReturnType (*)(ArgTypes...)>
{
};

// Fails the build if a function pointer type that is called across the boundary breaks the rules above.
#define CS_ASSERT_INTEROP_SAFE_FUNCTION(FunctionType) \
	static_assert(TIsInteropFunctionPointer<FunctionType>::value, \
		#FunctionType " is not a plain function pointer type."); \
	static_assert(TIsInteropSafeFunction<FunctionType>::value, \
		#FunctionType " passes an incomplete or non-trivially-copyable type by value, or an over-aligned type that may " \
		"live in managed memory, across the native/managed boundary (see CSInteropTypeTraits.h).")

// Fails the build if a struct that is shared with managed code by value breaks the rules above.
#define CS_ASSERT_INTEROP_SAFE_TYPE(Type) \
	static_assert(IsInteropSafeByValue<Type>(), \
		#Type " crosses the native/managed boundary by value and must be complete, trivially copyable and at most " \
		"pointer-aligned (see CSInteropTypeTraits.h).")
