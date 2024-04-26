namespace UnrealSharp.Attributes;

[AttributeUsage(AttributeTargets.Interface)]
public sealed class UInterfaceAttribute : BaseUAttribute
{
    /// <summary>
    /// If true, the interface cannot be implemented in a blueprint.
    /// </summary>
    public bool CannotImplementInterfaceInBlueprint = false;
}
