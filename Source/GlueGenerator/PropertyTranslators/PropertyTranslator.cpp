#include "PropertyTranslator.h"
#include "GlueGenerator/CSGenerator.h"
#include "GlueGenerator/CSScriptBuilder.h"

void FPropertyTranslator::GetPropertyProtection(const FProperty* Property, FString& OutProtection)
{
	//properties can be RF_Public and CPF_Protected, the first takes precedence
	if (Property->HasAnyFlags(RF_Public))
	{
		OutProtection = "public ";
	}
	else if (Property->HasAnyPropertyFlags(CPF_Protected) || Property->HasMetaData(MD_BlueprintProtected))
	{
		OutProtection = "protected ";
	}
	else //it must be MD_AllowPrivateAccess
	{
		OutProtection = "public ";
	}
}

void FPropertyTranslator::ExportReferences(const FProperty* Property) const
{
	TSet<UField*> References;
	AddReferences(Property, References);

	FCSGenerator& Generator = FCSGenerator::Get();
	for (UField* Reference : References)
	{
		Generator.GenerateGlueForType(Reference, true);
	}
}

void FPropertyTranslator::AddReferences(const FProperty* Property, TSet<UField*>& References) const
{

}

FString FPropertyTranslator::GetCSharpFixedSizeArrayType(const FProperty *Property) const
{
	FString ArrayType;
	if (Property->HasAnyPropertyFlags(CPF_BlueprintReadOnly))
	{
		ArrayType = TEXT("FixedSizeArrayReadOnly");
	}
	else
	{
		ArrayType = TEXT("FixedSizeArrayReadWrite");
	}
	return FString::Printf(TEXT("%s<%s>"), *ArrayType, *GetManagedType(Property));
}

void FPropertyTranslator::ExportWrapperProperty(FCSScriptBuilder& Builder, const FProperty* Property, bool IsGreylisted, bool IsWhitelisted, const TSet<FString>& ReservedNames) const
{
	if (Property->HasAnyPropertyFlags(CPF_EditorOnly))
	{
		Builder.BeginWithEditorOnlyBlock();
	}
	
	FString CSharpPropertyName = GetScriptNameMapper().MapPropertyName(Property, ReservedNames);
	FString NativePropertyName = GetPropertyName(Property);

	Builder.AppendLine(FString::Printf(TEXT("// %s"), *Property->GetFullName()));
	ExportPropertyVariables(Builder, Property, NativePropertyName);

	if (!IsGreylisted)
	{
		BeginWrapperPropertyAccessorBlock(Builder, Property, CSharpPropertyName);
		if (Property->ArrayDim == 1)
		{
            Builder.AppendLine(TEXT("get"));
			
            Builder.OpenBrace();
            ExportPropertyGetter(Builder, Property, NativePropertyName);
            Builder.CloseBrace(); // get

            if (IsSetterRequired() && (IsWhitelisted || !Property->HasAnyPropertyFlags(CPF_BlueprintReadOnly)))
            {
                Builder.AppendLine(TEXT("set"));
                Builder.OpenBrace();
                ExportPropertySetter(Builder, Property, NativePropertyName);
                Builder.CloseBrace();
            }
		}
		else
		{
			Builder.AppendLine(TEXT("get"));
			Builder.OpenBrace();
			Builder.BeginUnsafeBlock();
			Builder.AppendLine(FString::Printf(TEXT("if (%s_Wrapper == null)"), *NativePropertyName));
			Builder.OpenBrace();
			Builder.AppendLine(ExportInstanceMarshallerVariables(Property, NativePropertyName));
			Builder.AppendLine(FString::Printf(TEXT("%s_Wrapper = new %s (this, %s_Offset, %s_Length, %s);"), *NativePropertyName, *GetCSharpFixedSizeArrayType(Property), *NativePropertyName, *NativePropertyName, *ExportMarshallerDelegates(Property, NativePropertyName)));
			Builder.CloseBrace();
			Builder.AppendLine(FString::Printf(TEXT("return %s_Wrapper;"), *NativePropertyName));
			Builder.EndUnsafeBlock();
			Builder.CloseBrace();
		}
		
		EndWrapperPropertyAccessorBlock(Builder);
	}

	ExportReferences(Property);
	OnPropertyExported(Builder, Property, NativePropertyName);
	Builder.AppendLine();

	if (Property->HasAnyPropertyFlags(CPF_EditorOnly))
	{
		Builder.EndPreprocessorBlock();
	}
}

FString FPropertyTranslator::GetPropertyName(const FProperty* Property) const
{
	return Property->GetName();
}

void FPropertyTranslator::BeginWrapperPropertyAccessorBlock(FCSScriptBuilder& Builder, const FProperty* Property, const FString& CSharpPropertyName) const
{
	FString Protection;
	GetPropertyProtection(Property, Protection);
	
	Builder.AppendLine();
	const FString PropertyType = Property->ArrayDim == 1 ? GetManagedType(Property) : GetCSharpFixedSizeArrayType(Property);
	Builder.AppendLine(FString::Printf(TEXT("%s%s %s"), GetData(Protection), *PropertyType, *CSharpPropertyName));
	Builder.OpenBrace();
}

