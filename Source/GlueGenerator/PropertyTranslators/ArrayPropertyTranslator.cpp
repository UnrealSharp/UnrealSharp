#include "ArrayPropertyTranslator.h"
#include "GlueGenerator/CSScriptBuilder.h"
#include "GlueGenerator/CSPropertyTranslatorManager.h"

FArrayPropertyTranslator::FArrayPropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers) :
	FPropertyTranslator(InPropertyHandlers,
	                    static_cast<EPropertyUsage>(EPU_Property | EPU_Parameter | EPU_ReturnValue |
		                    EPU_OverridableFunctionParameter | EPU_OverridableFunctionReturnValue |
		                    EPU_StaticArrayProperty))
{

}

bool FArrayPropertyTranslator::CanHandleProperty(const FProperty* Property) const
{
	const FArrayProperty& ArrayProperty = *CastFieldChecked<FArrayProperty>(Property);
	const FProperty* InnerProperty = ArrayProperty.Inner;
	const FPropertyTranslator& Handler = PropertyHandlers.Find(InnerProperty);
	return Handler.IsSupportedAsArrayInner();
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
	Builder.AppendLine(FString::Printf(TEXT("%s_%s_ElementSize = %s.CallGetArrayElementSize(%s_NativeFunction, \"%s\");"), *NativeMethodName, *ParamName, FArrayPropertyCallbacks, *NativeMethodName, *ParamName));
}

void FArrayPropertyTranslator::ExportPropertyVariables(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName) const
{
	FPropertyTranslator::ExportPropertyVariables(Builder, Property, NativePropertyName);
	Builder.AppendLine(FString::Printf(TEXT("static IntPtr %s_NativeProperty;"), *NativePropertyName));
	Builder.AppendLine(FString::Printf(TEXT("%s %s_Wrapper = null;"), *GetWrapperType(Property), *NativePropertyName));
}

void FArrayPropertyTranslator::ExportParameterVariables(FCSScriptBuilder& Builder, UFunction* Function, const FString& NativeMethodName, FProperty* ParamProperty, const FString& NativePropertyName) const
{
	FPropertyTranslator::ExportParameterVariables(Builder, Function, NativeMethodName, ParamProperty, NativePropertyName);
	Builder.AppendLine(FString::Printf(TEXT("static int %s_%s_ElementSize;"), *NativeMethodName, *NativePropertyName));
}

void FArrayPropertyTranslator::ExportPropertyGetter(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName) const
{
	Builder.AppendLine(FString::Printf(TEXT("if(%s_Wrapper == null)"), *NativePropertyName));
	Builder.OpenBrace();

	const FArrayProperty& ArrayProperty = *CastFieldChecked<FArrayProperty>(Property);
	const FProperty* InnerProperty = ArrayProperty.Inner;
	const FPropertyTranslator& Handler = PropertyHandlers.Find(InnerProperty);

	Builder.AppendLine(FString::Printf(TEXT("%s_Wrapper = new %s(1, %s_NativeProperty, %s);"), *NativePropertyName, *GetWrapperType(Property), *NativePropertyName, *Handler.ExportMarshallerDelegates(InnerProperty, NativePropertyName)));

	Builder.CloseBrace();

	Builder.AppendLine();
	Builder.AppendLine(FString::Printf(TEXT("return %s_Wrapper.FromNative(IntPtr.Add(NativeObject,%s_Offset),0,this);"), *NativePropertyName, *NativePropertyName));
}

void FArrayPropertyTranslator::ExportMarshalToNativeBuffer(FCSScriptBuilder& Builder, const FProperty* Property, const FString &Owner, const FString& NativePropertyName, const FString& DestinationBuffer, const FString& Offset, const FString& Source) const
{
	const FArrayProperty& ArrayProperty = *CastFieldChecked<FArrayProperty>(Property);
	const FProperty* InnerProperty = ArrayProperty.Inner;
	const FPropertyTranslator& Handler = PropertyHandlers.Find(InnerProperty);

	FString ElementSize = NativePropertyName + "_ElementSize";
	if (UFunction* Function = Property->GetOwner<UFunction>())
	{
		FString NativeFunctionName = Function->GetName();
		ElementSize = NativeFunctionName + "_" + ElementSize;
	}

	FString InnerType = Handler.GetManagedType(InnerProperty);
	//Native buffer variable used in cleanup
	Builder.AppendLine(FString::Printf(TEXT("IntPtr %s_NativeBuffer = IntPtr.Add(%s, %s);"), *NativePropertyName, *DestinationBuffer, *Offset));
	Builder.AppendLine(FString::Printf(TEXT("UnrealArrayCopyMarshaller<%s> %s_Marshaller = new UnrealArrayCopyMarshaller<%s>(1, %s, %s);"), *InnerType, *NativePropertyName, *InnerType, *Handler.ExportMarshallerDelegates(InnerProperty, NativePropertyName),*ElementSize));
	Builder.AppendLine(FString::Printf(TEXT("%s_Marshaller.ToNative(%s_NativeBuffer, 0, null, %s);"), *NativePropertyName, *NativePropertyName, *Source));
}

