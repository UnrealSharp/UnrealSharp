using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AutomationTool;
using EpicGames.Core;
using UnrealBuildBase;
using UnrealBuildTool;

namespace UnrealSharp.Automation.Utilities;

public static class PackageArchiveUtilities
{
    public static string GetProjectDirectory(string archiveDirectory, FileReference project, string targetName,
        TargetType targetType, UnrealTargetPlatform platform, UnrealTargetConfiguration configuration, UnrealArch architecture)
    {
        if (platform != UnrealTargetPlatform.Win64 && platform != UnrealTargetPlatform.Mac)
        {
            throw new AutomationException("Archive directory resolution supports Windows and Mac archives.");
        }
        if (!Directory.Exists(archiveDirectory))
        {
            throw new DirectoryNotFoundException($"Archive directory does not exist: {archiveDirectory}");
        }

        UnrealArchitectures Architectures = new(architecture);
        FileReference ReceiptPath = TargetReceipt.GetDefaultPath(project.Directory, targetName, platform, configuration, Architectures);
        if (!FileReference.Exists(ReceiptPath))
        {
            ReceiptPath = TargetReceipt.GetDefaultPath(Unreal.EngineDirectory, targetName, platform, configuration, Architectures);
        }
        TargetReceipt Receipt = TargetReceipt.Read(ReceiptPath);
        if (Receipt.TargetName != targetName || Receipt.TargetType != targetType || Receipt.Platform != platform ||
            Receipt.Configuration != configuration || Receipt.Architectures.Architectures.Count != 1 ||
            Receipt.Architectures.SingleArchitecture != architecture ||
            (Receipt.ProjectFile != null && Receipt.ProjectFile != project) || Receipt.Launch == null)
        {
            throw new AutomationException($"The build receipt '{ReceiptPath}' does not match the selected project, target, platform, configuration and single architecture.");
        }

        string ProjectName = project.GetFileNameWithoutExtension();
        string ProjectDirectory;
        if (platform == UnrealTargetPlatform.Mac)
        {
            ProjectDirectory = GetMacProjectDirectory(archiveDirectory, ProjectName, Receipt.Launch.GetFileName(), architecture);
        }
        else
        {
            ProjectDirectory = Path.Combine(archiveDirectory, ProjectName);
            // UAT stages the receipt's executable under the project or Engine directory.
            // The root bootstrap launcher may have a different, undecorated name or be disabled.
            string ExecutablePath = Receipt.Launch.IsUnderDirectory(project.Directory)
                ? Path.Combine(ProjectDirectory, Receipt.Launch.MakeRelativeTo(project.Directory))
                : Path.Combine(archiveDirectory, Receipt.Launch.MakeRelativeTo(Unreal.RootDirectory));
            if (!File.Exists(ExecutablePath))
            {
                throw new AutomationException($"The archive does not contain the selected target's executable: {ExecutablePath}");
            }
        }
        if (!Directory.Exists(ProjectDirectory))
        {
            throw new DirectoryNotFoundException($"The staged project directory does not exist: {ProjectDirectory}");
        }
        return ProjectDirectory;
    }

    private static string GetMacProjectDirectory(string archiveDirectory, string projectName, string executableName, UnrealArch architecture)
    {
        if (!OperatingSystem.IsMacOS())
        {
            throw new AutomationException("Mac archive validation must run on macOS.");
        }
        string[] Apps = archiveDirectory.EndsWith(".app", StringComparison.OrdinalIgnoreCase)
            ? [archiveDirectory] : Directory.GetDirectories(archiveDirectory, "*.app", SearchOption.TopDirectoryOnly);
        string[] Matches = Apps.Where(app =>
        {
            string PlistPath = Path.Combine(app, "Contents", "Info.plist");
            return File.Exists(PlistPath) && Directory.Exists(Path.Combine(app, "Contents", "UE", projectName)) &&
                PListHelper.CreateFromFile(PlistPath).GetString("CFBundleExecutable", out string BundleExecutable) &&
                BundleExecutable == executableName && File.Exists(Path.Combine(app, "Contents", "MacOS", BundleExecutable));
        }).ToArray();
        if (Matches.Length != 1)
        {
            throw new AutomationException($"Expected one Mac application for executable '{executableName}' and project '{projectName}', found {Matches.Length}. Select the matching application or its archive root.");
        }

        ProcessStartInfo StartInfo = new("/usr/bin/lipo")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        StartInfo.ArgumentList.Add("-archs");
        StartInfo.ArgumentList.Add(Path.Combine(Matches[0], "Contents", "MacOS", executableName));
        using Process Process = System.Diagnostics.Process.Start(StartInfo)!;
        string NativeArchitectures = Process.StandardOutput.ReadToEnd().Trim();
        string Error = Process.StandardError.ReadToEnd();
        Process.WaitForExit();
        string ExpectedArchitecture = architecture == UnrealArch.X64 ? "x86_64" : architecture.ToString();
        if (Process.ExitCode != 0 || NativeArchitectures != ExpectedArchitecture)
        {
            throw new AutomationException($"Archive architecture '{NativeArchitectures}' does not match '{ExpectedArchitecture}'. Select a matching single architecture. {Error}");
        }
        return Path.Combine(Matches[0], "Contents", "UE", projectName);
    }
}
