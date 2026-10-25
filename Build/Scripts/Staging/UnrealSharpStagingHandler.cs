using System.Linq;
using AutomationTool;
using UnrealSharp.Automation.Utilities;

namespace UnrealSharp.Automation.Staging;

public class UnrealSharpStagingHandler : CustomStagingHandler
{
    protected override bool TryInitialize(ProjectParams Params, DeploymentContext SC)
    {
        // Automation modules can be discovered even when their plugin is disabled.
        // The native receipt identifies the plugins actually used by this target.
        return SC.StageTargets.Any(target => UnrealSharpStaging.UsesUnrealSharp(target.Receipt));
    }

    public override void GetFilesToStage(ProjectParams Params, DeploymentContext SC)
    {
        UnrealSharpStaging.Stage(Params, SC);
    }

    public override void GetFilesToStageForDLC(ProjectParams Params, DeploymentContext SC)
    {
        UnrealSharpStaging.Stage(Params, SC);
    }
}
