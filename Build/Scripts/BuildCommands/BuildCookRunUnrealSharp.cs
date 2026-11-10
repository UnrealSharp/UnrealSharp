using AutomationTool;
using UnrealSharp.Automation.Utilities;

namespace UnrealSharp.Automation.BuildCommands;

[Help("Runs BuildCookRun with managed publishing during staging on engines without CustomStagingHandler.")]
[Help(typeof(ProjectParams))]
public class BuildCookRunUnrealSharp : global::BuildCookRun
{
    public override void ExecuteBuild()
    {
        if (ParseParam("foreign") || ParseParam("foreigncode"))
        {
            throw new AutomationException("BuildCookRunUnrealSharp requires an existing project. Create foreign sample projects with BuildCookRun first.");
        }

        ProjectParams Params = SetupParams();
        Params.ModifyDeploymentContextCallback += UnrealSharpStaging.Stage;
        DoBuildCookRun(Params);
    }
}
