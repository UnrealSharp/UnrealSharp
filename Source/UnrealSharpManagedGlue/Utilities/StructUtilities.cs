using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.PropertyTranslators;

namespace UnrealSharpScriptGenerator.Utilities;

public static class StructUtilities
{
    public static bool IsStructBlittable(this UhtStruct structObj)
    {
        if (PropertyTranslatorManager.SpecialTypeInfo.Structs.BlittableTypes.ContainsKey(structObj.SourceName))
        {
            return true;
        }
        
        // Any struct we haven't manually exported is not blittable, yet.
        // The fix for this is to add a header parser to check for non-UPROPERTY properties in the struct.
        // Because a struct can be recognized as blittable by the reflection data,
        // but have a non-UPROPERTY property that is not picked up by UHT, that makes it not blittable causing a mismatch in memory layout.
        // This is a temporary solution until we can get that working.
        return false;
    }

    public static bool IsStructNativelyCopyable(this UhtStruct structObj)
    {
        return PropertyTranslatorManager.SpecialTypeInfo.Structs.NativelyCopyableTypes.ContainsKey(structObj.SourceName);
    }
    
    public static bool IsStructNativelyDestructible(this UhtStruct structObj)
    {
        return PropertyTranslatorManager.SpecialTypeInfo.Structs.NativelyCopyableTypes.TryGetValue(structObj.SourceName, out var info) && info.HasDestructor;
    }
}