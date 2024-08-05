namespace UnrealSharp.Attributes;

/// <summary>
/// Used to mark a type as generated. Don't use this attribute in your code.
/// It's public since glue for user code is generated in the user's project.
/// </summary>
public class GeneratedTypeAttribute : Attribute
{
    public GeneratedTypeAttribute(string engineName, string fullName = "")
    {
        EngineName = engineName;
        FullName = fullName;
    }
    
    public string EngineName { get; set; }
    public string FullName { get; set; }
}