#include "ArrayPropertyTranslator.h"
#include "GlueGenerator/CSScriptBuilder.h"
#include "GlueGenerator/CSPropertyTranslatorManager.h"

FArrayPropertyTranslator::FArrayPropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers) :
	FPropertyTranslator(InPropertyHandlers,
						static_cast<EPropertyUsage>(EPU_Property | EPU_StructProperty | EPU_Parameter | EPU_ReturnValue |
							EPU_OverridableFunctionParameter | EPU_OverridableFunctionReturnValue |
							EPU_StaticArrayProperty))
{

}

bool FArrayPropertyTranslator::CanHandleProperty(const FProperty* Property) const
{
	const FArrayProperty& ArrayProperty = *CastFieldChecked<FArrayProperty>(Property);
	const FProperty* InnerProperty = ArrayProperty.Inner;
	const FPropertyTranslator& Handler = PropertyHandlers.Find(InnerProperty);
	return Handler.IsSupportedAsInner();
}

void FArrayPropertyTranslator::AddReferences(const FProperty* Property, TSet<UField*>& References) const
{
	const FArrayProperty* ArrayProperty = CastFieldChecked<FArrayProperty>(Property);
	const FProperty* InnerProperty = ArrayProperty->Inner;
	const FPropertyTranslator& Handler = PropertyHandlers.Find(InnerProperty);
	return Handler.AddReferences(InnerProperty, References);
}

FString FArrayPropertyTranslator::GetManagedType(const FProperty* Property) const
{
	return GetWrapperInterface(Property);
}

void FArrayPropertyTranslator::ExportPropertyStaticConstruction(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName) const
{
	FPropertyTranslator::ExportPropertyStaticConstruction(Builder, Property, NativePropertyName);
	Builder.AppendLine(FString::Printf(TEXT("%s_NativeProperty = %s.CallGetNativePropertyFromName(NativeClassPtr, \"%s\");"), *NativePropertyName, FPropertyCallbacks,  *NativePropertyName));
}

void FArrayPropertyTranslator::ExportParameterStaticConstruction(FCSScriptBuilder& Builder, const FString& NativeMethodName, const FProperty* Parameter) const
{
	FPropertyTranslator::ExportParameterStaticConstruction(Builder, NativeMethodName, Parameter);
	const FString ParamName = Parameter->GetName();
	Builder.AppendLine(FString::Printf(TEXT("%s_%s_NativeProperty = %s.CallGetNativePropertyFromName(%s_NativeFunction, \"%s\");"), *NativeMethodName, *ParamName, FPropertyCallbacks, *NativeMethodName, *ParamName));
}

void FArrayPropertyTranslator::ExportPropertyVariables(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName) const
{
	FPropertyTranslator::ExportPropertyVariables(Builder, Property, NativePropertyName);
	Builder.AppendLine(FString::Printf(TEXT("static IntPtr %s_NativeProperty;"), *NativePropertyName));

	bool IsStructProperty = Property->GetOwnerStruct()->IsA<UScriptStruct>();
	if (IsStructProperty)
	{
		Builder.AppendLine(FString::Printf(TEXT("static %s %s_Marshaller = null;"), *GetWrapperType(Property), *NativePropertyName));
	}
	else
	{
		Builder.AppendLine(FString::Printf(TEXT("%s %s_Marshaller = null;"), *GetWrapperType(Property), *NativePropertyName));
	}
}

void FArrayPropertyTranslator::ExportParameterVariables(FCSScriptBuilder& Builder, UFunction* Function, const FString& NativeMethodName, FProperty* ParamProperty, const FString& NativePropertyName) const
{
	FPropertyTranslator::ExportParameterVariables(Builder, Function, NativeMethodName, ParamProperty, NativePropertyName);
	Builder.AppendLine(FString::Printf(TEXT("static IntPtr %s_%s_NativeProperty;"), *NativeMethodName, *NativePropertyName));
	if (Function->HasAnyFunctionFlags(FUNC_Static))
	{
		Builder.AppendLine(FString::Printf(TEXT("static %s %s_%s_Marshaller = null;"), *GetWrapperType(ParamProperty), *NativeMethodName, *NativePropertyName));
	}
	else
	{
		Builder.AppendLine(FString::Printf(TEXT("%s %s_%s_Marshaller = null;"), *GetWrapperType(ParamProperty), *NativeMethodName, *NativePropertyName));
	}
}

