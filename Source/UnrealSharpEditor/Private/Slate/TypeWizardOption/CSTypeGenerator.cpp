#include "Slate/TypeWizardOption/CSTypeGenerator.h"

#include "CSPathsUtilities.h"
#include "CSScriptBuilder.h"
#include "Engine/BlueprintGeneratedClass.h"
#include "GameFramework/Actor.h"
#include "HAL/FileManager.h"
#include "Misc/App.h"
#include "Misc/FileHelper.h"
#include "Misc/PackageName.h"
#include "Misc/Paths.h"
#include "UObject/Package.h"
#include "Utilities/CSClassUtilities.h"

#define LOCTEXT_NAMESPACE "UnrealSharpClassGenerator"

namespace FCSTypeGeneratorPrivate
{
	static const TSet<FString>& GetCSharpKeywords()
	{
		static const TSet<FString> Keywords = {
			TEXT("abstract"), TEXT("as"), TEXT("base"), TEXT("bool"), TEXT("break"), TEXT("byte"),
			TEXT("case"), TEXT("catch"), TEXT("char"), TEXT("checked"), TEXT("class"), TEXT("const"),
			TEXT("continue"), TEXT("decimal"), TEXT("default"), TEXT("delegate"), TEXT("do"), TEXT("double"),
			TEXT("else"), TEXT("enum"), TEXT("event"), TEXT("explicit"), TEXT("extern"), TEXT("false"),
			TEXT("finally"), TEXT("fixed"), TEXT("float"), TEXT("for"), TEXT("foreach"), TEXT("goto"),
			TEXT("if"), TEXT("implicit"), TEXT("in"), TEXT("int"), TEXT("interface"), TEXT("internal"),
			TEXT("is"), TEXT("lock"), TEXT("long"), TEXT("namespace"), TEXT("new"), TEXT("null"),
			TEXT("object"), TEXT("operator"), TEXT("out"), TEXT("override"), TEXT("params"), TEXT("private"),
			TEXT("protected"), TEXT("public"), TEXT("readonly"), TEXT("ref"), TEXT("return"), TEXT("sbyte"),
			TEXT("sealed"), TEXT("short"), TEXT("sizeof"), TEXT("stackalloc"), TEXT("static"), TEXT("string"),
			TEXT("struct"), TEXT("switch"), TEXT("this"), TEXT("throw"), TEXT("true"), TEXT("try"),
			TEXT("typeof"), TEXT("uint"), TEXT("ulong"), TEXT("unchecked"), TEXT("unsafe"), TEXT("ushort"),
			TEXT("using"), TEXT("virtual"), TEXT("void"), TEXT("volatile"), TEXT("while")
		};
		
		return Keywords;
	}
	
	static FString SanitizeNamespace(const FString& InIdentifier)
	{
		FString Out;
		Out.Reserve(InIdentifier.Len());

		for (const TCHAR Char : InIdentifier)
		{
			Out.AppendChar(FChar::IsAlnum(Char) || Char == TEXT('_') ? Char : TEXT('_'));
		}

		if (!Out.IsEmpty() && FChar::IsDigit(Out[0]))
		{
			Out.InsertAt(0, TEXT('_'));
		}

		return Out;
	}
}

FString FCSTypeGenerator::GetParentTypeName(const UClass* ParentClass)
{
	if (!IsValid(ParentClass))
	{
		return FString();
	}

	const bool IsManagedClass = FCSClassUtilities::IsManagedClass(ParentClass) && IsValid(ParentClass->ClassGeneratedBy);
	const FString ParentName = IsManagedClass ? ParentClass->ClassGeneratedBy->GetName() : ParentClass->GetName();

	if (ParentClass->HasAnyClassFlags(CLASS_Interface))
	{
		return TEXT("I") + ParentName;
	}

	return (ParentClass->IsChildOf(AActor::StaticClass()) ? TEXT("A") : TEXT("U")) + ParentName;
}

FCSNamespace FCSTypeGenerator::GetManagedNamespace(const UClass* Class)
{
	if (!Class)
	{
		return FCSNamespace();
	}

	if (const UCSClass* ManagedClass = Cast<UCSClass>(Class))
	{
		return ManagedClass->GetManagedTypeDefinition()->GetFieldName().GetNamespace();
	}
	
	FCSFieldName NativeClassFieldName(Class);
	return NativeClassFieldName.GetNamespace();
}

FString FCSTypeGenerator::DeriveNamespaceFromPath(const FString& Directory)
{
	FString Normalized = Directory;
	FPaths::NormalizeDirectoryName(Normalized);
	Normalized = FPaths::ConvertRelativePathToFull(Normalized);

	const FString ScriptRoot = UnrealSharp::Paths::GetScriptFolderDirectory();

	FString Relative = Normalized;
	if (Normalized.StartsWith(ScriptRoot))
	{
		Relative = Normalized.RightChop(ScriptRoot.Len());
	}
	else
	{
		Relative = FPaths::GetCleanFilename(Normalized);
	}

	TArray<FString> Namespaces;
	Relative.ParseIntoArray(Namespaces, TEXT("/"), true);

	TArray<FString> Sanitized;
	Sanitized.Reserve(Namespaces.Num());
	
	for (const FString& NamespaceSegment : Namespaces)
	{
		FString CleanNamespace = FCSTypeGeneratorPrivate::SanitizeNamespace(NamespaceSegment);
		
		if (!CleanNamespace.IsEmpty())
		{
			Sanitized.Add(MoveTemp(CleanNamespace));
		}
	}

	return FString::Join(Sanitized, TEXT("."));
}

