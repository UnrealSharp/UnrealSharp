#pragma once

class FCSGlueGeneratorFileManager
{
public:
	
	/** Saves generated script glue to a temporary file if its contents is different from the existing one. */
	static void SaveFileIfChanged(const FString& FilePath, const FString& NewFileContents);
	
	/** Renames/replaces all existing script glue files with the temporary (new) ones */
	void RenameTempFiles();

private:
	
	/** List of temporary files crated by SaveFileIfChanged */
	TArray<FString> TempFiles;

};