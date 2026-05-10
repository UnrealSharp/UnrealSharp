#pragma once

DECLARE_DELEGATE_OneParam(FCSCommandError, const FString&);

namespace UnrealSharp::Process
{
	UNREALSHARPUTILITIES_API bool InvokeCommand(const FString& ProgramPath, const FString& Arguments, int32& OutReturnCode, FString& Output, const FString* InWorkingDirectory = nullptr, const FCSCommandError& OnError = {});
	UNREALSHARPUTILITIES_API bool InvokeDotNet(const FString& Arguments, const FString* InWorkingDirectory = nullptr, const FCSCommandError& OnError = {});
	UNREALSHARPUTILITIES_API bool InvokeDotNetBuild(const FString& RootFolder, const FString& AdditionalArguments = {}, const FCSCommandError& OnError = {});
	UNREALSHARPUTILITIES_API bool InvokeDotNetBuild(const FCSCommandError& OnError = {});
}