void FArrayPropertyTranslator::ExportCleanupMarshallingBuffer(FCSScriptBuilder& Builder, const FProperty* ParamProperty, const FString& ParamName) const
{
	const FArrayProperty& ArrayProperty = *CastFieldChecked<FArrayProperty>(ParamProperty);
	const FProperty* InnerProperty = ArrayProperty.Inner;
	const FPropertyTranslator& Handler = PropertyHandlers.Find(InnerProperty);
	const FString InnerType = Handler.GetManagedType(InnerProperty);
	const FString Marshaller = FString::Printf(TEXT("UnrealArrayCopyMarshaller<%s>"), *InnerType);
	Builder.AppendLine(FString::Printf(TEXT("%s.DestructInstance(%s_NativeBuffer, 0);"), *Marshaller, *ParamName));
}

void FArrayPropertyTranslator::ExportMarshalFromNativeBuffer(FCSScriptBuilder& Builder, const FProperty* Property, const FString &Owner, const FString& NativePropertyName, const FString& AssignmentOrReturn, const FString& SourceBuffer, const FString& Offset, bool bCleanupSourceBuffer, bool reuseRefMarshallers) const
{
	const FArrayProperty& ArrayProperty = *CastFieldChecked<FArrayProperty>(Property);
	const FProperty* InnerProperty = ArrayProperty.Inner;
	const FPropertyTranslator& Handler = PropertyHandlers.Find(InnerProperty);

	const FString InnerType = Handler.GetManagedType(InnerProperty);
	const FString MarshallerType = FString::Printf(TEXT("UnrealArrayCopyMarshaller<%s>"), *InnerType);

	//if it was a "ref" parameter, we set the marshaler up before calling the function. if not, create one.
	if (!reuseRefMarshallers)
	{
		FString ElementSize = NativePropertyName + "_ElementSize";
		if (UFunction* Function = Property->GetOwner<UFunction>())
		{
			FString NativeFunctionName = Function->GetName();
			ElementSize = NativeFunctionName + "_" + ElementSize;
		}

		//Native buffer variable used in cleanup
		Builder.AppendLine(FString::Printf(TEXT("IntPtr %s_NativeBuffer = IntPtr.Add(%s, %s);"), *NativePropertyName, *SourceBuffer, *Offset));
		Builder.AppendLine(FString::Printf(TEXT("%s %s_Marshaller = new %s (1, %s, %s);"), *MarshallerType, *NativePropertyName, *MarshallerType, *Handler.ExportMarshallerDelegates(InnerProperty, NativePropertyName), *ElementSize));
	}
	Builder.AppendLine(FString::Printf(TEXT("%s %s_Marshaller.FromNative(%s_NativeBuffer, 0, null);"), *AssignmentOrReturn, *NativePropertyName, *NativePropertyName));

	if (bCleanupSourceBuffer)
	{
		Builder.AppendLine(FString::Printf(TEXT("%s.DestructInstance(%s_NativeBuffer, 0);"), *MarshallerType, *NativePropertyName));
	}
}


FString FArrayPropertyTranslator::GetWrapperInterface(const FProperty* Property) const
{
	const FArrayProperty& ArrayProperty = *CastFieldChecked<FArrayProperty>(Property);
	const FProperty* InnerProperty = ArrayProperty.Inner;
	const FPropertyTranslator& Handler = PropertyHandlers.Find(InnerProperty);
	check(Handler.IsSupportedAsArrayInner());

	FString InnerCSharpType = Handler.GetManagedType(InnerProperty);

	return FString::Printf(TEXT("System.Collections.Generic.%s<%s>"), Property->HasAnyPropertyFlags(CPF_BlueprintReadOnly) ? TEXT("IReadOnlyList") : TEXT("IList"), *InnerCSharpType);
}

FString FArrayPropertyTranslator::GetWrapperType(const FProperty* Property) const
{
	const FArrayProperty& ArrayProperty = *CastFieldChecked<FArrayProperty>(Property);
	const FProperty* InnerProperty = ArrayProperty.Inner;
	const FPropertyTranslator& Handler = PropertyHandlers.Find(InnerProperty);
	FString UnrealArrayType = Property->HasAnyPropertyFlags(CPF_BlueprintReadOnly) ? "UnrealArrayReadOnlyMarshaller" : "UnrealArrayReadWriteMarshaller";

	return FString::Printf(TEXT("%s<%s>"), *UnrealArrayType, *Handler.GetManagedType(InnerProperty));
}

FString FArrayPropertyTranslator::GetNullReturnCSharpValue(const FProperty* ReturnProperty) const
{
	return TEXT("null");
}

FString FArrayPropertyTranslator::ExportInstanceMarshallerVariables(const FProperty *Property, const FString &NativePropertyName) const
{
	FString MarshalerType = Property->HasAnyPropertyFlags(CPF_BlueprintReadOnly) ? "UnrealArrayReadOnlyMarshaller" : "UnrealArrayReadWriteMarshaller";
	const FArrayProperty& ArrayProperty = *CastChecked<FArrayProperty>(Property);
	const FProperty* InnerProperty = ArrayProperty.Inner;
	const FPropertyTranslator& Handler = PropertyHandlers.Find(InnerProperty);
	FString InnerType = Handler.GetManagedType(InnerProperty);
	return FString::Printf(TEXT("%s %s_Marshaller = %s(%s_Length, %s_NativeProperty, %s);"),*GetWrapperType(Property), *NativePropertyName, *GetWrapperType(Property), *NativePropertyName, *NativePropertyName, *Handler.ExportMarshallerDelegates(InnerProperty, NativePropertyName));
}

FString FArrayPropertyTranslator::ExportMarshallerDelegates(const FProperty *Property, const FString &NativePropertyName) const
{
	checkNoEntry();
	return "";
}
