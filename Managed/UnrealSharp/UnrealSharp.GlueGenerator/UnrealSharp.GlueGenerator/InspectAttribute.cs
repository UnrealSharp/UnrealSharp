using System;

namespace UnrealSharp.GlueGenerator;

public class Inspector : Attribute;
public class InspectAttribute : Attribute
{
    public InspectAttribute(string fullyQualifiedAttributeName = "", string name = "", string scope = "")
    {
        Scope = scope;
        FullyQualifiedAttributeName = fullyQualifiedAttributeName;
        Name = name;
    }

    public string FullyQualifiedAttributeName;
    public string Name;

    public string[] Names => [FullyQualifiedAttributeName, Name];
    public readonly string Scope;
}

public class InspectArgumentAttribute : Attribute
{
    public InspectArgumentAttribute(string[] specifierNames, params string[] attributeNames)
    {
        SpecifierNames = specifierNames;
        AttributeNames = attributeNames;
    }
    
    public InspectArgumentAttribute(string specifierNames, params string[] attributeNames)
    {
        SpecifierNames = new[] { specifierNames };
        AttributeNames = attributeNames;
    }

    public readonly string[] SpecifierNames;
    public readonly string[] AttributeNames;
}