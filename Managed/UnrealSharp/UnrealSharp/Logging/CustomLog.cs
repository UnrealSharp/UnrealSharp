namespace UnrealSharp.Logging;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class CustomLog(ELogVerbosity verbosity = ELogVerbosity.Display) : Attribute
{
    private ELogVerbosity _verbosity = verbosity;
}