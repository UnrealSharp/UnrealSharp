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
        CallSetterInternal(Container, InValue);

        // After setting the value we make a call to the getter with the output destination being the native memory
        void* OutObj = this->template ContainerPtrToValuePtr<uint8>(static_cast<UObject*>(Container));
        if (GetterFunc != nullptr)
        {
            CallGetterInternal(Container, OutObj);
        }
        else
        {
            static_cast<const FProperty*>(this)->GetValue_InContainer(Container, OutObj);
        }
	    
    }

    virtual void CallGetter(const void* Container, void* OutValue) const override
    {
        CallGetterInternal(Container, OutValue);

        // After get invocation we want to write the result into the native buffer as well
        static_cast<const FProperty*>(this)->SetValue_InContainer(const_cast<void*>(Container), OutValue);
    }

    virtual void ExportText_Internal(FString& ValueStr, const void* PropertyValueOrContainer, EPropertyPointerType PointerType, const void* DefaultValue, UObject* Parent, int32 PortFlags, UObject* ExportRootScope) const override
    {
        // In the case of direct access we want to call C# to write to the native buffer before we proceed
        if (PointerType == EPropertyPointerType::Direct)
        {
            const uint8* ObjectPointer = static_cast<const uint8*>(PropertyValueOrContainer) - this->GetOffset_ForInternal();
            CallGetterInternal(const_cast<uint8*>(ObjectPointer), const_cast<void*>(PropertyValueOrContainer));
        }
        
        
        PropertyBaseClass::ExportText_Internal(ValueStr, PropertyValueOrContainer, PointerType,
            DefaultValue, Parent, PortFlags, ExportRootScope);
    }
    
    virtual const TCHAR* ImportText_Internal(const TCHAR* Buffer, void* ContainerOrPropertyPtr, EPropertyPointerType PointerType, UObject* OwnerObject, int32 PortFlags, FOutputDevice* ErrorText) const override
    {
        const TCHAR* Result = PropertyBaseClass::ImportText_Internal(Buffer, ContainerOrPropertyPtr, PointerType, OwnerObject, PortFlags, ErrorText);

        // After setting the native memory from text we want to write the value into managed memory
        if (PointerType == EPropertyPointerType::Direct)
        {
            uint8* ObjectPointer = static_cast<uint8*>(ContainerOrPropertyPtr) - this->GetOffset_ForInternal();
            CallSetterInternal(ObjectPointer, ContainerOrPropertyPtr);
        }
        
        return Result;
    }

    virtual EConvertFromTypeResult ConvertFromType(const FPropertyTag& Tag, FStructuredArchive::FSlot Slot, uint8* Data, UStruct* DefaultsStruct, const uint8* Defaults)
    {
        // We need to call the setter to initialize the backing field from the serialized memory to ensure it gets initialized correctly
        const EConvertFromTypeResult Result = PropertyBaseClass::ConvertFromType(Tag, Slot, Data, DefaultsStruct, Defaults);
        CallSetterInternal(Data, Data + this->GetOffset_ForInternal());
        return Result;
    }

    virtual void SerializeItem(FStructuredArchive::FSlot Slot, void* Value, void const* Defaults) const
    {
        const FArchive &UnderlyingArchive = Slot.GetUnderlyingArchive();
        if (UnderlyingArchive.IsSaving())
        {
            // When saving we want to get the most up-to-date value of the property
            CallGetterInternal(static_cast<uint8*>(Value) - this->GetOffset_ForInternal(), Value);
        }

        PropertyBaseClass::SerializeItem(Slot, Value, Defaults);
        
        if (UnderlyingArchive.IsLoading())
        {
            // When loading we want to update the property with what we pulled out of the archive
            CallSetterInternal(static_cast<uint8*>(Value) - this->GetOffset_ForInternal(), Value);
        }
    }
    
    virtual bool NetSerializeItem(FArchive& Ar, UPackageMap* Map, void* Data, TArray<uint8> * MetaData) const
    {
        if (Ar.IsSaving())
        {
            // When saving we want to get the most up-to-date value of the property
            CallGetterInternal(static_cast<uint8*>(Data) - this->GetOffset_ForInternal(), Data);
        }
        
        const bool Result = PropertyBaseClass::NetSerializeItem(Ar, Map, Data, MetaData);

        if (Ar.IsLoading())
        {
            // When loading we want to update the property with what we pulled out of the archive
            CallSetterInternal(static_cast<uint8*>(Data) - this->GetOffset_ForInternal(), Data);
        }

        return Result;
    }

private:
    void CallSetterInternal(void* Container, const void* InValue) const
    {
        checkf(SetterFunc, TEXT("Calling a setter on %s but the property has no setter defined."), *PropertyBaseClass::GetFullName());
        auto AsObject = static_cast<UObject*>(Container);
        FFrame NewStack(AsObject, SetterFunc, const_cast<void*>(InValue), nullptr, SetterFunc->ChildProperties);
        SetterFunc->Invoke(AsObject, NewStack, nullptr);
    }
    
    void CallGetterInternal(const void* Container, void* OutValue) const
    {
        checkf(GetterFunc, TEXT("Calling a getter on %s but the property has no getter defined."), *PropertyBaseClass::GetFullName());
        auto AsObject = static_cast<UObject*>(const_cast<void*>(Container));
        FFrame NewStack(AsObject, GetterFunc, OutValue, nullptr, GetterFunc->ChildProperties);
        GetterFunc->Invoke(AsObject, NewStack, OutValue);
    }
    
	UFunction* SetterFunc = nullptr;
	UFunction* GetterFunc = nullptr;
};