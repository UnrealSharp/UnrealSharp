using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class ArrayPropertyTranslator : ContainerPropertyTranslator
{
    public ArrayPropertyTranslator()
        : base("ArrayCopyMarshaller",
            "ArrayReadOnlyMarshaller",
            "ArrayMarshaller",
            "IReadOnlyList",
            "IList")
    {
    }
}