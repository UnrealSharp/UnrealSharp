#include "CSProcessUtilities.h"

#include "CSPathsUtilities.h"
#include "UnrealSharpUtilities.h"
#include "Logging/StructuredLog.h"

bool UnrealSharp::Process::InvokeCommand(const FString& ProgramPath, const FString& Arguments, int32& OutReturnCode, FString& Output, const FString* InWorkingDirectory, const FCSCommandError& OnError)
{
	const double StartTime = FPlatformTime::Seconds();
	const FString ProgramName = FPaths::GetBaseFilename(ProgramPath);
	const FString WorkingDirectory = InWorkingDirectory ? *InWorkingDirectory : FPaths::GetPath(ProgramPath);

	FString ErrorMessage;
	FPlatformProcess::ExecProcess(*ProgramPath, *Arguments, &OutReturnCode, &Output, &ErrorMessage, *WorkingDirectory);

	if (OutReturnCode != 0)
	{
		const FString FullError = FString::Printf(TEXT("%s task failed:\n%s"), *ProgramName, *Output);
		UE_LOGFMT(LogUnrealSharpUtilities, Error, "{0}", FullError);

		if (OnError.IsBound())
		{
			OnError.Execute(FullError);
		}

		return false;
	}

	const double ElapsedTime = FPlatformTime::Seconds() - StartTime;
	UE_LOGFMT(LogUnrealSharpUtilities, Display, "{0} task completed in {1} seconds.", ProgramName, ElapsedTime);
	return true;
}

bool UnrealSharp::Process::InvokeDotNet(const FString& Arguments, const FString* InWorkingDirectory, const FCSCommandError& OnError)
{
	FString Output;
	int32 OutReturnCode = 0;
	return InvokeCommand(Paths::GetDotNetExecutablePath(), Arguments, OutReturnCode, Output, InWorkingDirectory, OnError);
}

bool UnrealSharp::Process::InvokeDotNetBuild(const FString& RootFolder, const FString& AdditionalArguments, const FCSCommandError& OnError)
{
	const FString Args = FString::Printf(TEXT("build \"%s\" %s"), *RootFolder, *AdditionalArguments);
	return InvokeDotNet(Args, nullptr, OnError);
}

bool UnrealSharp::Process::InvokeDotNetBuild(const FCSCommandError& OnError)
{
	return InvokeDotNetBuild(Paths::GetScriptFolderDirectory(), {}, OnError);
}
