#include "MapPropertyTranslator.h"
#include "GlueGenerator/CSScriptBuilder.h"

bool FMapPropertyTranslator::CanHandleProperty(const FProperty* Property) const
{
	const FMapProperty* MapProperty = CastField<FMapProperty>(Property);

	if (!MapProperty)
	{
		return false;
	}
	
	const FPropertyTranslator& KeyHandler = PropertyHandlers.Find(MapProperty->KeyProp);
	const FPropertyTranslator& ValueHandler = PropertyHandlers.Find(MapProperty->ValueProp);
	
	return KeyHandler.IsSupportedAsInner() && ValueHandler.IsSupportedAsInner();
}

void FMapPropertyTranslator::AddReferences(const FProperty* Property, TSet<UField*>& References) const
{
	const FMapProperty* MapProperty = CastField<FMapProperty>(Property);
	
	const FPropertyTranslator& KeyHandler = PropertyHandlers.Find(MapProperty->KeyProp);
	const FPropertyTranslator& Handler = PropertyHandlers.Find(MapProperty->ValueProp);

	KeyHandler.AddReferences(MapProperty->KeyProp, References);
	Handler.AddReferences(MapProperty->ValueProp, References);
}

FString FMapPropertyTranslator::GetManagedType(const FProperty* Property) const
{
	const FMapProperty* MapProperty = CastField<FMapProperty>(Property);

	const FPropertyTranslator& KeyHandler = PropertyHandlers.Find(MapProperty->GetKeyProperty());
	const FPropertyTranslator& Handler = PropertyHandlers.Find(MapProperty->GetValueProperty());

	FString KeyCSharpType = KeyHandler.GetManagedType(MapProperty->GetKeyProperty());
	FString ValueCSharpType = Handler.GetManagedType(MapProperty->GetValueProperty());
	
	return FString::Printf(TEXT("System.Collections.Generic.%s<%s, %s>"),
		Property->HasAnyPropertyFlags(CPF_BlueprintReadOnly)
		? TEXT("IReadOnlyDictionary") : TEXT("IDictionary"), *KeyCSharpType, *ValueCSharpType);
}

void FMapPropertyTranslator::ExportPropertyStaticConstruction(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName) const
{
	FPropertyTranslator::ExportPropertyStaticConstruction(Builder, Property, NativePropertyName);
	MakeGetNativePropertyFromName(Builder, NativePropertyName);
}

void FMapPropertyTranslator::ExportParameterStaticConstruction(FCSScriptBuilder& Builder, const FString& NativeMethodName, const FProperty* Parameter) const
{
	FPropertyTranslator::ExportParameterStaticConstruction(Builder, NativeMethodName, Parameter);
	const FString ParamName = Parameter->GetName();
	Builder.AppendLine(FString::Printf(TEXT("%s_%s_NativeProperty = %s.CallGetNativePropertyFromName(%s_NativeFunction, \"%s\");"), *NativeMethodName, *ParamName, FPropertyCallbacks, *NativeMethodName, *ParamName));
}

FString FMapPropertyTranslator::ExportInstanceMarshallerVariables(const FProperty* Property, const FString& PropertyName) const
{
	return FPropertyTranslator::ExportInstanceMarshallerVariables(Property, PropertyName);
}

FString FMapPropertyTranslator::ExportMarshallerDelegates(const FProperty* Property, const FString& PropertyName) const
{
	return "";
}

void FMapPropertyTranslator::ExportPropertyGetter(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName) const
{
	const FMapProperty* MapProperty = CastFieldChecked<FMapProperty>(Property);
	const FProperty* KeyProperty = MapProperty->KeyProp;
	const FProperty* ValueProperty = MapProperty->ValueProp;
	
	const FPropertyTranslator& KeyHandler = PropertyHandlers.Find(KeyProperty);
	const FPropertyTranslator& ValueHandler = PropertyHandlers.Find(ValueProperty);

	FString Marshaller;
	GetMarshaller(CastFieldChecked<FMapProperty>(Property), Marshaller);

	Builder.AppendLine(FString::Printf(TEXT("%s_Marshaller ??= new %s(1, %s_NativeProperty, %s, %s);"),
		*NativePropertyName,
		*Marshaller,
		*NativePropertyName,
		*KeyHandler.ExportMarshallerDelegates(KeyProperty, NativePropertyName),
		*ValueHandler.ExportMarshallerDelegates(ValueProperty, NativePropertyName)));

	Builder.AppendLine();
	Builder.AppendLine(FString::Printf(TEXT("return %s_Marshaller.FromNative(IntPtr.Add(NativeObject,%s_Offset),0);"),
		*NativePropertyName,
		*NativePropertyName));
}