void FPropertyTranslator::EndWrapperPropertyAccessorBlock(FCSScriptBuilder& Builder) const
{
	Builder.CloseBrace();
}

void FPropertyTranslator::ExportMirrorProperty(FCSScriptBuilder& Builder, const FProperty* Property, bool IsGreylisted, bool bSuppressOffsets, const TSet<FString>& ReservedNames) const
{
	FString CSharpPropertyName = GetScriptNameMapper().MapPropertyName(Property, ReservedNames);
	FString NativePropertyName = Property->GetName();

	Builder.AppendLine(FString::Printf(TEXT("// %s"), *Property->GetFullName()));

	if (!bSuppressOffsets)
	{
		ExportPropertyVariables(Builder, Property, NativePropertyName);
	}

	if (!IsGreylisted)
	{
		FString Protection;
		GetPropertyProtection(Property, Protection);
		
		if (IsSetterRequired())
		{
			Builder.AppendLine(FString::Printf(TEXT("%s%s %s;"), GetData(Protection), *GetManagedType(Property), *CSharpPropertyName));
		}
		else
		{
			Builder.AppendLine(FString::Printf(TEXT("%s%s %s { get; private set; }"), GetData(Protection), *GetManagedType(Property), *CSharpPropertyName));
		}
	}

	ExportReferences(Property);
	Builder.AppendLine();
}

void FPropertyTranslator::ExportPropertyStaticConstruction(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName) const
{
	Builder.AppendLine(FString::Printf(TEXT("%s_Offset = %s.CallGetPropertyOffsetFromName(NativeClassPtr, \"%s\");"), *NativePropertyName, FPropertyCallbacks, *NativePropertyName));

	if (Property->ArrayDim > 1)
	{
		check(IsSupportedInStaticArray());
		Builder.AppendLine(FString::Printf(TEXT("%s_Length = %s.CallGetPropertyArrayDimFromName(NativeClassPtr, \"%s\");"), *NativePropertyName, FPropertyCallbacks, *NativePropertyName));
	}
}

void FPropertyTranslator::ExportParameterStaticConstruction(FCSScriptBuilder& Builder, const FString& NativeMethodName, const FProperty* Parameter) const
{
	const FString ParamName = Parameter->GetName();
	Builder.AppendLine(FString::Printf(TEXT("%s_%s_Offset = %s.CallGetPropertyOffsetFromName(%s_NativeFunction, \"%s\");"),
		*NativeMethodName,
		*ParamName,
		FPropertyCallbacks,
		*NativeMethodName,
		*ParamName));
}

FPropertyTranslator::FunctionExporter::FunctionExporter(const FPropertyTranslator& InHandler, UFunction& InFunction, ProtectionMode InProtectionMode, OverloadMode InOverloadMode, BlueprintVisibility InBlueprintVisibility)
	: Handler(InHandler)
	, Function(InFunction)
	, OverrideClassBeingExtended(nullptr)
	, SelfParameter(nullptr)
{
	Initialize(InProtectionMode, InOverloadMode, InBlueprintVisibility);
}

FPropertyTranslator::FunctionExporter::FunctionExporter(const FPropertyTranslator& InHandler, UFunction& InFunction, const FProperty* InSelfParameter, const UClass* InOverrideClassBeingExtended)
	: Handler(InHandler)
	, Function(InFunction)
	, OverrideClassBeingExtended(InOverrideClassBeingExtended)
	, SelfParameter(InSelfParameter)
{
	Initialize(ProtectionMode::UseUFunctionProtection, OverloadMode::AllowOverloads, BlueprintVisibility::Call);
}

