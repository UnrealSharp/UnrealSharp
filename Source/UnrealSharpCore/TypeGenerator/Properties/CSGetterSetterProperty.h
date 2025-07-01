// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"

class UCSUnrealSharpSettings;
template <typename T>
concept ValidProperty = std::derived_from<T, FProperty>
	&& std::constructible_from<T, FFieldVariant, FName, EObjectFlags>
	&& requires	{
		{ T::StaticClass() } -> std::same_as<FFieldClass*>;
	};

/**
 * 
 */
template <ValidProperty PropertyBaseClass>
class  TCSGetterSetterProperty : public PropertyBaseClass {
public:
	TCSGetterSetterProperty(FFieldVariant InOwner, FName InName, EObjectFlags InFlags, UFunction* InSetterFunc, UFunction* InGetterFunc) : PropertyBaseClass(InOwner, InName, InFlags), SetterFunc(MoveTemp(InSetterFunc)), GetterFunc(MoveTemp(InGetterFunc)) {
	}
	
	virtual bool HasSetter() const override
	{
		return !!SetterFunc;
	}

	virtual bool HasGetter() const override
	{
		return !!GetterFunc;
	}

	virtual bool HasSetterOrGetter() const override
	{
		return !!SetterFunc || !!GetterFunc;
	}

	virtual void CallSetter(void* Container, const void* InValue) const override
	{
		checkf(SetterFunc, TEXT("Calling a setter on %s but the property has no setter defined."), *PropertyBaseClass::GetFullName());
		auto AsObject = static_cast<UObject*>(Container);
		FFrame NewStack(AsObject, SetterFunc, const_cast<void*>(InValue), nullptr, SetterFunc->ChildProperties);
		SetterFunc->Invoke(AsObject, NewStack, nullptr);
	}

	virtual void CallGetter(const void* Container, void* OutValue) const override
	{
		checkf(GetterFunc, TEXT("Calling a getter on %s but the property has no getter defined."), *PropertyBaseClass::GetFullName());
		auto AsObject = static_cast<UObject*>(const_cast<void*>(Container));
		FFrame NewStack(AsObject, GetterFunc, OutValue, nullptr, GetterFunc->ChildProperties);
		GetterFunc->Invoke(AsObject, NewStack, OutValue);
	}

private:
	UFunction* SetterFunc = nullptr;
	UFunction* GetterFunc = nullptr;
};