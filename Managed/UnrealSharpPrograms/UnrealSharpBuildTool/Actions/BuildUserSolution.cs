using System.Collections.ObjectModel;

namespace UnrealSharpBuildTool.Actions;

public class BuildUserSolution : BuildSolution
{
    public BuildUserSolution(Collection<string>? extraArguments = null, BuildConfig buildConfig = BuildConfig.Debug) 
        : base(Program.GetScriptFolder(), extraArguments, buildConfig)
    {
        
    }
}