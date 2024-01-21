#include "CSScriptBuilder.h"
#include "Internationalization/Regex.h"
#include "Misc/FileHelper.h"
#include "Misc/Paths.h"
#include "HAL/PlatformFileManager.h"

const FName MD_IsBlueprintBase(TEXT("IsBlueprintBase"));
const FName MD_BlueprintFunctionLibrary(TEXT("BlueprintFunctionLibrary"));
const FName MD_AllowableBlueprintVariableType(TEXT("BlueprintType"));
const FName MD_NotAllowableBlueprintVariableType(TEXT("NotBlueprintType"));
const FName MD_BlueprintInternalUseOnly(TEXT("BlueprintInternalUseOnly"));
const FName MD_BlueprintSpawnableComponent(TEXT("BlueprintSpawnableComponent"));
const FName MD_FunctionCategory(TEXT("Category"));
const FName MD_DefaultToSelf(TEXT("DefaultToSelf"));
const FName MD_Latent(TEXT("Latent"));
const FName NAME_ToolTip(TEXT("ToolTip"));

void FCSScriptBuilder::GenerateScriptSkeleton(const FString& Namespace)
{
	AppendLine(FString::Printf(TEXT("using %s;"), UNREAL_SHARP_ENGINE_NAMESPACE));
	AppendLine(FString::Printf(TEXT("using %s;"), UNREAL_SHARP_ATTRIBUTES_NAMESPACE));
	AppendLine(TEXT("using UnrealSharp.Interop;"));
	AppendLine(TEXT("using System.DoubleNumerics;"));
	AppendLine(TEXT("using System.Runtime;"));
	AppendLine(TEXT("using System.Runtime.InteropServices;"));
	AppendLine();
	AppendLine(FString::Printf(TEXT("namespace %s;"), *Namespace));
	AppendLine();
}

void FCSScriptBuilder::DeclareDirective(const FString& ModuleName)
{
	AppendLine(FString::Printf(TEXT("using %s;"), *ModuleName));
}

void FCSScriptBuilder::DeclareType(const FString& TypeName, const FString& DeclaredTypeName, const FString& SuperTypeName, bool IsAbstract, bool IsPartial, const TArray<FString>& Interfaces)
{
	FString PartialSpecifier = IsPartial ? "partial " : "";
	FString AbstractSpecifier = IsAbstract ? "abstract " : "";
		
	FString SuperTypeDeclaration;
	if (!SuperTypeName.IsEmpty())
	{
		SuperTypeDeclaration = FString::Printf(TEXT(" : %s"), *SuperTypeName);
	}

	FString InterfacesDeclaration;
	if (!Interfaces.IsEmpty())
	{
		for (const FString& InterfaceName : Interfaces)
		{
			InterfacesDeclaration += FString::Printf(TEXT(", %s"), *("I" + InterfaceName));
		}
	}
		
	FString DeclarationLine = FString::Printf(TEXT("%spublic %s%s %s%s%s"),
	                                          *AbstractSpecifier,
	                                          *PartialSpecifier,
	                                          *TypeName,
	                                          *DeclaredTypeName,
	                                          *SuperTypeDeclaration,
	                                          *InterfacesDeclaration);
	
	AppendLine(DeclarationLine);
	OpenBrace();
}
