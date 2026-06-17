// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "Logging/StructuredLog.h"

class UCSUnrealSharpSettings;
template <typename T>
concept ValidProperty = std::derived_from<T, FProperty>
	&& std::constructible_from<T, FFieldVariant, FName, EObjectFlags>
	&& requires	{
		{ T::StaticClass() } -> std::same_as<FFieldClass*>;
	};

template <ValidProperty PropertyBaseClass>
class TCSGetterSetterProperty : public PropertyBaseClass {
public:
    TCSGetterSetterProperty(FFieldVariant InOwner, FName InName, EObjectFlags InFlags, UFunction* InSetterFunc, UFunction* InGetterFunc)
#if ENGINE_MINOR_VERSION >= 8
        : PropertyBaseClass(InOwner, InName)
#else
        : PropertyBaseClass(InOwner, InName, InFlags)
#endif
        , SetterFunc(MoveTemp(InSetterFunc)), GetterFunc(MoveTemp(InGetterFunc))
    {
    }
	
    // FProperty interface
    virtual bool HasSetter() const override { return !!SetterFunc; }
    virtual bool HasGetter() const override { return !!GetterFunc; }
    virtual bool HasSetterOrGetter() const override { return !!SetterFunc || !!GetterFunc; }
 
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

#if ENGINE_MINOR_VERSION >= 8
    virtual void ExportText_Internal(FString& ValueStr, TNotNull<const void*> PropertyValueOrContainer, EPropertyPointerType PointerType, const void* DefaultValue, UObject* Parent, int32 PortFlags, UObject* ExportRootScope) const override
    {
        if (PointerType == EPropertyPointerType::Direct)
        {
            const void* RawValueOrContainer = PropertyValueOrContainer;
            const uint8* ObjectPointer = static_cast<const uint8*>(RawValueOrContainer) - this->GetOffset_ForInternal();
            CallGetterInternal(const_cast<uint8*>(ObjectPointer), const_cast<void*>(RawValueOrContainer));
        }
        
        PropertyBaseClass::ExportText_Internal(ValueStr, PropertyValueOrContainer, PointerType, DefaultValue, Parent, PortFlags, ExportRootScope);
    }
#else
    virtual void ExportText_Internal(FString& ValueStr, const void* PropertyValueOrContainer, EPropertyPointerType PointerType, const void* DefaultValue, UObject* Parent, int32 PortFlags, UObject* ExportRootScope) const override
    {
        if (PointerType == EPropertyPointerType::Direct)
        {
            const uint8* ObjectPointer = static_cast<const uint8*>(PropertyValueOrContainer) - this->GetOffset_ForInternal();
            CallGetterInternal(const_cast<uint8*>(ObjectPointer), const_cast<void*>(PropertyValueOrContainer));
        }
        
        PropertyBaseClass::ExportText_Internal(ValueStr, PropertyValueOrContainer, PointerType, DefaultValue, Parent, PortFlags, ExportRootScope);
    }
#endif
    
#if ENGINE_MINOR_VERSION >= 8
    virtual const TCHAR* ImportText_Internal(const TCHAR* Buffer, TNotNull<void*> ContainerOrPropertyPtr, EPropertyPointerType PointerType, UObject* OwnerObject, int32 PortFlags, FOutputDevice* ErrorText) const override
    {
        const TCHAR* Result = PropertyBaseClass::ImportText_Internal(Buffer, ContainerOrPropertyPtr, PointerType, OwnerObject, PortFlags, ErrorText);

        // After setting the native memory from text we want to write the value into managed memory
        if (PointerType == EPropertyPointerType::Direct)
        {
            void* RawContainerOrPropertyPtr = ContainerOrPropertyPtr;
            uint8* ObjectPointer = static_cast<uint8*>(RawContainerOrPropertyPtr) - this->GetOffset_ForInternal();
            CallSetterInternal(ObjectPointer, RawContainerOrPropertyPtr);
        }
        
        return Result;
    }
#else
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
#endif

#if ENGINE_MINOR_VERSION <= 6
    virtual EConvertFromTypeResult ConvertFromType(const FPropertyTag& Tag, FStructuredArchive::FSlot Slot, uint8* Data, UStruct* DefaultsStruct, const uint8* Defaults)
    {
        // We need to call the setter to initialize the backing field from the serialized memory to ensure it gets initialized correctly
        const EConvertFromTypeResult Result = PropertyBaseClass::ConvertFromType(Tag, Slot, Data, DefaultsStruct, Defaults);
        CallSetterInternal(Data, Data + this->GetOffset_ForInternal());
        return Result;
    }
#endif

#if ENGINE_MINOR_VERSION >= 8
    virtual void SerializeItem(FStructuredArchive::FSlot Slot, TNotNull<void*> Value, void const* Defaults) const override
    {
        const FArchive& UnderlyingArchive = Slot.GetUnderlyingArchive();
        void* RawValue = Value;
        if (UnderlyingArchive.IsSaving())
        {
            // When saving we want to get the most up-to-date value of the property
            CallGetterInternal(static_cast<uint8*>(RawValue) - this->GetOffset_ForInternal(), RawValue);
        }

        PropertyBaseClass::SerializeItem(Slot, Value, Defaults);

        if (UnderlyingArchive.IsLoading())
        {
            // When loading we want to update the property with what we pulled out of the archive
            CallSetterInternal(static_cast<uint8*>(RawValue) - this->GetOffset_ForInternal(), RawValue);
        }
    }
#else
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
#endif

#if ENGINE_MINOR_VERSION >= 8
    virtual bool NetSerializeItem(FArchive& Ar, UPackageMap* Map, TNotNull<void*> Data, TArray<uint8>* MetaData) const override
    {
        void* RawData = Data;
        if (Ar.IsSaving())
        {
            // When saving we want to get the most up-to-date value of the property
            CallGetterInternal(static_cast<uint8*>(RawData) - this->GetOffset_ForInternal(), RawData);
        }

        const bool Result = PropertyBaseClass::NetSerializeItem(Ar, Map, Data, MetaData);

        if (Ar.IsLoading())
        {
            // When loading we want to update the property with what we pulled out of the archive
            CallSetterInternal(static_cast<uint8*>(RawData) - this->GetOffset_ForInternal(), RawData);
        }

        return Result;
    }
#else
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
#endif
    // End of FProperty interface

private:
    void CallSetterInternal(void* Container, const void* InValue) const
    {
        CallGetterSetterInternal(SetterFunc, Container, const_cast<void*>(InValue));
    }
    
    void CallGetterInternal(const void* Container, void* OutValue) const
    {
        CallGetterSetterInternal(GetterFunc, Container, OutValue);
    }
    
    void CallGetterSetterInternal(UFunction* Function, const void* Container, void* OutValue) const
    {
        if (!IsValid(Function))
        {
            UE_LOGFMT(LogUnrealSharp, Error, "Attempted to call invalid getter/setter function on property '{0}'", *this->GetName());
            return;
        }
        
#if WITH_EDITOR
        if (!Function->GetNativeFunc())
        {
            // Usually happens on skeleton classes. No big deal, just skip the call.
            return;
        }
#endif

        UObject* AsObject = static_cast<UObject*>(const_cast<void*>(Container));
        FFrame NewStack(AsObject, GetterFunc, OutValue, nullptr, GetterFunc->ChildProperties);
        
        Function->Invoke(AsObject, NewStack, OutValue);
    }
    
	UFunction* SetterFunc = nullptr;
	UFunction* GetterFunc = nullptr;
};