void FPropertyTranslator::FunctionExporter::Initialize(ProtectionMode InProtectionMode, OverloadMode InOverloadMode, BlueprintVisibility InBlueprintVisibility)
{
	ReturnProperty = Function.GetReturnProperty();
	CSharpMethodName = GetScriptNameMapper().MapScriptMethodName(&Function);
	if (!Function.IsA<UDelegateFunction>())
	{
		const FString OwnerClassName = GetScriptNameMapper().GetTypeScriptName(Function.GetOwnerClass());
		if (OwnerClassName == CSharpMethodName)
		{
			// Methods in C# can't have the same name as their enclosing type, so rename it
			CSharpMethodName = TEXT("Call") + CSharpMethodName;
		}
	}
	bProtected = false;

	switch (InProtectionMode)
	{
	case ProtectionMode::UseUFunctionProtection:
		if (Function.HasAnyFunctionFlags(FUNC_Public))
		{
			Modifiers = TEXT("public ");
		}
		else if (Function.HasAnyFunctionFlags(FUNC_Protected) || Function.HasMetaData(MD_BlueprintProtected))
		{
			Modifiers = TEXT("protected ");
			bProtected = true;
		}
		else
		{
			Modifiers = TEXT("public ");
		}
		break;
	case ProtectionMode::OverrideWithInternal:
		Modifiers = TEXT("internal ");
		break;
	case ProtectionMode::OverrideWithProtected:
		Modifiers = TEXT("protected ");
		break;
	default:
		checkNoEntry();
		break;
	}

	bBlueprintEvent = InBlueprintVisibility == BlueprintVisibility::Event;

	if (Function.HasAnyFunctionFlags(FUNC_Static))
	{
		Modifiers += TEXT("static ");
		PinvokeFunction = FString::Printf(TEXT("%s.CallInvokeNativeStaticFunction"), UObjectCallbacks);
		PinvokeFirstArg = TEXT("NativeClassPtr");
	}
	else if (Function.HasAnyFunctionFlags(FUNC_Delegate))
	{
		if (Function.NumParms == 0)
		{
			CustomInvoke = "ProcessDelegate(IntPtr.Zero);";
		}
		else
		{
			CustomInvoke = "ProcessDelegate(ParamsBuffer);";
		}
	}
	else
	{
		if (bBlueprintEvent)
		{
			Modifiers += "virtual ";
		}
		
		if (IsInterfaceFunction(&Function))
		{
			Modifiers = "public ";
		}
		
		PinvokeFunction = FString::Printf(TEXT("%s.CallInvokeNativeFunction"), UObjectCallbacks);
		PinvokeFirstArg = "NativeObject";
	}

	FString ParamsStringAPI;

	bool bHasDefaultParameters = false;
	const FCSNameMapper& Mapper = GetScriptNameMapper();

	// if we have a self parameter and we're exporting as a class extension method, add it as the first type
	if (SelfParameter)
	{
		const FPropertyTranslator& ParamHandler = Handler.PropertyHandlers.Find(SelfParameter);
		FString ParamType = OverrideClassBeingExtended? Mapper.GetQualifiedName(OverrideClassBeingExtended) : ParamHandler.GetManagedType(SelfParameter);

		ParamsStringAPI = FString::Printf(TEXT("this %s %s, "), *ParamType, *Mapper.MapParameterName(SelfParameter));
		ParamsStringAPIWithDefaults = ParamsStringAPI;
	}

	int ParamsProcessed = 0;
	FString ParamsStringCallNative;

	for (TFieldIterator<FProperty> ParamIt(&Function); ParamIt; ++ParamIt)
	{
		FProperty* Parameter = *ParamIt;
		const FPropertyTranslator& ParamHandler = Handler.PropertyHandlers.Find(Parameter);
		FString CSharpParamName = Mapper.MapParameterName(Parameter);
		
		if (!Parameter->HasAnyPropertyFlags(CPF_ReturnParm))
		{
			FString RefQualifier;
			
			if (!Parameter->HasAnyPropertyFlags(CPF_ConstParm))
			{
				if (Parameter->HasAnyPropertyFlags(CPF_ReferenceParm))
				{
					RefQualifier = "ref ";
				}
				else if (Parameter->HasAnyPropertyFlags(CPF_OutParm))
				{
					RefQualifier = "out ";
				}
			}

			if (SelfParameter == Parameter)
			{
				FString SelfParamName = Mapper.MapParameterName(SelfParameter);
				if (ParamsStringCall.IsEmpty())
				{
					ParamsStringCall += SelfParamName;
				}
				else
				{
					ParamsStringCall = FString::Printf(TEXT("%s, "), *SelfParamName) + ParamsStringCall.Left(ParamsStringCall.Len() - 2);
				}
				ParamsStringCallNative += SelfParamName;
			}
			else
			{
				const FString CppDefaultValue = ParamHandler.GetCppDefaultParameterValue(&Function, Parameter);

				if (CppDefaultValue == "()" && Parameter->IsA(FStructProperty::StaticClass()))
				{
					FStructProperty* StructProperty = CastFieldChecked<FStructProperty>(Parameter);
					ParamsStringCall += FString::Printf(TEXT("new %s()"), *Mapper.GetStructScriptName(StructProperty->Struct.Get()));
				}
				else
				{
					ParamsStringCall += FString::Printf(TEXT("%s%s"), *RefQualifier, *CSharpParamName);
				}
				
				ParamsStringCallNative += FString::Printf(TEXT("%s%s"), *RefQualifier, *CSharpParamName);
				ParamsStringAPI += FString::Printf(TEXT("%s%s %s"), *RefQualifier, *ParamHandler.GetManagedType(Parameter), *CSharpParamName);
				
				if ((bHasDefaultParameters || CppDefaultValue.Len()) && InOverloadMode == OverloadMode::AllowOverloads)
				{
					bHasDefaultParameters = true;
					FString CSharpDefaultValue;
					if (!CppDefaultValue.Len() || CppDefaultValue == "None")
					{
						// UHT doesn't bother storing default params for some properties when the value is equivalent to a default-constructed value.
						CSharpDefaultValue = ParamHandler.GetNullReturnCSharpValue(Parameter);

						//TODO: We can't currently detect the case where the first default parameter to a function has a default-constructed value.
						//		The metadata doesn't store so much as an empty string in that case, so an explicit HasMetaData check won't tell us anything.
					}
					else if (ParamHandler.CanExportDefaultParameter())
					{
						CSharpDefaultValue = ParamHandler.ConvertCppDefaultParameterToCSharp(CppDefaultValue, &Function, Parameter);
					}

					if (!CSharpDefaultValue.IsEmpty())
					{
						ParamsStringAPIWithDefaults += FString::Printf(TEXT("%s%s %s%s"),
							*RefQualifier,
							*ParamHandler.GetManagedType(Parameter),
							*CSharpParamName,
							*FString::Printf(TEXT(" = %s"), *CSharpDefaultValue));
					}
					else
					{
						// Approximate a default parameter by outputting multiple APIs to call this UFunction.
						// remove last comma
						if (ParamsStringAPIWithDefaults.Len() > 0)
						{
							ParamsStringAPIWithDefaults = ParamsStringAPIWithDefaults.Left(ParamsStringAPIWithDefaults.Len() - 2);
						}

						FunctionOverload Overload;
						Overload.CppDefaultValue = CppDefaultValue;
						Overload.CSharpParamName = CSharpParamName;
						Overload.ParamsStringAPIWithDefaults = ParamsStringAPIWithDefaults;
						Overload.ParamsStringCall = ParamsStringCall;
						Overload.ParamHandler = &ParamHandler;
						Overload.ParamProperty = Parameter;

						// record overload for later
						Overloads.Add(Overload);

						// Clobber all default params so far, since we've already exported an API that includes them.
						ParamsStringAPIWithDefaults = ParamsStringAPI;
					}
				}
				else
				{
					ParamsStringAPIWithDefaults = ParamsStringAPI;
				}

				ParamsStringAPI += ", ";
				ParamsStringAPIWithDefaults += ", ";
			}

			ParamsStringCall += ", ";
			ParamsStringCallNative += ", ";

		}

		ParamHandler.ExportReferences(Parameter);
		ParamsProcessed++;
	}

	// After last parameter revert change in parameter order to call native function
	if (SelfParameter)
	{
		ParamsStringCall = ParamsStringCallNative;
	}

	// remove last comma
	if (ParamsStringAPIWithDefaults.Len() > 0)
	{
		ParamsStringAPIWithDefaults = ParamsStringAPIWithDefaults.Left(ParamsStringAPIWithDefaults.Len() - 2);
	}
	
	if (ParamsStringCall.Len() > 0)
	{
		ParamsStringCall = ParamsStringCall.Left(ParamsStringCall.Len() - 2);
	}
}

