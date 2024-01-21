#include "CSGlueGeneratorFileManager.h"
#include "GlueGeneratorModule.h"
#include "Misc/FileHelper.h"

void FCSGlueGeneratorFileManager::SaveFileIfChanged(const FString& FilePath, const FString& NewFileContents)
{
	FString OriginalFileContents;
	bool bFileExists = FFileHelper::LoadFileToString(OriginalFileContents, *FilePath);

	if (!bFileExists || OriginalFileContents != NewFileContents)
	{
		FFileHelper::SaveStringToFile(NewFileContents, *FilePath);
	}
}

void FCSGlueGeneratorFileManager::RenameTempFiles()
{
	for (const auto& TempFilename : TempFiles)
	{
		FString Filename = TempFilename.Replace(TEXT(".tmp"), TEXT(""));
		
		if (!IFileManager::Get().Move(*Filename, *TempFilename, true, true))
		{
			UE_LOG(LogGlueGenerator, Error, TEXT("Couldn't write file '%s'"), *Filename);
		}
		else
		{
			UE_LOG(LogGlueGenerator, Log, TEXT("Exported updated script glue: %s"), *Filename);
		}
	}
	
	TempFiles.Empty();
	
}




