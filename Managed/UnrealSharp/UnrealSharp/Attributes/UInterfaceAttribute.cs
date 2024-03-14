namespace UnrealSharp.Attributes;

[AttributeUsage(AttributeTargets.Interface)]
public sealed class UInterfaceAttribute : BaseUAttribute
{
    public bool CannotImplementInterfaceInBlueprint = false;
}