void FPropertyTranslator::FunctionExporter::ExportFunctionVariables(FCSScriptBuilder& Builder) const
{
	const FString NativeMethodName = Function.GetName();
	Builder.AppendLine(FString::Printf(TEXT("// Function %s"), *Function.GetPathName()));
	Builder.AppendLine(FString::Printf(TEXT("%sIntPtr %s_NativeFunction;"), !bBlueprintEvent ? TEXT("static ") : TEXT(""), *NativeMethodName));

	if (Function.NumParms > 0)
	{
		Builder.AppendLine(FString::Printf(TEXT("static int %s_ParamsSize;"), *NativeMethodName));
	}

	for (TFieldIterator<FProperty> ParamIt(&Function); ParamIt; ++ParamIt)
	{
		FProperty* ParamProperty = *ParamIt;
		const FPropertyTranslator& ParamHandler = Handler.PropertyHandlers.Find(ParamProperty);
		ParamHandler.ExportParameterVariables(Builder, &Function, NativeMethodName, ParamProperty, ParamProperty->GetName());
	}
}

void FPropertyTranslator::FunctionExporter::ExportOverloads(FCSScriptBuilder& Builder) const
{
	for (const FunctionOverload& Overload : Overloads)
	{
		Builder.AppendLine();
		ExportDeprecation(Builder);
		Builder.AppendLine(FString::Printf(TEXT("%s%s %s(%s)"), *Modifiers, *Handler.GetManagedType(ReturnProperty), *CSharpMethodName, *Overload.ParamsStringAPIWithDefaults));
		Builder.OpenBrace();

		FString ReturnStatement = ReturnProperty ? "return " : "";

		Overload.ParamHandler->ExportCppDefaultParameterAsLocalVariable(Builder, *Overload.CSharpParamName, Overload.CppDefaultValue, &Function, Overload.ParamProperty);
		Builder.AppendLine(FString::Printf(TEXT("%s%s(%s);"), *ReturnStatement, *CSharpMethodName, *Overload.ParamsStringCall));

		Builder.CloseBrace(); // Overloaded function
	}
}

void FPropertyTranslator::FunctionExporter::ExportFunction(FCSScriptBuilder& Builder) const
{
	Builder.AppendLine();
	ExportDeprecation(Builder);
	
	if (bBlueprintEvent)
	{
		Builder.AppendLine(TEXT("[UFunction(FunctionFlags.BlueprintEvent)]"));
	}
	
	ExportSignature(Builder, Modifiers);
	Builder.OpenBrace();
	Builder.BeginUnsafeBlock();

	ExportInvoke(Builder, InvokeMode::Normal);

	Builder.CloseBrace();
	Builder.EndUnsafeBlock();
	Builder.AppendLine();
}

void FPropertyTranslator::FunctionExporter::ExportSignature(FCSScriptBuilder& Builder, const FString& Protection) const
{
	Builder.AppendLine(FString::Printf(TEXT("%s%s %s(%s)"), *Protection, ReturnProperty ? *Handler.GetManagedType(ReturnProperty) : TEXT("void"), *CSharpMethodName, *ParamsStringAPIWithDefaults));
}