void FMapPropertyTranslator::ExportMarshalToNativeBuffer(FCSScriptBuilder& Builder, const FProperty* Property,
	const FString& PropertyName, const FString& DestinationBuffer, const FString& Offset, const FString& Source) const
{
	const FMapProperty* MapProperty = CastFieldChecked<FMapProperty>(Property);
	const FProperty* KeyProperty = MapProperty->KeyProp;
	const FProperty* ValueProperty = MapProperty->ValueProp;
	
	const FPropertyTranslator& KeyHandler = PropertyHandlers.Find(KeyProperty);
	const FPropertyTranslator& ValueHandler = PropertyHandlers.Find(ValueProperty);

	FString NativeProperty = PropertyName + "_NativeProperty";
	FString Marshaller = PropertyName + "_Marshaller";
	
	if (UFunction* Function = Property->GetOwner<UFunction>())
	{
		FString NativeFunctionName = Function->GetName();
		NativeProperty = NativeFunctionName + "_" + NativeProperty;
		Marshaller = NativeFunctionName + "_" + Marshaller;
	}

	FString KeyType = KeyHandler.GetManagedType(KeyProperty);
	FString ValueType = ValueHandler.GetManagedType(ValueProperty);
	
	const FString MarshallerType = FString::Printf(TEXT("MapCopyMarshaller<%s, %s>"), *KeyType, *ValueType);

	FString KeyMarshaller = KeyHandler.ExportMarshallerDelegates(KeyProperty, PropertyName);
	FString ValueMarshaller = ValueHandler.ExportMarshallerDelegates(ValueProperty, PropertyName);

	Builder.AppendLine(FString::Printf(TEXT("%s ??= new %s(%s, %s, %s);"), *Marshaller, *MarshallerType, *NativeProperty, *KeyMarshaller, *ValueMarshaller));

	//Native buffer variable used in cleanup
	Builder.AppendLine(FString::Printf(TEXT("IntPtr %s_NativeBuffer = IntPtr.Add(%s, %s);"), *PropertyName, *DestinationBuffer, *Offset));
	Builder.AppendLine(FString::Printf(TEXT("%s.ToNative(%s_NativeBuffer, 0, %s);"), *Marshaller, *PropertyName, *Source));
}

void FMapPropertyTranslator::ExportMarshalFromNativeBuffer(FCSScriptBuilder& Builder, const FProperty* Property,
	const FString& NativePropertyName, const FString& AssignmentOrReturn, const FString& SourceBuffer, const FString& Offset,
	bool bCleanupSourceBuffer, bool reuseRefMarshallers) const
{
	const FMapProperty* ArrayProperty = CastFieldChecked<FMapProperty>(Property);
	const FProperty* KeyProperty = ArrayProperty->KeyProp;
	const FProperty* ValueProperty = ArrayProperty->ValueProp;
	
	const FPropertyTranslator& KeyHandler = PropertyHandlers.Find(KeyProperty);
	const FPropertyTranslator& ValueHandler = PropertyHandlers.Find(ValueProperty);

	FString NativeProperty = NativePropertyName + "_NativeProperty";
	FString Marshaller = NativePropertyName + "_Marshaller";
	if (UFunction* Function = Property->GetOwner<UFunction>())
	{
		FString NativeFunctionName = Function->GetName();
		NativeProperty = NativeFunctionName + "_" + NativeProperty;
		Marshaller = NativeFunctionName + "_" + Marshaller;
	}

	const FString KeyType = KeyHandler.GetManagedType(KeyProperty);
	const FString ValueType = ValueHandler.GetManagedType(ValueProperty);
	const FString MarshallerType = FString::Printf(TEXT("MapCopyMarshaller<%s, %s>"), *KeyType, *ValueType);

	if (!reuseRefMarshallers)
	{
		FString KeyMarshaller = KeyHandler.ExportMarshallerDelegates(KeyProperty, NativePropertyName);
		FString ValueMarshaller = ValueHandler.ExportMarshallerDelegates(ValueProperty, NativePropertyName);
		
		Builder.AppendLine(FString::Printf(TEXT("%s ??= new %s(%s, %s, %s);"), *Marshaller, *MarshallerType, *NativeProperty, *KeyMarshaller, *ValueMarshaller));

		//Native buffer variable used in cleanup
		Builder.AppendLine(FString::Printf(TEXT("IntPtr %s_NativeBuffer = IntPtr.Add(%s, %s);"), *NativePropertyName, *SourceBuffer, *Offset));
	}

	Builder.AppendLine(FString::Printf(TEXT("%s %s.FromNative(%s_NativeBuffer, 0);"), *AssignmentOrReturn, *Marshaller, *NativePropertyName));

	if (bCleanupSourceBuffer)
	{
		Builder.AppendLine(FString::Printf(TEXT("%s.DestructInstance(%s_NativeBuffer, 0);"), *Marshaller, *NativePropertyName));
	}
}