void FArrayPropertyTranslator::ExportPropertyGetter(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName) const
{
	const FArrayProperty& ArrayProperty = *CastFieldChecked<FArrayProperty>(Property);
	const FProperty* InnerProperty = ArrayProperty.Inner;
	const FPropertyTranslator& Handler = PropertyHandlers.Find(InnerProperty);

	Builder.AppendLine(FString::Printf(TEXT("%s_Marshaller ??= new %s(1, %s_NativeProperty, %s);"), *NativePropertyName, *GetWrapperType(Property), *NativePropertyName, *Handler.ExportMarshallerDelegates(InnerProperty, NativePropertyName)));

	Builder.AppendLine();
	Builder.AppendLine(FString::Printf(TEXT("return %s_Marshaller.FromNative(IntPtr.Add(NativeObject,%s_Offset),0);"), *NativePropertyName, *NativePropertyName));
}

void FArrayPropertyTranslator::ExportMarshalToNativeBuffer(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName, const FString& DestinationBuffer, const FString& Offset, const
														   FString& Source) const
{
	const FArrayProperty& ArrayProperty = *CastFieldChecked<FArrayProperty>(Property);
	const FProperty* InnerProperty = ArrayProperty.Inner;
	const FPropertyTranslator& Handler = PropertyHandlers.Find(InnerProperty);

	FString NativeProperty = NativePropertyName + "_NativeProperty";
	FString Marshaller = NativePropertyName + "_Marshaller";
	if (UFunction* Function = Property->GetOwner<UFunction>())
	{
		FString NativeFunctionName = Function->GetName();
		NativeProperty = NativeFunctionName + "_" + NativeProperty;
		Marshaller = NativeFunctionName + "_" + Marshaller;
	}

	const FString InnerType = Handler.GetManagedType(InnerProperty);
	const FString MarshallerType = FString::Printf(TEXT("ArrayCopyMarshaller<%s>"), *InnerType);

	Builder.AppendLine(FString::Printf(TEXT("%s ??= new %s(%s, %s);"), *Marshaller, *MarshallerType, *NativeProperty, *Handler.ExportMarshallerDelegates(InnerProperty, NativePropertyName)));

	//Native buffer variable used in cleanup
	Builder.AppendLine(FString::Printf(TEXT("IntPtr %s_NativeBuffer = IntPtr.Add(%s, %s);"), *NativePropertyName, *DestinationBuffer, *Offset));

	
	Builder.AppendLine(FString::Printf(TEXT("%s.ToNative(%s_NativeBuffer, 0, %s);"), *Marshaller, *NativePropertyName, *Source));
}

void FArrayPropertyTranslator::ExportCleanupMarshallingBuffer(FCSScriptBuilder& Builder, const FProperty* ParamProperty, const FString& ParamName) const
{
	const FString Marshaller = ParamProperty->GetOwnerChecked<UFunction>()->GetName() + "_" + ParamName + "_Marshaller";
	Builder.AppendLine(FString::Printf(TEXT("%s.DestructInstance(%s_NativeBuffer, 0);"), *Marshaller, *ParamName));
}

