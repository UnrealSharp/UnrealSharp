using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutomationTool;
using UnrealBuildTool;
using UnrealSharp.Automation.Utilities;

namespace UnrealSharp.Automation.BuildCommands;

[Help("Packages the UnrealSharp managed code into an Unreal Engine packaged build.")]
[Help("ArchiveDirectory=<Path>", "REQUIRED. The base directory containing the packaged game.")]
[Help("UETargetType=<TargetType>", "REQUIRED. The Unreal Engine target type (Editor, Game, Client, Server).")]
[Help("UEBuildConfig=<Config>", "REQUIRED. The build configuration (Debug, Development, Shipping, Test).")]
[Help("TargetPlatform=<Platform>", "Optional. Target platform. Defaults to Win64.")]
[Help("TargetArchitecture=<Arch>", "Optional. Target architecture. Defaults to X64.")]
[Help("NativeAOT", "Optional flag. Enables Native AOT compilation for the user solution.")]
public class PackageProject : BuildCommand
{
    private const string ManagedFolderName = "Managed";
    private const string BindingsProjectFolder = "UnrealSharp";
    
    private static readonly string[] CommonMsBuildProperties =
    [
        "-p:DisableWithEditor=true",
        "-p:GenerateDocumentationFile=false",
    ];
    
    public sealed record PackagingOptions(
        string ArchiveDirectory,
        string TargetType,
        UnrealTargetConfiguration BuildConfiguration,
        UnrealTargetPlatform TargetPlatform,
        UnrealArch TargetArchitecture,
        bool NativeAot);

    public override void ExecuteBuild()
    {
        PackagingOptions Options = ParseOptionsFromCommandLine();
        StartPackaging(Options);
    }
    
    public void StartPackaging(PackagingOptions options)
    {
        ValidateOptions(options);
        LogOptions(options);

        DotNetSdkUtilities.CopyGlobalJson(this);

        string RuntimeIdentifier = DotNetSdkUtilities.GetDotNetRuntimeIdentifier(options.TargetPlatform, options.TargetArchitecture);
        string BuildOutput = BuildUtilities.GetIntermediateBuildPathForPlatform(this, options.TargetArchitecture, options.TargetPlatform, options.BuildConfiguration);
        string PublishFolder = PathUtilities.GetOutputPath(Path.Combine(options.ArchiveDirectory, this.GetProjectName()));

        CleanBuildArtifacts(BuildOutput);
        CleanBuildArtifacts(PublishFolder);

        IReadOnlyList<string> BaseArguments = BuildBaseArguments(RuntimeIdentifier, options, BuildOutput, PublishFolder);

        BuildBindingsSolution(BaseArguments, options.BuildConfiguration);
        BuildUserSolution(BaseArguments, options.BuildConfiguration, options.NativeAot);

        EmitLoadOrder(PublishFolder);

        LoggerUtilities.LogUnrealSharpInfo($"Packaging complete. Published files: {PublishFolder}");
    }

    private PackagingOptions ParseOptionsFromCommandLine()
    {
        string ArchiveDirectory = ParseRequiredStringParam("ArchiveDirectory");
        string TargetType = ParseRequiredStringParam("UETargetType");
        UnrealTargetConfiguration BuildConfig = ParseRequiredEnumParamEnum<UnrealTargetConfiguration>("UEBuildConfig");

        string? PlatformString = ParseOptionalStringParam("TargetPlatform");
        UnrealTargetPlatform TargetPlatform = string.IsNullOrEmpty(PlatformString) ? UnrealTargetPlatform.Win64 : UnrealTargetPlatform.Parse(PlatformString);

        string? ArchString = ParseOptionalStringParam("TargetArchitecture");
        UnrealArch TargetArchitecture = string.IsNullOrEmpty(ArchString) ? UnrealArch.X64 : UnrealArch.Parse(ArchString);

        bool NativeAot = ParseParam("NativeAOT");

        return new PackagingOptions(ArchiveDirectory, TargetType, BuildConfig, TargetPlatform, TargetArchitecture, NativeAot);
    }

