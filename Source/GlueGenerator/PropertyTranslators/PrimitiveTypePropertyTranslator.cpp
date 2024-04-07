#include "PrimitiveTypePropertyTranslator.h"
#include "GlueGenerator/GlueGeneratorModule.h"
#include "GlueGenerator/CSScriptBuilder.h"

FSimpleTypePropertyTranslator::FSimpleTypePropertyTranslator
(FCSSupportedPropertyTranslators& InPropertyHandlers, FFieldClass* InPropertyClass, const FString& InManagedType, const FString& InMarshallerType, EPropertyUsage InPropertyUsage)
: FPropertyTranslator(InPropertyHandlers, InPropertyUsage), PropertyClass(InPropertyClass), ManagedType(InManagedType), MarshallerType(InMarshallerType)
{

}

bool FSimpleTypePropertyTranslator::CanHandleProperty(const FProperty* Property) const
{
	return Property->IsA(PropertyClass);
}

FString FSimpleTypePropertyTranslator::GetManagedType(const FProperty* Property) const
{
	return ManagedType;
}

FString FSimpleTypePropertyTranslator::GetNullReturnCSharpValue(const FProperty* ReturnProperty) const
{
	return FString::Printf(TEXT("default(%s)"), *GetManagedType(ReturnProperty));
}

FString FSimpleTypePropertyTranslator::ConvertCppDefaultParameterToCSharp(const FString& CppDefaultValue, UFunction* Function, FProperty* ParamProperty) const
{
	if (CppDefaultValue == "None")
	{
		return GetNullReturnCSharpValue(ParamProperty);
	}
	return CppDefaultValue;
}

void FSimpleTypePropertyTranslator::ExportMarshalToNativeBuffer(FCSScriptBuilder& Builder, const FProperty* Property, const FString &Owner, const FString& NativePropertyName, const FString& DestinationBuffer, const FString& Offset, const FString& Source) const
{
	Builder.AppendLine(FString::Printf(TEXT("%s.ToNative(IntPtr.Add(%s, %s), 0, %s, %s);"), *GetMarshaller(Property), *DestinationBuffer, *Offset, *Owner, *Source));
}

void FSimpleTypePropertyTranslator::ExportCleanupMarshallingBuffer(FCSScriptBuilder& Builder, const FProperty* ParamProperty, const FString& ParamName) const
{
	// No cleanup required for simple types
}

FSimpleTypePropertyTranslator::FSimpleTypePropertyTranslator(FCSSupportedPropertyTranslators& InPropertyHandlers,
	FFieldClass* InPropertyClass, EPropertyUsage InPropertyUsage): FPropertyTranslator(InPropertyHandlers, InPropertyUsage)
	                                                               , PropertyClass(InPropertyClass)
{

}

FString FSimpleTypePropertyTranslator::GetMarshaller(const FProperty *Property) const
{
	check(!MarshallerType.IsEmpty());
	return MarshallerType;
}

void FSimpleTypePropertyTranslator::ExportMarshalFromNativeBuffer(FCSScriptBuilder& Builder, const FProperty* Property, const FString &Owner, const FString& NativePropertyName, const FString& AssignmentOrReturn, const FString& SourceBuffer, const FString& Offset, bool bCleanupSourceBuffer, bool reuseRefMarshallers) const
{
	// The returned handle is just a pointer to the return value memory in the parameter buffer.
	Builder.AppendLine(FString::Printf(TEXT("%s %s.FromNative(IntPtr.Add(%s, %s), 0, %s);"), *AssignmentOrReturn, *GetMarshaller(Property), *SourceBuffer, *Offset, *Owner));
}

void FSimpleTypePropertyTranslator::ExportDefaultStructParameter(FCSScriptBuilder& Builder, const FString& VariableName, const FString& CppDefaultValue, FProperty* ParamProperty, const FPropertyTranslator& Handler) const
{
	FStructProperty* StructProperty = CastFieldChecked<FStructProperty>(ParamProperty);
	FString StructName = StructProperty->Struct->GetName();

	FString FieldInitializerList;
	if (CppDefaultValue.StartsWith(TEXT("(")) && CppDefaultValue.EndsWith(TEXT(")")))
	{
		FieldInitializerList = CppDefaultValue.Mid(1, CppDefaultValue.Len() - 2);
	}
	else
	{
		FieldInitializerList = CppDefaultValue;
	}

	TArray<FString> FieldInitializers;
	FString FieldInitializerSplit;
	while (FieldInitializerList.Split(TEXT(","), &FieldInitializerSplit, &FieldInitializerList))
	{
		FieldInitializers.Add(FieldInitializerSplit);
	}
	if (FieldInitializerList.Len())
	{
		FieldInitializers.Add(FieldInitializerList);
	}

	FString FoundCSharpType = Handler.GetManagedType(ParamProperty);
	Builder.AppendLine(FString::Printf(TEXT("%s %s = new %s"), *FoundCSharpType, *VariableName, *FoundCSharpType));
	Builder.AppendLine(TEXT("{"));
	Builder.Indent();

	bool isFloat = true;
	if (StructName == "Color")
	{
		isFloat = false;

		check(FieldInitializers.Num()== 4);
		// RGBA -> BGRA
		FString tmp = FieldInitializers[0];
		FieldInitializers[0] = FieldInitializers[2];
		FieldInitializers[2] = tmp;
	}

	TFieldIterator<FProperty> StructPropIt(StructProperty->Struct);
	for (int i = 0; i < FieldInitializers.Num(); ++i, ++StructPropIt)
	{
		check(StructPropIt);
		FProperty* Prop = *StructPropIt;
		const FString& FieldInitializer = FieldInitializers[i];

		int32 Pos = FieldInitializer.Find(TEXT("="));
		if (Pos < 0)
		{
			Builder.AppendLine(isFloat
				                   ? FString::Printf(TEXT("%s=%sf,"), *Prop->GetName(), *FieldInitializer)
				                   : FString::Printf(TEXT("%s=%s,"), *Prop->GetName(), *FieldInitializer));
		}
		else
		{
			check(Prop->GetName() == FieldInitializer.Left(Pos));
			Builder.AppendLine(isFloat
				                   ? FString::Printf(TEXT("%sf,"), *FieldInitializer)
				                   : FString::Printf(TEXT("%s,"), *FieldInitializer));
		}
	}

	Builder.Unindent();
	Builder.AppendLine(TEXT("};"));
}

FString FSimpleTypePropertyTranslator::ExportMarshallerDelegates(const FProperty *Property, const FString &PropertyName) const
{
	return FString::Printf(TEXT("%s.ToNative, %s.FromNative"), *GetMarshaller(Property), *GetMarshaller(Property));
}

FSimpleTypePropertyTranslator::FSimpleTypePropertyTranslator(FCSSupportedPropertyTranslators& InPropertyHandlers,
	FFieldClass* InPropertyClass, const FString& InCSharpType, EPropertyUsage InPropertyUsage): FPropertyTranslator(InPropertyHandlers, InPropertyUsage)
	, PropertyClass(InPropertyClass)
	, ManagedType(InCSharpType)
{

}
