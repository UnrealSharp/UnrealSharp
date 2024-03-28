#pragma once

#include "CoreMinimal.h"
#include "UObject/UnrealType.h"
#include "UObject/MetaData.h"
#include "UObject/Package.h"
#include "Misc/PackageName.h"

#define UNREAL_SHARP_NAMESPACE TEXT("UnrealSharp")
#define UNREAL_SHARP_OBJECT TEXT("UnrealSharpObject")
#define UNREAL_SHARP_RUNTIME_NAMESPACE UNREAL_SHARP_NAMESPACE TEXT(".Runtime")
#define UNREAL_SHARP_ENGINE_NAMESPACE UNREAL_SHARP_NAMESPACE TEXT(".Engine")
#define UNREAL_SHARP_ATTRIBUTES_NAMESPACE UNREAL_SHARP_NAMESPACE TEXT(".Attributes")

// mirrored from EdGraphSchema_K2.cpp (we can't bring in Kismet into a program plugin)
extern const FName MD_IsBlueprintBase;
extern const FName MD_BlueprintFunctionLibrary;
extern const FName MD_AllowableBlueprintVariableType;
extern const FName MD_NotAllowableBlueprintVariableType;
extern const FName MD_BlueprintInternalUseOnly;
extern const FName MD_BlueprintSpawnableComponent;
extern const FName MD_FunctionCategory;
extern const FName MD_DefaultToSelf;
extern const FName MD_Latent;
extern const FName NAME_ToolTip;

class FCSScriptBuilder
{
public:
	enum class IndentType
	{
		Spaces,
		Tabs
	};
	explicit FCSScriptBuilder(IndentType InIndentMode)
	: UnsafeBlockCount(0)
	, IndentCount(0)
	, IndentMode(InIndentMode)
	{
	}

	void Indent()
	{
		++IndentCount;
	}

	void Unindent()
	{
		--IndentCount;
	}

	void AppendLine()
	{
		if (Report.Len() != 0)
		{
			Report.Append(LINE_TERMINATOR);
		}

		if (IndentMode == IndentType::Spaces)
		{
			for (int32 Index = 0; Index < IndentCount; Index++)
			{
				Report.Append(TEXT("    "));
			}
		}
		else
		{
			for (int32 Index = 0; Index < IndentCount; Index++)
			{
				Report.Append(TEXT("\t"));
			}
		}
	}

	void Append(const FString& String)
	{
		Report.Append(String);
	}

	void AppendLine(const FText& Text)
	{
		AppendLine();

		if (const FString* SourceString = FTextInspector::GetSourceString(Text))
		{
			Report.Append(*SourceString);
		}
		else
		{
			Report.Append(Text.ToString());
		}
	}

	void AppendLine(const FString& String)
	{
		AppendLine();
		Report.Append(String);
	}

	void AppendLine(const ANSICHAR* Line)
	{
		AppendLine();
		Report.Append(Line);
	}

	void AppendLine(const FName& Name)
	{
		AppendLine();
		Report.Append(Name.ToString());
	}

	void AppendLine(const TCHAR* Line)
	{
		AppendLine();
		Report.Append(Line);
	}

	void OpenBrace()
	{
		AppendLine(TEXT("{"));
		Indent();
	}

	void CloseBrace()
	{
		Unindent();
		AppendLine(TEXT("}"));
	}

	void BeginUnsafeBlock()
	{
		if (!UnsafeBlockCount++)
		{
			AppendLine(TEXT("unsafe"));
			OpenBrace();
		}
	}

	void EndUnsafeBlock()
	{
		check(UnsafeBlockCount >= 0);
		if (!--UnsafeBlockCount)
		{
			CloseBrace();
		}
	}

	void AppendUnsafeLine(const FString& Line)
	{
		if (!UnsafeBlockCount)
		{
			AppendLine(FString::Printf(TEXT("unsafe { %s }"), *Line));
		}
		else
		{
			AppendLine(Line);
		}
	}

	void AppendUnsafeLine(const TCHAR* Line)
	{
		AppendUnsafeLine(FString(Line));
	}

	void Clear()
	{
		Report.Reset();
	}

	FText ToText() const
	{
		return FText::FromString(ToString());
	}

	FString ToString() const
	{
		return Report.ToString();
	}

	bool IsEmpty() const
	{
		return Report.Len() == 0;
	}

	void GenerateScriptSkeleton(const FString& Namespace);
	void DeclareDirective(const FString& ModuleName);
	void DeclareType(const FString& TypeName, const FString& DeclaredTypeName, const FString& SuperTypeName = "", bool IsPartial = true, const
	                 TArray<FString>& Interfaces = {});

private:

	TStringBuilder<2048> Report;
	TArray<FString> Directives;
	int32 UnsafeBlockCount;
	int32 IndentCount;
	IndentType IndentMode;
};

class FCSPropertyBuilder
{
public:
	FCSPropertyBuilder()
	{
		String = FString::Printf(TEXT("["));
		State = AttributeState::Open;
	}

	void AddAttribute(const FString& AttributeName)
	{
		switch (State)
		{
		case AttributeState::Open:
			break;
		case AttributeState::InAttribute:
			String += TEXT(", ");
			break;
		case AttributeState::InAttributeParams:
			String += TEXT("), ");
			break;
		default:
			checkNoEntry();
			break;
		}
		String += AttributeName;
		State = AttributeState::InAttribute;
	}

	void AddArgument(const FString& Arg)
	{
		switch (State)
		{
		case AttributeState::InAttribute:
			String += TEXT("(");
			break;
		case AttributeState::InAttributeParams:
			String += TEXT(", ");
			break;
		default:
			checkNoEntry();
			break;
		}
		String += Arg;
		State = AttributeState::InAttributeParams;
	}

	void AddMetaData(const UObject& InObject)
	{
		TMap<FName, FString>* MetaDataMap = UMetaData::GetMapForObject(&InObject);

		if (nullptr != MetaDataMap)
		{
			for (TMap<FName, FString>::TIterator It(*MetaDataMap); It; ++It)
			{
				AddAttribute(TEXT("UMetaData"));
				AddArgument(FString::Printf(TEXT("\"%s\""),*It.Key().ToString()));
				if (It.Value().Len() > 0)
				{
					FString Value = It.Value();
					// ReplaceCharWithEscapedChar doesn't seem to do what we want, it'll replace "\r" with "\\\\r"
					Value.ReplaceInline(TEXT("\\"), TEXT("\\\\"));
					Value.ReplaceInline(TEXT("\r"), TEXT("\\r"));
					Value.ReplaceInline(TEXT("\n"), TEXT("\\n"));
					Value.ReplaceInline(TEXT("\t"), TEXT("\\t"));
					Value.ReplaceInline(TEXT("\""), TEXT("\\\""));

					AddArgument(FString::Printf(TEXT("\"%s\""), *Value));
			}
			}
		}
	}

	void Finish()
	{
		switch (State)
		{
		case AttributeState::InAttribute:
			String += TEXT("]");
			break;
		case AttributeState::InAttributeParams:
			String += TEXT(")]");
			break;
		default:
			checkNoEntry();
			break;
		}

		State = AttributeState::Closed;
	}

	const FString& ToString() const
	{
		check(State == AttributeState::Closed);
		return String;
	}

private:
	FString String;
	enum class AttributeState : uint8
	{
		Open,
		Closed,
		InAttribute,
		InAttributeParams
	};
	AttributeState State;
};