void FPropertyTranslator::FunctionExporter::ExportGetter(FCSScriptBuilder& Builder) const
{
	check(ReturnProperty);
	check(Function.NumParms == 1);

	Builder.AppendLine();
	Builder.AppendLine("get");
	Builder.OpenBrace();
	Builder.BeginUnsafeBlock();
	ExportInvoke(Builder, InvokeMode::Getter);
	Builder.EndUnsafeBlock();
	Builder.CloseBrace();

}

void FPropertyTranslator::FunctionExporter::ExportSetter(FCSScriptBuilder& Builder) const
{
	check(nullptr == ReturnProperty);
	check(Function.NumParms == 1);

	Builder.AppendLine();
	Builder.AppendLine(FString::Printf(TEXT("%s set"), bProtected?TEXT("protected "):TEXT("")));
	Builder.OpenBrace();
	Builder.BeginUnsafeBlock();
	ExportInvoke(Builder, InvokeMode::Setter);
	Builder.EndUnsafeBlock();
	Builder.CloseBrace();

}

void FPropertyTranslator::FunctionExporter::ExportInvoke(FCSScriptBuilder& Builder, InvokeMode Mode) const
{
	const FString NativeMethodName = Function.GetName();

	if (bBlueprintEvent)
	{
		// Lazy-init the instance function pointer.
		Builder.AppendLine(FString::Printf(TEXT("if (%s_NativeFunction == IntPtr.Zero)"), *NativeMethodName));
		Builder.OpenBrace();
		Builder.AppendLine(FString::Printf(TEXT("%s_NativeFunction = UClassExporter.CallGetNativeFunctionFromInstanceAndName(NativeObject, \"%s\");"), *NativeMethodName, *NativeMethodName));
		Builder.CloseBrace();
	}
	
	if (Function.NumParms == 0)
	{
		if (CustomInvoke.IsEmpty())
		{
			Builder.AppendLine(FString::Printf(TEXT("%s(%s, %s_NativeFunction, IntPtr.Zero);"), *PinvokeFunction, *PinvokeFirstArg, *NativeMethodName));
		}
		else
		{
			Builder.AppendLine(CustomInvoke);
		}
	}
	else
	{
		Builder.AppendLine(FString::Printf(TEXT("byte* ParamsBufferAllocation = stackalloc byte[%s_ParamsSize];"), *NativeMethodName));
		Builder.AppendLine(TEXT("nint ParamsBuffer = (IntPtr) ParamsBufferAllocation;"));

		for (TFieldIterator<FProperty> ParamIt(&Function); ParamIt; ++ParamIt)
		{
			FProperty* ParamProperty = *ParamIt;
			const FString NativePropertyName = ParamProperty->GetName();
			
			if (!ParamProperty->HasAnyPropertyFlags(CPF_ReturnParm) && (ParamProperty->HasAnyPropertyFlags(CPF_ReferenceParm) || !ParamProperty->HasAnyPropertyFlags(CPF_OutParm)))
			{
				const FPropertyTranslator& ParamHandler = Handler.PropertyHandlers.Find(ParamProperty);
				FString SourceName = Mode == InvokeMode::Setter ? "value" : GetScriptNameMapper().MapParameterName(ParamProperty);
				ParamHandler.ExportMarshalToNativeBuffer(Builder, ParamProperty, "null", NativePropertyName, "ParamsBuffer", FString::Printf(TEXT("%s_%s_Offset"), *NativeMethodName, *NativePropertyName), SourceName);
			}
			
		}

		Builder.AppendLine();
		
		if (CustomInvoke.IsEmpty())
		{
			Builder.AppendLine(FString::Printf(TEXT("%s(%s, %s_NativeFunction, ParamsBuffer);"), *PinvokeFunction, *PinvokeFirstArg, *NativeMethodName));
		}
		else
		{
			Builder.AppendLine(CustomInvoke);
		}
		
		if (ReturnProperty || Function.HasAnyFunctionFlags(FUNC_HasOutParms))
		{
			Builder.AppendLine();
			for (TFieldIterator<FProperty> ParamIt(&Function); ParamIt; ++ParamIt)
			{
				FProperty* ParamProperty = *ParamIt;
				const FPropertyTranslator& ParamHandler = Handler.PropertyHandlers.Find(ParamProperty);
				if (ParamProperty->HasAnyPropertyFlags(CPF_ReturnParm)
					|| (!ParamProperty->HasAnyPropertyFlags(CPF_ConstParm) && ParamProperty->HasAnyPropertyFlags(CPF_OutParm)))
				{
					FString NativeParamName = ParamProperty->GetName();

					FString MarshalDestination;
					if (ParamProperty->HasAnyPropertyFlags(CPF_ReturnParm))
					{
						Builder.AppendLine(FString::Printf(TEXT("%s returnValue;"), *Handler.GetManagedType(ReturnProperty)));
						MarshalDestination = "returnValue";
					}
					else
					{
						check(Mode == InvokeMode::Normal);
						MarshalDestination = GetScriptNameMapper().MapParameterName(ParamProperty);
					}
					ParamHandler.ExportMarshalFromNativeBuffer(
						Builder,
						ParamProperty, 
						"null",
						NativeParamName,
						FString::Printf(TEXT("%s ="), *MarshalDestination),
						"ParamsBuffer",
						FString::Printf(TEXT("%s_%s_Offset"), *NativeMethodName, *NativeParamName),
						true,
						ParamProperty->HasAnyPropertyFlags(CPF_ReferenceParm) && !ParamProperty->HasAnyPropertyFlags(CPF_ReturnParm));
				}
			}
		}

		Builder.AppendLine();
		for (TFieldIterator<FProperty> ParamIt(&Function); ParamIt; ++ParamIt)
		{
			FProperty* ParamProperty = *ParamIt;
			if (!ParamProperty->HasAnyPropertyFlags(CPF_ReturnParm | CPF_OutParm))
			{
				const FPropertyTranslator& ParamHandler = Handler.PropertyHandlers.Find(ParamProperty);
				FString NativeParamName = ParamProperty->GetName();
				ParamHandler.ExportCleanupMarshallingBuffer(Builder, ParamProperty, NativeParamName);
			}
		}

		if (ReturnProperty)
		{
			Builder.AppendLine("return returnValue;");
		}
	}
}

