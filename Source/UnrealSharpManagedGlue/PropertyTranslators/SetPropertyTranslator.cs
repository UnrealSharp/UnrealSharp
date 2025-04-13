namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class SetPropertyTranslator : ContainerPropertyTranslator
{
    public SetPropertyTranslator()
        : base("SetCopyMarshaller", 
            "SetReadOnlyMarshaller", 
            "SetMarshaller", 
            "IReadOnlySet", 
            "ISet")
    {
    }
}