    private static void ValidateOptions(PackagingOptions options)
    {
        ArgumentException.ThrowIfNullOrEmpty(options.ArchiveDirectory);
        ArgumentException.ThrowIfNullOrEmpty(options.TargetType);

        if (!Directory.Exists(options.ArchiveDirectory))
        {
            throw new DirectoryNotFoundException($"Archive directory does not exist: {options.ArchiveDirectory}");
        }

        ValidatePlatformArchitecture(options.TargetPlatform, options.TargetArchitecture);
    }
    
    private static void ValidatePlatformArchitecture(UnrealTargetPlatform platform, UnrealArch architecture)
    {
        if (platform == UnrealTargetPlatform.LinuxArm64 && architecture != UnrealArch.Arm64)
        {
            throw new ArgumentException($"Platform '{platform}' requires architecture '{UnrealArch.Arm64}', " + $"but '{architecture}' was specified.");
        }
    }

    private static void LogOptions(PackagingOptions options)
    {
        LoggerUtilities.LogUnrealSharpInfo("Packaging project with parameters:");
        LoggerUtilities.LogUnrealSharpInfo($"Archive Directory: {options.ArchiveDirectory}");
        LoggerUtilities.LogUnrealSharpInfo($"Target Platform: {options.TargetPlatform}");
        LoggerUtilities.LogUnrealSharpInfo($"Target Architecture: {options.TargetArchitecture}");
        LoggerUtilities.LogUnrealSharpInfo($"UE Build Configuration: {options.BuildConfiguration}");
        LoggerUtilities.LogUnrealSharpInfo($"UE Target Type: {options.TargetType}");
        LoggerUtilities.LogUnrealSharpInfo($"Native AOT: {options.NativeAot}");
    }

    private static void CleanBuildArtifacts(string buildOutput)
    {
        if (!Directory.Exists(buildOutput))
        {
            return;
        }

        LoggerUtilities.LogUnrealSharpInfo($"Cleaning existing build output at '{buildOutput}'.");
        
        try
        {
            Directory.Delete(buildOutput, recursive: true);
        }
        catch (Exception Ex)
        {
            throw new IOException($"Failed to clean existing build output at '{buildOutput}'. See inner exception for details.", Ex);
        }
    }

    private static IReadOnlyList<string> BuildBaseArguments(string runtimeIdentifier, PackagingOptions options, string buildOutput, string publishFolder)
    {
        return
        [
            "--runtime", runtimeIdentifier,

            .. CommonMsBuildProperties,

            $"-p:UETargetType={options.TargetType}",
            $"-p:UEBuildConfig={options.BuildConfiguration}",

            $"-p:PublishDir=\"{publishFolder}\"",
            $"-p:OutputPath=\"{buildOutput}\"",
        ];
    }

    private void BuildBindingsSolution(IReadOnlyList<string> baseArguments, UnrealTargetConfiguration buildConfig)
    {
        string BindingsPath = Path.Combine(this.GetUnrealSharpRootFolder(), ManagedFolderName, BindingsProjectFolder);
        BuildCommands.BuildSolution.RunBuild(BindingsPath, buildConfig, publish: true, [.. baseArguments]);
    }

    private void BuildUserSolution(IReadOnlyList<string> baseArguments, UnrealTargetConfiguration buildConfig, bool nativeAot)
    {
        List<string> UserSolutionArguments = new List<string>(baseArguments);

        if (!nativeAot)
        {
            UserSolutionArguments.Add("--self-contained");
        }

        string ScriptFolder = this.GetProjectScriptFolder();
        BuildCommands.BuildSolution.RunBuild(ScriptFolder, buildConfig, publish: true, UserSolutionArguments);
    }

    private void EmitLoadOrder(string publishFolder)
    {
        List<FileInfo> RuntimeProjectFiles = this.GetUnrealSharpProjectFiles()
            .Where(file => !ProjectUtilities.IsEditorOnlyProject(file.FullName))
            .ToList();

        if (RuntimeProjectFiles.Count == 0)
        {
            LoggerUtilities.LogUnrealSharpInfo("No runtime projects found. Skipping load order emission.");
            return;
        }

        LoggerUtilities.LogUnrealSharpInfo($"Emitting load order for {RuntimeProjectFiles.Count} runtime project(s).");
        
        BuildEmitLoadOrder.EmitLoadOrder(RuntimeProjectFiles, publishFolder, publishFolder);
    }
}