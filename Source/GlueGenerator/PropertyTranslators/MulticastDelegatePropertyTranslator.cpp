#include "MulticastDelegatePropertyTranslator.h"

#include "GlueGenerator/CSGenerator.h"
#include "GlueGenerator/CSScriptBuilder.h"

bool FMulticastDelegatePropertyTranslator::CanHandleProperty(const FProperty* Property) const
{
	const FMulticastDelegateProperty* DelegateProperty = CastField<FMulticastDelegateProperty>(Property);

	if (!FCSGenerator::Get().CanExportFunctionParameters(DelegateProperty->SignatureFunction))
	{
		return false;	
	}
	
	return true;
}

FString FMulticastDelegatePropertyTranslator::GetManagedType(const FProperty* Property) const
{
	return Property->GetName();
}



void FMulticastDelegatePropertyTranslator::ExportPropertyVariables(FCSScriptBuilder& Builder, const FProperty* Property, const FString& PropertyName) const
{
	AddNativePropertyField(Builder, PropertyName);

	FString BackingFieldName = GetBackingFieldName(Property);
	Builder.AppendLine(FString::Printf(TEXT("private %s %s;"), *PropertyName, *BackingFieldName));
	
	FPropertyTranslator::ExportPropertyVariables(Builder, Property, PropertyName);
}

void FMulticastDelegatePropertyTranslator::ExportPropertySetter(FCSScriptBuilder& Builder, const FProperty* Property, const FString& PropertyName) const
{
	Builder.AppendLine(FString::Printf(TEXT("DelegateMarshaller<%s>.ToNative(IntPtr.Add(NativeObject,%s_Offset),0,this,value);"), *PropertyName, *PropertyName));
}

void FMulticastDelegatePropertyTranslator::ExportPropertyGetter(FCSScriptBuilder& Builder, const FProperty* Property, const FString& PropertyName) const
{
	FString BackingFieldName = GetBackingFieldName(Property);
	FString NativePropertyFieldName = GetNativePropertyField(PropertyName);
	
	Builder.AppendLine(FString::Printf(TEXT("if (%s == null)"), *BackingFieldName));
	Builder.OpenBrace();
	Builder.AppendLine(FString::Printf(TEXT("%s = DelegateMarshaller<%s>.FromNative(IntPtr.Add(NativeObject, %s_Offset), %s, 0, this);"),
		*BackingFieldName, GetData(PropertyName), GetData(PropertyName), *NativePropertyFieldName));
	Builder.CloseBrace();
	Builder.AppendLine(FString::Printf(TEXT("return %s;"), *BackingFieldName));
}

FString FMulticastDelegatePropertyTranslator::GetNullReturnCSharpValue(const FProperty* ReturnProperty) const
{
	return "null";
}

void FMulticastDelegatePropertyTranslator::OnPropertyExported(FCSScriptBuilder& Builder, const FProperty* Property, const FString& PropertyName) const
{
	FCSModule& Module = FCSGenerator::Get().FindOrRegisterModule(Property->GetOutermost());
	const FMulticastDelegateProperty* DelegateProperty = CastFieldChecked<FMulticastDelegateProperty>(Property);
	UFunction* Function = DelegateProperty->SignatureFunction;
	
	FCSScriptBuilder DelegateBuilder(FCSScriptBuilder::IndentType::Spaces);

	DelegateBuilder.GenerateScriptSkeleton(Module.GetNamespace());
	DelegateBuilder.AppendLine();

	FString SignatureName = FString::Printf(TEXT("%s.Signature"), *PropertyName);
	FString SuperClass = FString::Printf(TEXT("MulticastDelegate<%s>"), *SignatureName);
	
	DelegateBuilder.DeclareType("class", PropertyName, SuperClass, true);
	
	FunctionExporter Exporter(*this, *Function, ProtectionMode::UseUFunctionProtection, OverloadMode::SuppressOverloads, BlueprintVisibility::Call);

	// Write signature delegate
	DelegateBuilder.AppendLine(FString::Printf(TEXT("public delegate void Signature(%s);"), *Exporter.ParamsStringAPIWithDefaults));
	DelegateBuilder.AppendLine();

	// Write fields needed for native invoker
	Exporter.ExportFunctionVariables(DelegateBuilder);
	DelegateBuilder.AppendLine();

	// Write native invoker
	DelegateBuilder.AppendLine(FString::Printf(TEXT("protected void Invoker(%s)"), *Exporter.ParamsStringAPIWithDefaults));
	DelegateBuilder.OpenBrace();
	DelegateBuilder.BeginUnsafeBlock();
	Exporter.ExportInvoke(DelegateBuilder, FunctionExporter::InvokeMode::Normal);
	DelegateBuilder.EndUnsafeBlock();
	DelegateBuilder.CloseBrace();

	// Write delegate initializer
	DelegateBuilder.AppendLine("static public void InitializeUnrealDelegate(IntPtr nativeDelegateProperty)");
	DelegateBuilder.OpenBrace();
	FCSGenerator::Get().ExportDelegateFunctionStaticConstruction(DelegateBuilder, Function);
	DelegateBuilder.CloseBrace();
	
	DelegateBuilder.CloseBrace();
	
	FString FileName = FString::Printf(TEXT("%s.generated.cs"), *PropertyName);
	FCSGenerator::Get().SaveGlue(&Module, FileName, DelegateBuilder.ToString());
}

FString FMulticastDelegatePropertyTranslator::GetBackingFieldName(const FProperty* Property)
{
	return FString::Printf(TEXT("%s_BackingField"), *Property->GetName());
}