void FPropertyTranslator::FunctionExporter::ExportDeprecation(FCSScriptBuilder& Builder) const
{
	if (Function.HasMetaData(MD_DeprecatedFunction))
	{
		FString DeprecationMessage = Function.GetMetaData(MD_DeprecationMessage);
		if (DeprecationMessage.Len() == 0)
		{
			DeprecationMessage = "This function is obsolete";
		}
		Builder.AppendLine(FString::Printf(TEXT("[Obsolete(\"%s\")]"), *DeprecationMessage));
	}
}

void FPropertyTranslator::ExportFunction(FCSScriptBuilder& Builder, UFunction* Function, FunctionType FuncType) const
{
	bool bIsEditorOnly = Function->HasAnyFunctionFlags(FUNC_EditorOnly);
	ProtectionMode ProtectionBehavior = ProtectionMode::UseUFunctionProtection;
	OverloadMode OverloadBehavior = OverloadMode::AllowOverloads;
	BlueprintVisibility CallBehavior = BlueprintVisibility::Call;

	if (FuncType == FunctionType::ExtensionOnAnotherClass)
	{
		ProtectionBehavior = ProtectionMode::OverrideWithInternal;
		OverloadBehavior = OverloadMode::SuppressOverloads;
	}
	else if (FuncType == FunctionType::BlueprintEvent)
	{
		OverloadBehavior = OverloadMode::SuppressOverloads;
		CallBehavior = BlueprintVisibility::Event;
	}
	else if (FuncType == FunctionType::InternalWhitelisted)
	{
		ProtectionBehavior = ProtectionMode::OverrideWithInternal;
	}

	if (bIsEditorOnly)
	{
		Builder.BeginWithEditorOnlyBlock();
	}
	
	FunctionExporter Exporter(*this, *Function, ProtectionBehavior, OverloadBehavior, CallBehavior);
	Exporter.ExportFunctionVariables(Builder);
	Exporter.ExportOverloads(Builder);
	Exporter.ExportFunction(Builder);

	if (bIsEditorOnly)
	{
		Builder.EndPreprocessorBlock();
	}
}

