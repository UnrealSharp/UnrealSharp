
using System.Collections.Generic;
using UnrealSharp.Automation.Utilities;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue;

public static class UnrealSharpAutomationUtilities
{
    public static bool InvokeUnrealSharpAutomation(string action, List<KeyValuePair<string, string>>? actionArgs = null)
    {
        return CommandUtilities.RunCommand(action, GeneratorStatics.Factory.Session.ProjectFile!, actionArgs);
    }
}