namespace UnrealSharpBuildTool.Exceptions;

public class BuildToolException : Exception
{
    public BuildToolException(string message) : base(message) { }
    public BuildToolException(string message, Exception inner) : base(message, inner) { }
}