void FMapPropertyTranslator::ExportCleanupMarshallingBuffer(FCSScriptBuilder& Builder, const FProperty* ParamProperty,
	const FString& ParamName) const
{

}

void FMapPropertyTranslator::ExportParameterVariables(FCSScriptBuilder& Builder, UFunction* Function, const FString& NativeMethodName, FProperty* ParamProperty, const FString& NativePropertyName) const
{
	FPropertyTranslator::ExportParameterVariables(Builder, Function, NativeMethodName, ParamProperty, NativePropertyName);
	
	FString Marshaller;
	GetMarshaller(CastFieldChecked<FMapProperty>(ParamProperty), Marshaller);
	
	Builder.AppendLine(FString::Printf(TEXT("static IntPtr %s_%s_NativeProperty;"), *NativeMethodName, *NativePropertyName));
	if (Function->HasAnyFunctionFlags(FUNC_Static))
	{
		Builder.AppendLine(FString::Printf(TEXT("static %s %s_%s_Marshaller = null;"), *Marshaller, *NativeMethodName, *NativePropertyName));
	}
	else
	{
		Builder.AppendLine(FString::Printf(TEXT("%s %s_%s_Marshaller = null;"), *Marshaller, *NativeMethodName, *NativePropertyName));
	}
}

void FMapPropertyTranslator::ExportPropertyVariables(FCSScriptBuilder& Builder, const FProperty* Property, const FString& PropertyName) const
{
	FPropertyTranslator::ExportPropertyVariables(Builder, Property, PropertyName);
	MakeNativePropertyField(Builder, PropertyName);

	FString Marshaller;
	GetMarshaller(CastFieldChecked<FMapProperty>(Property), Marshaller);
	
	if (IsOwnedBy<UScriptStruct>(Property))
	{
		Builder.AppendLine(FString::Printf(TEXT("static %s %s_Marshaller = null;"), *Marshaller, *PropertyName));
	}
	else
	{
		Builder.AppendLine(FString::Printf(TEXT("%s %s_Marshaller = null;"), *Marshaller, *PropertyName));
	}
}

FString FMapPropertyTranslator::GetNullReturnCSharpValue(const FProperty* ReturnProperty) const
{
	return TEXT("null");
}

void FMapPropertyTranslator::GetMarshaller(const FMapProperty* Property, FString& Marshaller) const
{
	bool IsStructProperty = Property->GetOwnerStruct()->IsA<UScriptStruct>();
	bool IsParamProperty = Property->HasAnyPropertyFlags(CPF_Parm);

	const FProperty* KeyProperty = Property->KeyProp;
	const FProperty* ValueProperty = Property->ValueProp;
	
	const FPropertyTranslator& ValueHandler = PropertyHandlers.Find(ValueProperty);
	const FPropertyTranslator& KeyHandler = PropertyHandlers.Find(KeyProperty);
	
	FString MarshallerType = IsStructProperty || IsParamProperty
	? TEXT("MapCopyMarshaller") : Property->HasAnyPropertyFlags(CPF_BlueprintReadOnly) ? TEXT("MapReadOnlyMarshaller") : TEXT("MapMarshaller");

	FString KeyType = KeyHandler.GetManagedType(KeyProperty);
	FString ValueType = ValueHandler.GetManagedType(ValueProperty);

	Marshaller = FString::Printf(TEXT("%s<%s, %s>"), *MarshallerType, *KeyType, *ValueType);
}

