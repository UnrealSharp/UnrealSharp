#pragma once

#include "CoreMinimal.h"

class FCSGlueGeneratorFileManager
{
public:
	~FCSGlueGeneratorFileManager();

	/** Saves generated script glue to a temporary file if its contents is different from the existing one. */
	void SaveFileIfChanged(const FString& FilePath, const FString& NewFileContents);
	/** Renames/replaces all existing script glue files with the temporary (new) ones */
	void RenameTempFiles();

private:
	/** List of temporary files crated by SaveFileIfChanged */
	TArray<FString> TempFiles;

};