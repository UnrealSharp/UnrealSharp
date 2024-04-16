#pragma once

#include "GlueGenerator/CSNameMapper.h"
#include "GlueGenerator/CSPropertyTranslatorManager.h"

static const FName MD_DeprecatedFunction(TEXT("DeprecatedFunction"));
static const FName MD_DeprecationMessage(TEXT("DeprecationMessage"));
static const FName MD_BlueprintProtected(TEXT("BlueprintProtected"));

class FPropertyTranslator
{
public:
	virtual ~FPropertyTranslator() = default;

	FPropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers, EPropertyUsage InPropertyUsage)
	: PropertyHandlers(InPropertyHandlers)
	, SupportedPropertyUsage(InPropertyUsage)
	{

	}

	static void GetPropertyProtection(const FProperty* Property, FString& OutProtection);

	virtual bool CanHandleProperty(const FProperty* Property) const = 0;

	// Subclasses may override to specify any additional classes that must be exported to handle a property.
	void ExportReferences(const FProperty* Property) const;
	virtual void AddReferences(const FProperty* Property, TSet<UField*>& References) const;
	void ExportDelegateReferences(const FProperty* Property) const;
	virtual void AddDelegateReferences(const FProperty* Property, TSet<UFunction*>& DelegateSignatures) const;

	virtual FString GetManagedType(const FProperty* Property) const = 0;
	virtual FString GetCSharpFixedSizeArrayType(const FProperty* Property) const;

	bool IsSupportedAsProperty() const { return !!(SupportedPropertyUsage & EPU_Property); }
	bool IsSupportedAsParameter() const { return !!(SupportedPropertyUsage & EPU_Parameter); }
	bool IsSupportedAsReturnValue() const { return !!(SupportedPropertyUsage & EPU_ReturnValue); }
	bool IsSupportedAsArrayInner() const { return !!(SupportedPropertyUsage & EPU_ArrayInner); }
	bool IsSupportedAsStructProperty() const { return !!(SupportedPropertyUsage & EPU_StructProperty); }
	bool IsSupportedAsOverridableFunctionParameter() const { return !!(SupportedPropertyUsage & EPU_OverridableFunctionParameter); }
	bool IsSupportedAsOverridableFunctionReturnValue() const { return !!(SupportedPropertyUsage & EPU_OverridableFunctionReturnValue); }
	bool IsSupportedInStaticArray() const { return !!(SupportedPropertyUsage & EPU_StaticArrayProperty); }

	virtual bool IsBlittable() const { return false; }

	// Exports a C# property which wraps a native FProperty, suitable for use in a reference type backed by a UObject.
	void ExportWrapperProperty(FCSScriptBuilder& Builder, const FProperty* Property, bool IsGreylisted, bool IsWhitelisted, const TSet<FString>& ReservedNames) const;
	virtual FString GetPropertyName(const FProperty* Property) const;
	virtual void ExportPropertyStaticConstruction(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName) const;
	virtual void ExportParameterStaticConstruction(FCSScriptBuilder& Builder, const FString& NativeMethodName, const FProperty* Parameter) const;
	
	// helpers for collapsed getter/setters
	void BeginWrapperPropertyAccessorBlock(FCSScriptBuilder& Builder, const FProperty* Property, const FString& PropertyName) const;
	void EndWrapperPropertyAccessorBlock(FCSScriptBuilder& Builder) const;

	// Exports a C# property which mirrors a FProperty, suitable for use in a value type.
	void ExportMirrorProperty(FCSScriptBuilder& Builder, const FProperty* Property, bool IsGreylisted, bool bSuppressOffsets, const TSet<FString>& ReservedNames) const;

	enum class FunctionType : uint8
	{
		Normal,
		BlueprintEvent,
		ExtensionOnAnotherClass,
		InterfaceFunction,
		InternalWhitelisted
	};
	
	void ExportFunction(FCSScriptBuilder& Builder, UFunction* Function, FunctionType FuncType) const;
	void ExportInterfaceFunction(FCSScriptBuilder& Builder, UFunction* Function) const;
	void ExportOverridableFunction(FCSScriptBuilder& Builder, UFunction* Function) const;
	void ExportDelegateFunction(FCSScriptBuilder& Builder, UFunction* SignatureFunction) const;

	static void AddNativePropertyField(FCSScriptBuilder& Builder, const FString& PropertyName);
	static FString GetNativePropertyField(const FString& PropertyName);

	virtual void ExportMarshalToNativeBuffer(FCSScriptBuilder& Builder, const FProperty* Property, const FString &Owner, const FString& PropertyName, const FString& DestinationBuffer, const FString& Offset, const FString& Source) const;
	virtual void ExportCleanupMarshallingBuffer(FCSScriptBuilder& Builder, const FProperty* ParamProperty, const FString& ParamName) const;
	virtual void ExportMarshalFromNativeBuffer(FCSScriptBuilder& Builder, const FProperty* Property, const FString &Owner, const FString& PropertyName, const FString& AssignmentOrReturn, const FString& SourceBuffer, const FString& Offset, bool bCleanupSourceBuffer, bool reuseRefMarshallers) const;

	// Subclasses must override to export the C# property's get accessor, if property usage is supported.
	virtual void ExportPropertyGetter(FCSScriptBuilder& Builder, const FProperty* Property, const FString& PropertyName) const;
	virtual void OnPropertyExported(FCSScriptBuilder& Builder, const FProperty* Property, const FString& PropertyName) const;
	
	struct FunctionOverload
	{
		FString ParamsStringAPIWithDefaults;
		FString ParamsStringCall;
		FString CSharpParamName;
		FString CppDefaultValue;
		const FPropertyTranslator* ParamHandler;
		FProperty* ParamProperty;
	};

	enum class ProtectionMode : uint8
	{
		UseUFunctionProtection,
		OverrideWithInternal,
		OverrideWithProtected,
	};

	enum class OverloadMode : uint8
	{
		AllowOverloads,
		SuppressOverloads,
	};

	enum class BlueprintVisibility : uint8
	{
		Call,
		Event,
	};

	class FunctionExporter
	{
	public:
		FunctionExporter(const FPropertyTranslator& InHandler, UFunction& InFunction, ProtectionMode InProtectionMode = ProtectionMode::UseUFunctionProtection, OverloadMode InOverloadMode = OverloadMode::AllowOverloads, BlueprintVisibility InBlueprintVisibility = BlueprintVisibility::Call);
		FunctionExporter(const FPropertyTranslator& InHandler, UFunction& InFunction, const FProperty* InSelfParameter, const UClass* InOverrideClassBeingExtended);

		void ExportFunctionVariables(FCSScriptBuilder& Builder) const;

		void ExportOverloads(FCSScriptBuilder& Builder) const;

		void ExportFunction(FCSScriptBuilder& Builder) const;

		void ExportSignature(FCSScriptBuilder& Builder, const FString& Protection) const;

		void ExportGetter(FCSScriptBuilder& Builder) const;
		void ExportSetter(FCSScriptBuilder& Builder) const;

		const FCSNameMapper& GetScriptNameMapper() const { return Handler.GetScriptNameMapper(); }
		
		void Initialize(ProtectionMode InProtectionMode, OverloadMode InOverloadMode, BlueprintVisibility InBlueprintVisibility);

		enum class InvokeMode : uint8
		{
			Normal,
			Getter,
			Setter,
		};
		
		void ExportInvoke(FCSScriptBuilder& Builder, InvokeMode Mode) const;

		void ExportDeprecation(FCSScriptBuilder& Builder) const;

		const FPropertyTranslator& Handler;
		UFunction& Function;
		const UClass* OverrideClassBeingExtended;
		const FProperty* SelfParameter;
		FProperty* ReturnProperty;
		FString CSharpMethodName;
		FString Modifiers;
		bool bProtected;
		bool bBlueprintEvent;
		FString PinvokeFunction;
		FString PinvokeFirstArg;
		FString CustomInvoke;
		FString ParamsStringCall;
		FString ParamsStringAPIWithDefaults;
		TArray<FunctionOverload> Overloads;
	};

	virtual FString ExportInstanceMarshallerVariables(const FProperty *Property, const FString &PropertyName) const { return TEXT(""); }
	virtual FString ExportMarshallerDelegates(const FProperty *Property, const FString &PropertyName) const;

	const FCSNameMapper& GetScriptNameMapper() const { return PropertyHandlers.GetScriptNameMapper(); }