void FPropertyTranslator::ExportOverridableFunction(FCSScriptBuilder& Builder, UFunction* Function) const
{
	bool bIsEditorOnly = Function->HasAnyFunctionFlags(FUNC_EditorOnly);
	FProperty* ReturnProperty = Function->GetReturnProperty();

	FString ParamsStringAPI;
	FString ParamsCallString;
	FString NativeMethodName = Function->GetName();

	if (bIsEditorOnly)
	{
		Builder.BeginWithEditorOnlyBlock();
	}

	for (TFieldIterator<FProperty> ParamIt(Function); ParamIt; ++ParamIt)
	{
		FProperty* ParamProperty = *ParamIt;
		if (!ParamProperty->HasAnyPropertyFlags(CPF_ReturnParm))
		{
			FString CSharpParamName = GetScriptNameMapper().MapParameterName(ParamProperty);
			FString CSharpParamType = PropertyHandlers.Find(ParamProperty).GetManagedType(ParamProperty);

			// Don't generate ref or out bindings for const-reference params.
			// While the extra qualifiers would only clutter up the generated invoker,  not user code,
			// it would still give an incorrect impression that the user's implementation of the UFunction
			// is meant to change those parameters.
			FString RefQualifier;
			if (!ParamProperty->HasAnyPropertyFlags(CPF_ConstParm))
			{
				if (ParamProperty->HasAnyPropertyFlags(CPF_ReferenceParm))
				{
					RefQualifier = "ref ";
				}
				else if (ParamProperty->HasAnyPropertyFlags(CPF_OutParm))
				{
					RefQualifier = "out ";
				}
			}

			ParamsStringAPI += FString::Printf(TEXT("%s%s %s"), *RefQualifier, *CSharpParamType, *CSharpParamName);
			ParamsStringAPI += TEXT(", ");
			ParamsCallString += FString::Printf(TEXT("%s%s, "), *RefQualifier, *CSharpParamName);
		}
	}

	// remove last comma
	if (ParamsStringAPI.Len() > 0)
	{
		ParamsStringAPI = ParamsStringAPI.Left(ParamsStringAPI.Len() - 2);
	}
	if (ParamsCallString.Len() > 0)
	{
		ParamsCallString = ParamsCallString.Left(ParamsCallString.Len() - 2);
	}

	ExportFunction(Builder, Function, FunctionType::BlueprintEvent);

	Builder.AppendLine("//Hide implementation function from Intellisense/ReSharper");
	Builder.AppendLine("[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]");
	Builder.AppendLine(FString::Printf(TEXT("protected virtual %s %s_Implementation(%s)"), *GetManagedType(ReturnProperty), *NativeMethodName, *ParamsStringAPI));
	Builder.OpenBrace();

	// Out params must be initialized before we return, since there may not be any override to do it.
	for (TFieldIterator<FProperty> ParamIt(Function); ParamIt; ++ParamIt)
	{
		FProperty* ParamProperty = *ParamIt;
		if (ParamProperty->HasAnyPropertyFlags(CPF_OutParm) && !ParamProperty->HasAnyPropertyFlags(CPF_ReturnParm | CPF_ConstParm | CPF_ReferenceParm))
		{
			const FPropertyTranslator& ParamHandler = PropertyHandlers.Find(ParamProperty);
			FString CSharpParamName = GetScriptNameMapper().MapParameterName(ParamProperty);
			FString CSharpDefaultValue = ParamHandler.GetNullReturnCSharpValue(ParamProperty);
			Builder.AppendLine(FString::Printf(TEXT("%s = %s;"), *CSharpParamName, *CSharpDefaultValue));
		}
	}
	
	if (ReturnProperty)
	{
		Builder.AppendLine(FString::Printf(TEXT("return %s;"), *GetNullReturnCSharpValue(ReturnProperty)));
	}
	
	Builder.CloseBrace(); // Function

	// Export the native invoker
	Builder.AppendLine(FString::Printf(TEXT("void Invoke_%s(IntPtr buffer, IntPtr returnBuffer)"), *NativeMethodName));
	Builder.OpenBrace();
	Builder.BeginUnsafeBlock();

	FString ReturnAssignment;
	for (TFieldIterator<FProperty> ParamIt(Function); ParamIt; ++ParamIt)
	{
		FProperty* ParamProperty = *ParamIt;
		const FPropertyTranslator& ParamHandler = PropertyHandlers.Find(ParamProperty);
		FString NativeParamName = ParamProperty->GetName();
		FString CSharpParamName = GetScriptNameMapper().MapParameterName(ParamProperty);
		FString ParamType = ParamHandler.GetManagedType(ParamProperty);
		if (ParamProperty->HasAnyPropertyFlags(CPF_ReturnParm))
		{
			ReturnAssignment = FString::Printf(TEXT("%s returnValue = "), *ParamType);
		}
		else if (!ParamProperty->HasAnyPropertyFlags(CPF_ConstParm) && ParamProperty->HasAnyPropertyFlags(CPF_OutParm))
		{
			Builder.AppendLine(FString::Printf(TEXT("%s %s = default;"), *ParamType, *CSharpParamName));
		}
		else
		{
			ParamHandler.ExportMarshalFromNativeBuffer(
				Builder,
				ParamProperty, 
				TEXT("null"),
				NativeParamName,
				FString::Printf(TEXT("%s %s ="), *ParamType, *CSharpParamName),
				TEXT("buffer"),
				FString::Printf(TEXT("%s_%s_Offset"), *NativeMethodName, *NativeParamName),
				false,
				false);
		}
	}
	
	Builder.AppendLine(FString::Printf(TEXT("%s%s_Implementation(%s);"), *ReturnAssignment, *NativeMethodName, *ParamsCallString));

	if (ReturnProperty)
	{
		const FPropertyTranslator& ReturnValueHandler = PropertyHandlers.Find(ReturnProperty);
		ReturnValueHandler.ExportMarshalToNativeBuffer(
			Builder,
			ReturnProperty, 
			TEXT("null"),
			GetScriptNameMapper().MapPropertyName(ReturnProperty, {}),
			TEXT("returnBuffer"),
			TEXT("0"),
			TEXT("returnValue"));
	}
	
	for (TFieldIterator<FProperty> ParamIt(Function); ParamIt; ++ParamIt)
	{
		FProperty* ParamProperty = *ParamIt;
		const FPropertyTranslator& ParamHandler = PropertyHandlers.Find(ParamProperty);
		FString NativePropertyName = ParamProperty->GetName();
		FString CSharpParamName = GetScriptNameMapper().MapParameterName(ParamProperty);
		FString ParamType = ParamHandler.GetManagedType(ParamProperty);
		if (!ParamProperty->HasAnyPropertyFlags(CPF_ReturnParm | CPF_ConstParm) && ParamProperty->HasAnyPropertyFlags(CPF_OutParm))
		{
			ParamHandler.ExportMarshalToNativeBuffer(
				Builder,
				ParamProperty, 
				TEXT("null"),
				NativePropertyName,
				TEXT("buffer"),
				FString::Printf(TEXT("%s_%s_Offset"), *NativeMethodName, *NativePropertyName),
				CSharpParamName);
		}
	}

	Builder.EndUnsafeBlock();
	Builder.CloseBrace(); // Invoker

	if (bIsEditorOnly)
	{
		Builder.EndPreprocessorBlock();
	}

	Builder.AppendLine();
}

