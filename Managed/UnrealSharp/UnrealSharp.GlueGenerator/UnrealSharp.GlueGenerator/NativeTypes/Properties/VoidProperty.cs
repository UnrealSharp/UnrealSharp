namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record VoidProperty : UnrealProperty
{
    public const string VoidTypeName = "Void";
    public const string VoidSourceName = "void";
    
    public VoidProperty(UnrealType outer) : base(PropertyType.Unknown, outer)
    {
        ManagedType = VoidSourceName;
    }
}