protected:
	// Export the variables backing the C# property accessor for a FProperty.
	// By default, this is just the FProperty's offset within the UObject, but subclasses may override
	// to export different or additional fields.
	virtual void ExportPropertyVariables(FCSScriptBuilder& Builder, const FProperty* Property, const FString& PropertyName) const;

	// Export the variables backing a FProperty used as a function parameter.
	virtual void ExportParameterVariables(FCSScriptBuilder& Builder, UFunction* Function, const FString& BackingFunctionName, FProperty* ParamProperty, const FString& BackingPropertyName) const;

	// Subclasses may override to suppress generation of a property setter in cases where none is required.
	virtual bool IsSetterRequired() const { return true; }

	// Subclasses must override to export the C# property's set accessor, if property usage is supported and IsSetterRequired can return true.
	virtual void ExportPropertySetter(FCSScriptBuilder& Builder, const FProperty* Property, const FString& PropertyName) const;

	virtual void ExportFunctionReturnStatement(FCSScriptBuilder& Builder, const UFunction* Function, const FProperty* ReturnProperty, const FString& FunctionName, const FString& ParamsCallString) const;

	virtual FString GetNullReturnCSharpValue(const FProperty* ReturnProperty) const = 0;

	// Subclasses may override to suppress the generation of default parameters, which may be necessary due to C#'s 
	// requirement that default values be compile-time const, and limitations on what types may be declared const.
	// When necessary, non-exportable default parameters will be approximated by generating overloaded methods.
	virtual bool CanExportDefaultParameter() const { return true; }

	virtual FString ConvertCppDefaultParameterToCSharp(const FString& CppDefaultValue, UFunction* Function, FProperty* ParamProperty) const;

	// Export C# code to declare and initialize a variable approximating a default parameter.
	// Subclasses must override when CanExportDefaultParameter() can return false.
	virtual void ExportCppDefaultParameterAsLocalVariable(FCSScriptBuilder& Builder, const FString& VariableName, const FString& CppDefaultValue, UFunction* Function, FProperty* ParamProperty) const;

	FCSPropertyTranslatorManager& PropertyHandlers;

private:
	// Returns the default value for a parameter property, or an empty string if no default is defined.
	FString GetCppDefaultParameterValue(UFunction* Function, FProperty* ParamProperty) const;

	EPropertyUsage SupportedPropertyUsage;
};
