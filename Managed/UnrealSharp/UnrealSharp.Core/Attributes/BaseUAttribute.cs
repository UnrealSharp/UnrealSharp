namespace UnrealSharp.Attributes;

public class BaseUAttribute : Attribute
{
    /// <summary>
    /// The display name of the type, used in the editor.
    /// </summary>
    public string DisplayName = "";
    
    /// <summary>
    /// The category of the type, used in the editor.
    /// </summary> 
    public string Category = "";
}