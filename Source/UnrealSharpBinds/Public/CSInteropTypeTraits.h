#pragma once

#include <type_traits>

class UObjectBase;
class FField;

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

/**
 * Whether T may appear in a signature that crosses the native/managed boundary.
 *
 * - By value, T must be complete and trivially copyable: C++ passes non-trivially-copyable types (TArray, FString)
 *   through a hidden pointer, while .NET passes a blittable struct as a plain value. On SysV x86-64 and AArch64
 *   small structs travel in registers, so a mismatch corrupts arguments; Windows x64 often hides it.
 * - T must not be over-aligned (alignof > alignof(void*), e.g. FMatrix, FQuat, FTransform) by value, nor as a pointer
 *   or reference to such a value type: managed memory is only pointer-aligned and the compiler may use aligned SIMD
 *   accesses. Pass such values as void* and copy them with FMemory::Memcpy.
 * - Pointers to UObject and FField types are always allowed, since those objects are always natively allocated.
 * - Pointers to incomplete types are allowed: code that only sees the forward declaration cannot access the pointee.
 *   Completeness is decided at the first check in a translation unit, so include the full type before asserting.
 *
 * Necessary but not sufficient for MSVC, whose register vs. hidden-pointer return rules are stricter (x64: no
 * user-declared constructors for 8-byte returns; ARM64: only aggregates are HFAs).
 */
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

/** Fails the build if FunctionType passes or returns a type that is not TIsInteropSafeType. */
#define CS_ASSERT_INTEROP_SAFE_FUNCTION(FunctionType) \
	static_assert(TIsInteropFunctionPointer<FunctionType>::value, \
		#FunctionType " is not a plain function pointer type."); \
	static_assert(TIsInteropSafeFunction<FunctionType>::value, \
		#FunctionType " is not safe to call across the native/managed boundary (see CSInteropTypeTraits.h).")

/** Fails the build if Type may not be passed by value across the native/managed boundary. */
#define CS_ASSERT_INTEROP_SAFE_TYPE(Type) \
	static_assert(IsInteropSafeByValue<Type>(), \
		#Type " is not safe to pass by value across the native/managed boundary (see CSInteropTypeTraits.h).")