bool FCSTypeGenerator::IsValidCSharpIdentifier(const FString& Name, FText& OutFailReason)
{
	if (Name.IsEmpty())
	{
		OutFailReason = LOCTEXT("EmptyName", "Enter a name.");
		return false;
	}

	if (!FChar::IsAlpha(Name[0]) && Name[0] != TEXT('_'))
	{
		OutFailReason = LOCTEXT("BadFirstChar", "Names start with a letter or an underscore.");
		return false;
	}

	for (const TCHAR Char : Name)
	{
		if (FChar::IsAlnum(Char) || Char == TEXT('_'))
		{
			continue;
		}
		
		OutFailReason = FText::Format(LOCTEXT("BadChar", "'{0}' isn't allowed in a name. Use letters, digits and underscores."), FText::FromString(FString::Chr(Char)));
		return false;
	}

	if (FCSTypeGeneratorPrivate::GetCSharpKeywords().Contains(Name))
	{
		OutFailReason = FText::Format(LOCTEXT("ReservedWord", "'{0}' is a C# keyword."), FText::FromString(Name));
		return false;
	}

	return true;
}

bool FCSTypeGenerator::IsDirectoryInAProject(const FString& FilePath)
{
	auto HasCsproj = [](const FString& Path)
	{
		IFileManager& FileManager = IFileManager::Get();
		
		TArray<FString> FileNames;
		FileManager.FindFiles(FileNames, *Path, TEXT("*.csproj"));
		
		return FileNames.Num() > 0;
	};
	
	bool bFoundProject = false;
	FString CurrentDir = FilePath;
	
	while (!CurrentDir.IsEmpty())
	{
		FString ParentDir = FPaths::GetPath(CurrentDir);
		
		if (ParentDir == CurrentDir || ParentDir.IsEmpty())
		{
			break;
		}
		
		if (HasCsproj(CurrentDir))
		{
			bFoundProject = true;
			break;
		}

		CurrentDir = ParentDir;
	}
	
	return bFoundProject;
}

FString FCSTypeGenerator::BuildSource(const FCSNewTypeParams& Params)
{
	if (!Params.TypeOption.IsValid())
	{
		return FString();
	}

	TArray<FString> DeclaredUsings;
	Params.TypeOption->CollectUsings(Params, DeclaredUsings);

	FCSScriptBuilder ScriptBuilder(FCSScriptBuilder::IndentType::Tabs);
	
	for (const FString& Using : DeclaredUsings)
	{
		ScriptBuilder.AppendLine(FString::Printf(TEXT("using %s;"), *Using));
	}

	if (!DeclaredUsings.IsEmpty())
	{
		ScriptBuilder.AppendLine();
	}

	if (!Params.Namespace.IsEmpty())
	{
		ScriptBuilder.AppendLine(FString::Printf(TEXT("namespace %s;"), *Params.Namespace));
		ScriptBuilder.AppendLine();
	}

	Params.TypeOption->AppendDeclaration(Params, ScriptBuilder);

	ScriptBuilder.OpenBrace();
	ScriptBuilder.AppendLine();
	ScriptBuilder.CloseBrace();
	
	return ScriptBuilder.ToString();
}

bool FCSTypeGenerator::GenerateTypeFile(const FCSNewTypeParams& Params, FString& OutFilePath, FText& OutFailReason)
{
	if (!Params.TypeOption.IsValid())
	{
		OutFailReason = LOCTEXT("NoTypeOption", "No type option was selected.");
		return false;
	}

	OutFilePath = FPaths::Combine(Params.OutputDirectory, Params.TypeOption->GetFileName(Params));

	if (FPaths::FileExists(OutFilePath))
	{
		OutFailReason = FText::Format(LOCTEXT("AlreadyExists", "{0} already exists."), FText::FromString(OutFilePath));
		return false;
	}

	if (!IFileManager::Get().DirectoryExists(*Params.OutputDirectory) && !IFileManager::Get().MakeDirectory(*Params.OutputDirectory, true))
	{
		OutFailReason = FText::Format(LOCTEXT("MakeDirFailed", "Couldn't create {0}."), FText::FromString(Params.OutputDirectory));
		return false;
	}

	const FString Source = BuildSource(Params);
	if (!FFileHelper::SaveStringToFile(Source, *OutFilePath, FFileHelper::EEncodingOptions::ForceUTF8WithoutBOM))
	{
		OutFailReason = FText::Format(LOCTEXT("WriteFailed", "Couldn't write {0}."), FText::FromString(OutFilePath));
		return false;
	}
	
	return true;
}

#undef LOCTEXT_NAMESPACE