void FPropertyTranslator::AddNativePropertyField(FCSScriptBuilder& Builder, const FString& PropertyName)
{
	Builder.AppendLine(FString::Printf(TEXT("static IntPtr %s;"), *GetNativePropertyField(PropertyName)));
}

FString FPropertyTranslator::GetNativePropertyField(const FString& PropertyName)
{
	return FString::Printf(TEXT("%s_NativeProperty"), *PropertyName);
}

void FPropertyTranslator::ExportInterfaceFunction(FCSScriptBuilder& Builder, UFunction* Function) const
{
	FunctionExporter Exporter(*this, *Function);
	Exporter.ExportSignature(Builder, "public ");
	Builder.Append(";");
}

void FPropertyTranslator::ExportPropertyVariables(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName) const
{
	Builder.AppendLine(FString::Printf(TEXT("static int %s_Offset;"), *NativePropertyName));
	if (Property->ArrayDim > 1)
	{
		Builder.AppendLine(FString::Printf(TEXT("static int %s_Length;"), *NativePropertyName));
		Builder.AppendLine(FString::Printf(TEXT("%s %s_Wrapper;"), *GetCSharpFixedSizeArrayType(Property), *NativePropertyName));
	}
}

void FPropertyTranslator::ExportPropertyGetter(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName) const
{
	AddCheckObjectForValidity(Builder);

	ExportMarshalFromNativeBuffer(
		Builder,
		Property, 
		"this",
		NativePropertyName,
		"return",
		"NativeObject",
		FString::Printf(TEXT("%s_Offset"), *NativePropertyName),
		false,
		false);
}

void FPropertyTranslator::OnPropertyExported(FCSScriptBuilder& Builder, const FProperty* Property, const FString& PropertyName) const
{
	
}

void FPropertyTranslator::ExportPropertySetter(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName) const
{
	AddCheckObjectForValidity(Builder);
	ExportMarshalToNativeBuffer(
		Builder, 
		Property, 
		"this", 
		NativePropertyName,
		"NativeObject", 
		FString::Printf(TEXT("%s_Offset"), *NativePropertyName),
		TEXT("value"));
}

void FPropertyTranslator::ExportFunctionReturnStatement(FCSScriptBuilder& Builder, const UFunction* Function, const FProperty* ReturnProperty, const FString& NativeFunctionName, const FString& ParamsCallString) const
{
	const FString ReturnStatement = nullptr == ReturnProperty ? "" : "return ";
	Builder.AppendLine(FString::Printf(TEXT("%sInvoke_%s(NativeObject, %s_NativeFunction%s);"), GetData(ReturnStatement), *NativeFunctionName, *NativeFunctionName, *ParamsCallString));
}

void FPropertyTranslator::ExportMarshalToNativeBuffer(FCSScriptBuilder& Builder, const FProperty* Property, const FString &Owner, const FString& NativePropertyName, const FString& DestinationBuffer, const FString& Offset, const FString& Source) const
{
	checkNoEntry();
}

void FPropertyTranslator::ExportCleanupMarshallingBuffer(FCSScriptBuilder& Builder, const FProperty* ParamProperty, const FString& NativeParamName) const
{
	checkNoEntry();
}

void FPropertyTranslator::ExportMarshalFromNativeBuffer(FCSScriptBuilder& Builder, const FProperty* Property, const FString &Owner, const FString& NativePropertyName, const FString& AssignmentOrReturn, const FString& SourceBuffer, const FString& Offset, bool bCleanupSourceBuffer, bool reuseRefMarshallers) const
{
	checkNoEntry();
}

FString FPropertyTranslator::GetCppDefaultParameterValue(UFunction* Function, FProperty* ParamProperty) const
{
	//TODO: respect defaults specified as metadata, not C++ default params?
	//		The syntax for those seems to be a bit looser, but they're pretty rare...
	//		When specified that way, the key will just be the param name.

	// Return the default value exactly as specified for C++.
	// Subclasses may intercept it if it needs to be massaged for C# purposes.
	const FString MetadataCppDefaultValueKey = FString::Printf(TEXT("CPP_Default_%s"), *ParamProperty->GetName());
	return Function->GetMetaData(*MetadataCppDefaultValueKey);
}

FString FPropertyTranslator::ConvertCppDefaultParameterToCSharp(const FString& CppDefaultValue, UFunction* Function, FProperty* ParamProperty) const
{
	checkNoEntry();
	return "";
}


void FPropertyTranslator::ExportCppDefaultParameterAsLocalVariable(FCSScriptBuilder& Builder, const FString& VariableName, const FString& CppDefaultValue, UFunction* Function, FProperty* ParamProperty) const
{
	checkNoEntry();
}

FString FPropertyTranslator::ExportMarshallerDelegates(const FProperty *Property, const FString &PropertyName) const
{
	checkNoEntry();
	return "";
}

void FPropertyTranslator:: ExportParameterVariables(FCSScriptBuilder& Builder, UFunction* Function, const FString& NativeMethodName, FProperty* ParamProperty, const FString& NativePropertyName) const
{
	Builder.AppendLine(FString::Printf(TEXT("static int %s_%s_Offset;"), *NativeMethodName, *NativePropertyName));
}
