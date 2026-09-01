using System.IO;
using UnrealSharp.Automation.Utilities;

namespace UnrealSharp.Automation.Processes;

public class DotnetProcess : BuildToolProcess
{
    public DotnetProcess() : base(DotNetUtilities.DotNetExecutable)
    {
        string LatestDotNetSdkPath = DotNetUtilities.LatestDotNetSdkPath;
        StartInfo.ArgumentList.Add(Path.Combine(LatestDotNetSdkPath, "dotnet.dll"));
        StartInfo.Environment["MSBuildExtensionsPath"] = LatestDotNetSdkPath;
        StartInfo.Environment["MSBUILD_EXE_PATH"] = Path.Combine(LatestDotNetSdkPath, "MSBuild.dll");
        StartInfo.Environment["MSBuildSDKsPath"] = Path.Combine(LatestDotNetSdkPath, "Sdks");
        StartInfo.Environment["DOTNET_ROLL_FORWARD"] = "LatestMinor";
    }
}
