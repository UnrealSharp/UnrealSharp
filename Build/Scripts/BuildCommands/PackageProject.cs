using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutomationTool;
using UnrealBuildTool;
using UnrealSharp.Automation.Processes;
using UnrealSharp.Automation.Utilities;

namespace UnrealSharp.Automation.BuildCommands;

[Help("Packages the project. This will create a self-contained package in an archived directory.")]
[Help("ArchiveDirectory=<Path>", "The base directory where the packaged game is located.")]
[Help("TargetPlatform=<Platform>", "The target platform for the package.")]
[Help("TargetArchitecture=<Arch>", "The target architecture for the package.")]
[Help("UEBuildConfig=<Config>", "The build configuration for the package (Debug, Development, Shipping, etc.).")]
[Help("NativeAOT", "Enable Native AOT compilation. Will overwrite the BuildConfig to Release if set to true.")]
[Help("UETargetType=<TargetType>", "The type of Unreal Engine target (Editor, Game, etc.). Required for the Publish action.")]
public class PackageProject : BuildCommand
{
    public override void ExecuteBuild()
    {
        string ArchiveDirectory = ParseRequiredStringParam("ArchiveDirectory");

        string? PlatformString = ParseOptionalStringParam("TargetPlatform");
        UnrealTargetPlatform TargetPlatform = string.IsNullOrEmpty(PlatformString) ? UnrealTargetPlatform.Win64 : UnrealTargetPlatform.Parse(PlatformString);

        string? ArchString = ParseOptionalStringParam("TargetArchitecture");
        UnrealArch TargetArchitecture = string.IsNullOrEmpty(ArchString) ? UnrealArch.X64 : UnrealArch.Parse(ArchString);

        UnrealTargetConfiguration UeBuildConfig = ParseRequiredEnumParamEnum<UnrealTargetConfiguration>("UEBuildConfig");
        bool NativeAot = ParseParam("NativeAOT");
        string UeTargetType = ParseRequiredStringParam("UETargetType");

        StartPackaging(ArchiveDirectory, TargetPlatform, TargetArchitecture, UeBuildConfig, NativeAot, UeTargetType);
    }

    public void StartPackaging(string archiveDirectory, UnrealTargetPlatform targetPlatform, UnrealArch targetArchitecture, UnrealTargetConfiguration ueBuildConfig, bool nativeAot, string ueTargetType)
    {
        if (!Directory.Exists(archiveDirectory))
        {
            throw new DirectoryNotFoundException(archiveDirectory);
        }

        DotNetSdkUtilities.CopyGlobalJson(this);

        if (string.IsNullOrEmpty(ueTargetType))
        {
            throw new Exception("UETargetType argument is required for the Publish action.");
        }

        string BindingsPath = Path.Combine(this.GetUnrealSharpRootFolder(), "Managed", "UnrealSharp");
        string BuildOutput = BuildUtilities.GetIntermediateBuildPathForPlatform(this, targetArchitecture, targetPlatform, ueBuildConfig);
        string RootProjectPath = Path.Combine(archiveDirectory, this.GetProjectName());
        string PublishFolder = PathUtilities.GetOutputPath(RootProjectPath);

        List<string> ExtraArguments = new List<string>
        {
            "--self-contained",

            "--runtime",
            "win-x64",

            "-p:DisableWithEditor=true",
            "-p:GenerateDocumentationFile=false",

            $"-p:UETargetType={ueTargetType}",
            $"-p:UEBuildConfig={ueBuildConfig}",

            $"-p:PublishDir=\"{PublishFolder}\"",
            $"-p:OutputPath=\"{BuildOutput}\"",
        };

        if (!nativeAot)
        {
            ExtraArguments.Add("--self-contained");
        }

        List<string> Folders = new List<string> { BindingsPath, this.GetProjectScriptFolder() };
        
        foreach (string SolutionFolder in Folders)
        {
            if (!Directory.Exists(SolutionFolder))
            {
                throw new Exception($"Specified solution folder does not exist: {SolutionFolder}");
            }

            DotnetProcess PublishProcess = new DotnetProcess();
            PublishProcess.StartInfo.ArgumentList.Add("publish");
            PublishProcess.StartInfo.ArgumentList.Add($"\"{SolutionFolder}\"");
            PublishProcess.StartInfo.ArgumentList.Add("--configuration");
            PublishProcess.StartInfo.ArgumentList.Add(ueBuildConfig.GetDotNetBuildConfiguration());

            foreach (string ExtraArgument in ExtraArguments)
            {
                PublishProcess.StartInfo.ArgumentList.Add(ExtraArgument);
            }

            PublishProcess.StartBuildToolProcess();
        }

        List<FileInfo> ProjectFiles = this.GetUnrealSharpProjectFiles()
            .Where(file => !ProjectUtilities.IsEditorOnlyProject(file.FullName)).ToList();
        
        BuildEmitLoadOrder.EmitLoadOrder(ProjectFiles, PublishFolder, PublishFolder);
    }
}