void FArrayPropertyTranslator::ExportMarshalFromNativeBuffer(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName, const FString& AssignmentOrReturn, const FString& SourceBuffer, const FString& Offset, bool bCleanupSourceBuffer, bool reuseRefMarshallers) const
{
	const FArrayProperty& ArrayProperty = *CastFieldChecked<FArrayProperty>(Property);
	const FProperty* InnerProperty = ArrayProperty.Inner;
	const FPropertyTranslator& Handler = PropertyHandlers.Find(InnerProperty);

	FString NativeProperty = NativePropertyName + "_NativeProperty";
	FString Marshaller = NativePropertyName + "_Marshaller";
	if (UFunction* Function = Property->GetOwner<UFunction>())
	{
		FString NativeFunctionName = Function->GetName();
		NativeProperty = NativeFunctionName + "_" + NativeProperty;
		Marshaller = NativeFunctionName + "_" + Marshaller;
	}

	const FString InnerType = Handler.GetManagedType(InnerProperty);
	const FString MarshallerType = FString::Printf(TEXT("ArrayCopyMarshaller<%s>"), *InnerType);

	if (!reuseRefMarshallers)
	{
		Builder.AppendLine(FString::Printf(TEXT("%s ??= new %s(%s, %s);"), *Marshaller, *MarshallerType, *NativeProperty, *Handler.ExportMarshallerDelegates(InnerProperty, NativePropertyName)));

		//Native buffer variable used in cleanup
		Builder.AppendLine(FString::Printf(TEXT("IntPtr %s_NativeBuffer = IntPtr.Add(%s, %s);"), *NativePropertyName, *SourceBuffer, *Offset));
	}

	Builder.AppendLine(FString::Printf(TEXT("%s %s.FromNative(%s_NativeBuffer, 0);"), *AssignmentOrReturn, *Marshaller, *NativePropertyName));

	if (bCleanupSourceBuffer)
	{
		Builder.AppendLine(FString::Printf(TEXT("%s.DestructInstance(%s_NativeBuffer, 0);"), *Marshaller, *NativePropertyName));
	}
}

FString FArrayPropertyTranslator::GetWrapperInterface(const FProperty* Property) const
{
	const FArrayProperty& ArrayProperty = *CastFieldChecked<FArrayProperty>(Property);
	const FProperty* InnerProperty = ArrayProperty.Inner;
	const FPropertyTranslator& Handler = PropertyHandlers.Find(InnerProperty);
	check(Handler.IsSupportedAsInner());

	FString InnerCSharpType = Handler.GetManagedType(InnerProperty);

	return FString::Printf(TEXT("System.Collections.Generic.%s<%s>"), Property->HasAnyPropertyFlags(CPF_BlueprintReadOnly) ? TEXT("IReadOnlyList") : TEXT("IList"), *InnerCSharpType);
}

FString FArrayPropertyTranslator::GetWrapperType(const FProperty* Property) const
{
	bool IsStructProperty = Property->GetOwnerStruct()->IsA<UScriptStruct>();
	bool IsParamProperty = Property->HasAnyPropertyFlags(CPF_Parm);
	const FArrayProperty& ArrayProperty = *CastFieldChecked<FArrayProperty>(Property);
	const FProperty* InnerProperty = ArrayProperty.Inner;
	const FPropertyTranslator& Handler = PropertyHandlers.Find(InnerProperty);
	
	FString UnrealArrayType = IsStructProperty || IsParamProperty
	? TEXT("ArrayCopyMarshaller") : Property->HasAnyPropertyFlags(CPF_BlueprintReadOnly) ? TEXT("ArrayReadOnlyMarshaller") : TEXT("ArrayMarshaller");

	return FString::Printf(TEXT("%s<%s>"), *UnrealArrayType, *Handler.GetManagedType(InnerProperty));
}

FString FArrayPropertyTranslator::GetNullReturnCSharpValue(const FProperty* ReturnProperty) const
{
	return TEXT("null");
}

FString FArrayPropertyTranslator::ExportInstanceMarshallerVariables(const FProperty *Property, const FString &NativePropertyName) const
{
	return "";
}

FString FArrayPropertyTranslator::ExportMarshallerDelegates(const FProperty *Property, const FString &NativePropertyName) const
{
	checkNoEntry();
	return "";
}
