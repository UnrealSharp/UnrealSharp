using UnrealSharp.Interop;

namespace UnrealSharp.Logging;

public static class UnrealLogger
{
    public static void Log(string logName, string message, ELogVerbosity logVerbosity = ELogVerbosity.Display)
    {
        unsafe
        {
            fixed (char* stringPtr = message)
            {
                FMsgExporter.CallLog(new FName(logName), logVerbosity, stringPtr);
            }
        }
    }
    
    public static void LogWarning(string logName, string message)
    {
        Log(logName, message, ELogVerbosity.Warning);
    }
    
    public static void LogError(string logName, string message)
    {
        Log(logName, message, ELogVerbosity.Error);
    }
    
    public static void LogFatal(string logName, string message)
    {
        Log(logName, message, ELogVerbosity.Fatal);
    }
    
    public static void LogVerbose(string logName, string message)
    {
        Log(logName, message, ELogVerbosity.Verbose);
    }
    
    public static void LogVeryVerbose(string logName, string message)
    {
        Log(logName, message, ELogVerbosity.VeryVerbose);
    }
}