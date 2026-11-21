using System;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.SourceGeneration;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue.PropertyTranslators;

public class SubclassOfPropertyTranslator : ObjectContainerPropertyTranslator
{
	public SubclassOfPropertyTranslator() : base(typeof(UhtClassProperty), "TSubclassOf", "SubclassOfMarshaller")
	{
	}

	protected override UhtClass GetMetaClass(UhtObjectPropertyBase property)
	{
		UhtClassProperty classProperty = (UhtClassProperty)property;
		return classProperty.MetaClass!;
	}

#if UE_5_9_OR_LATER
    public override bool ExportDefaultParameter => false;

    public override bool CanExportDefaultValue(UhtFunction function, UhtProperty property, string value)
        => GeneratorStatics.Factory.Session.FindType(null,
            UhtFindOptions.Class | UhtFindOptions.EngineName | UhtFindOptions.PathName, value) is UhtClass;

    public override void ExportCppDefaultParameterAsLocalVariable(GeneratorStringBuilder builder, string variableName,
        string defaultValue, UhtFunction function, UhtProperty paramProperty)
    {
        UhtClass defaultClass = GeneratorStatics.Factory.Session.FindType(null,
            UhtFindOptions.Class | UhtFindOptions.EngineName | UhtFindOptions.PathName, defaultValue) as UhtClass
            ?? throw new InvalidOperationException($"Unable to resolve default class '{defaultValue}'.");
        string managedType = GetManagedType(paramProperty);
        builder.AppendLine($"{managedType} {variableName} = new {managedType}(typeof({defaultClass.GetFullManagedName()}));");
    }
#endif
}