using AutomationTool;
using Microsoft.Extensions.Logging;

namespace UnrealSharp.Automation.Utilities;

public static class LoggerUtilities
{
    private const string LogPrefix = "[UnrealSharp] ";

    public static void LogUnrealSharpInfo(string message) => CommandUtils.Logger.LogInformation(LogPrefix + message);
    public static void LogUnrealSharpWarning(string message) => CommandUtils.Logger.LogWarning(LogPrefix + message);
    public static void LogUnrealSharpError(string message) => CommandUtils.Logger.LogError(LogPrefix + message);
}