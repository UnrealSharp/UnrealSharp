#include "DefaultGenerators/CSStringTableGlueGenerator.h"
#include "AssetRegistry/IAssetRegistry.h"
#include "CSScriptBuilder.h"
#include "Internationalization/StringTable.h"
#include "Internationalization/StringTableCore.h"
#include "UObject/UObjectHash.h"
#include "Utilities/CSEditorUtilities.h"

void UCSStringTableGlueGenerator::Initialize()
{
	IAssetRegistry::GetChecked().OnFilesLoaded().AddUObject(this, &ThisClass::ProcessStringTables);
	UPackage::PackageSavedWithContextEvent.AddUObject(this, &ThisClass::OnPackageSaved);

	if (!IAssetRegistry::GetChecked().IsLoadingAssets())
	{
		ProcessStringTables();
	}
}

void UCSStringTableGlueGenerator::OnPackageSaved(const FString& PackageFileName, UPackage* Package, FObjectPostSaveContext SaveContext)
{
	if (!Package || !FindObjectWithOuter(Package, UStringTable::StaticClass()))
	{
		return;
	}

	ProcessStringTables();
}

void UCSStringTableGlueGenerator::ProcessStringTables()
{
	TArray<FAssetData> StringTableAssets;
	IAssetRegistry::GetChecked().GetAssetsByClass(UStringTable::StaticClass()->GetClassPathName(), StringTableAssets);

	StringTableAssets.Sort([](const FAssetData& A, const FAssetData& B)
	{
		return A.AssetName.LexicalLess(B.AssetName);
	});

	FCSScriptBuilder ScriptBuilder(FCSScriptBuilder::IndentType::Tabs);
	ScriptBuilder.AppendLine(TEXT("using UnrealSharp.Core;"));
	ScriptBuilder.AppendLine(TEXT("using UnrealSharp.Engine;"));
	ScriptBuilder.AppendLine();
	ScriptBuilder.AppendLine(TEXT("public static class StringTables"));
	ScriptBuilder.OpenBrace();

	for (const FAssetData& StringTableAsset : StringTableAssets)
	{
		const UStringTable* StringTable = Cast<UStringTable>(StringTableAsset.GetAsset());
		if (!StringTable)
		{
			continue;
		}

		TArray<FString> Keys;
		StringTable->GetStringTable()->EnumerateSourceStrings([&Keys](const FString& Key, const FString& SourceString)
		{
			Keys.Add(Key);
			return true;
		});
		Keys.Sort();

		ScriptBuilder.AppendLine(FString::Printf(TEXT("public static class %s"), *FCSEditorUtilities::ReplaceSpecialCharacters(StringTableAsset.AssetName.ToString())));
		ScriptBuilder.OpenBrace();
		ScriptBuilder.AppendLine(FString::Printf(TEXT("private static readonly FName TableId = new(\"%s\");"), *StringTable->GetStringTableId().ToString()));

		for (const FString& Key : Keys)
		{
			ScriptBuilder.AppendLine(FString::Printf(TEXT("public static FText %s => TextLibrary.TextFromStringTable(TableId, \"%s\");"),
			                                         *FCSEditorUtilities::ReplaceSpecialCharacters(Key), *Key.ReplaceCharWithEscapedChar()));
		}

		ScriptBuilder.CloseBrace();
		ScriptBuilder.AppendLine();
	}

	ScriptBuilder.CloseBrace();
	SaveRuntimeGlue(ScriptBuilder, TEXT("StringTables"));
}
