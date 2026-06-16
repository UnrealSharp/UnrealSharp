#include "CSBindsRegistry.h"
#include "INotifyFieldValueChanged.h"

DECLARE_UNREALSHARP_BINDER(Bind_FProperty)
{
	FProperty* GetNativePropertyFromName(UStruct* Struct, const char* PropertyName)
	{
		FProperty* Property = FindFProperty<FProperty>(Struct, PropertyName);
		return Property;
	}

	int32 GetPropertyOffset(FProperty* Property)
	{
		return Property->GetOffset_ForInternal();
	}

	int32 GetSize(FProperty* Property)
	{
		return Property->GetSize();
	}

	int32 GetArrayDim(FProperty* Property)
	{
		return Property->ArrayDim;
	}

	void DestroyValue(FProperty* Property, void* Value)
	{
		Property->DestroyValue(Value);
	}

	void DestroyValue_InContainer(FProperty* Property, void* Value)
	{
		Property->DestroyValue_InContainer(Value);
	}

	void InitializeValue(FProperty* Property, void* Value)
	{
		Property->InitializeValue(Value);
	}

	bool Identical(const FProperty* Property, void* ValueA, void* ValueB)
	{
		bool bIsIdentical = Property->Identical(ValueA, ValueB);
		return bIsIdentical;
	}

	void GetInnerFields(FProperty* SetProperty, TArray<FField*>* OutFields)
	{
		SetProperty->GetInnerFields(*OutFields);
	}

	uint32 GetValueTypeHash(FProperty* Property, void* Source)
	{
		return Property->GetValueTypeHash(Source);
	}

	bool HasAnyPropertyFlags(FProperty* Property, EPropertyFlags FlagsToCheck)
	{
		return Property->HasAnyPropertyFlags(FlagsToCheck);
	}

	bool HasAllPropertyFlags(FProperty* Property, EPropertyFlags FlagsToCheck)
	{
		return Property->HasAllPropertyFlags(FlagsToCheck);
	}

	void CopySingleValue(FProperty* Property, void* Dest, void* Src)
	{
		Property->CopySingleValue(Dest, Src);
	}

	void GetValue_InContainer(FProperty* Property, void* Container, void* OutValue)
	{
		Property->GetValue_InContainer(Container, OutValue);
	}

	void SetValue_InContainer(FProperty* Property, void* Container, void* Value)
	{
		Property->SetValue_InContainer(Container, Value);
	}

	uint8 GetBoolPropertyFieldMaskFromName(UStruct* InStruct, const char* InPropertyName)
	{
		FBoolProperty* Property = FindFProperty<FBoolProperty>(InStruct, InPropertyName);
		if (!Property)
		{
			return 0;
		}

		return Property->GetFieldMask();
	}

	int32 GetPropertyOffsetFromName(UStruct* InStruct, const char* InPropertyName)
	{
		FProperty* FoundProperty = GetNativePropertyFromName(InStruct, InPropertyName);
		if (!FoundProperty)
		{
			return -1;
		}
		
		return GetPropertyOffset(FoundProperty);
	}

	int32 GetPropertyArrayDimFromName(UStruct* InStruct, const char* PropertyName)
	{
		FProperty* Property = GetNativePropertyFromName(InStruct, PropertyName);
		return GetArrayDim(Property);
	}

	void BroadcastFieldValueChanged(UObject* Object, FProperty* Property)
	{
		TScriptInterface<INotifyFieldValueChanged> NotifyFieldSelf = Object;
		FName FieldName = Property->GetFName();
		if (NotifyFieldSelf.GetObject() != nullptr && NotifyFieldSelf.GetInterface() != nullptr && FieldName.IsValid())
		{
			const UE::FieldNotification::FFieldId FieldId = NotifyFieldSelf->GetFieldNotificationDescriptor().GetField(Object->GetClass(), FieldName);
			if (FieldId.IsValid())
			{
				NotifyFieldSelf->BroadcastFieldValueChanged(FieldId);
			}
		}
	}
	
	BIND_UNREALSHARP_FUNCTION(GetNativePropertyFromName)
	BIND_UNREALSHARP_FUNCTION(GetPropertyOffset)
	BIND_UNREALSHARP_FUNCTION(GetSize)
	BIND_UNREALSHARP_FUNCTION(GetArrayDim)
	BIND_UNREALSHARP_FUNCTION(DestroyValue)
	BIND_UNREALSHARP_FUNCTION(DestroyValue_InContainer)
	BIND_UNREALSHARP_FUNCTION(InitializeValue)
	BIND_UNREALSHARP_FUNCTION(Identical)
	BIND_UNREALSHARP_FUNCTION(GetInnerFields)
	BIND_UNREALSHARP_FUNCTION(GetValueTypeHash)
	BIND_UNREALSHARP_FUNCTION(HasAnyPropertyFlags)
	BIND_UNREALSHARP_FUNCTION(HasAllPropertyFlags)
	BIND_UNREALSHARP_FUNCTION(CopySingleValue)
	BIND_UNREALSHARP_FUNCTION(GetValue_InContainer)
	BIND_UNREALSHARP_FUNCTION(SetValue_InContainer)
	BIND_UNREALSHARP_FUNCTION(GetBoolPropertyFieldMaskFromName)
	BIND_UNREALSHARP_FUNCTION(GetPropertyOffsetFromName)
	BIND_UNREALSHARP_FUNCTION(GetPropertyArrayDimFromName)
	BIND_UNREALSHARP_FUNCTION(BroadcastFieldValueChanged)
}