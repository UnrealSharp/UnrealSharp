#pragma once

#include <type_traits>

class UObjectBase;
class FField;

// By-value types must be trivially copyable, and over-aligned types (FMatrix, FTransform, ...) may only cross
// the boundary as void*, since managed memory is only pointer-aligned. UObject/FField pointers are always fine.
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

#define CS_ASSERT_INTEROP_SAFE_FUNCTION(FunctionType) \
	static_assert(TIsInteropFunctionPointer<FunctionType>::value, \
		#FunctionType " is not a plain function pointer type."); \
	static_assert(TIsInteropSafeFunction<FunctionType>::value, \
		#FunctionType " is not safe to call across the native/managed boundary (see CSInteropTypeTraits.h).")

#define CS_ASSERT_INTEROP_SAFE_TYPE(Type) \
	static_assert(IsInteropSafeByValue<Type>(), \
		#Type " is not safe to pass by value across the native/managed boundary (see CSInteropTypeTraits.h).")
