using UnrealSharp.Interop;

namespace UnrealSharp.GameplayTags;

public partial struct FGameplayTag
{
    private FName _tagName;
    
    /// <summary>
    /// Determine if this tag matches TagToCheck, expanding our parent tags
    /// "A.1".MatchesTag("A") will return True, "A".MatchesTag("A.1") will return False
    /// If TagToCheck is not Valid it will always return False
    /// </summary>
    /// <returns>True if this tag matches TagToCheck</returns>
    public bool MatchesTag(FGameplayTag tagToCheck)
    {
        return FGameplayTagExporter.CallMatchesTag(ref _tagName, ref tagToCheck._tagName).ToManagedBool();
    }
    
    /// <summary>
    /// Determine if TagToCheck is valid and exactly matches this tag
    /// "A.1".MatchesTagExact("A") will return False
    /// If TagToCheck is not Valid it will always return False
    /// </summary>
    /// <returns>True if TagToCheck is Valid and is exactly this tag</returns>
    public bool MatchesTagExact(FGameplayTag tagToCheck)
    {
        return _tagName == tagToCheck._tagName;
    }
    
    /// <summary>
    /// Check to see how closely two FGameplayTags match. Higher values indicate more matching terms in the tags.
    /// </summary>
    /// <returns>The depth of the match, higher means they are closer to an exact match</returns>
    public bool MatchesTagDepth(FGameplayTag tagToCheck)
    {
        return FGameplayTagExporter.CallMatchesTagDepth(ref _tagName, ref tagToCheck._tagName).ToManagedBool();
    }
    
    /// <summary>
    /// Checks if this tag matches ANY of the tags in the specified container, also checks against our parent tags
    /// "A.1".MatchesAny({"A","B"}) will return True, "A".MatchesAny({"A.1","B"}) will return False
    /// If ContainerToCheck is empty/invalid it will always return False
    /// </summary>
    /// <returns>True if this tag matches ANY of the tags of in ContainerToCheck</returns>
    public bool MatchesAny(GameplayTagContainer tagContainer)
    {
        return FGameplayTagExporter.CallMatchesAny(ref _tagName, ref tagContainer).ToManagedBool();
    }
    
    /// <summary>
    /// Checks if this tag matches ANY of the tags in the specified container, only allowing exact matches
    /// "A.1".MatchesAny({"A","B"}) will return False
    /// If ContainerToCheck is empty/invalid it will always return False
    /// </summary>
    /// <returns>True if this tag matches ANY of the tags of in ContainerToCheck exactly</returns>
    public bool MatchesAnyDepth(GameplayTagContainer tagContainer)
    {
        return FGameplayTagExporter.CallMatchesAnyExact(ref _tagName, ref tagContainer).ToManagedBool();
    }
    
    public bool Equals(FGameplayTag other)
    {
        return _tagName.Equals(other._tagName);
    }

    public override bool Equals(object? obj)
    {
        return obj is FGameplayTag other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _tagName.GetHashCode();
    }

    public override string ToString()
    {
        return _tagName.ToString();
    }
    
    public static bool operator == (FGameplayTag lhs, FGameplayTag rhs)
    {
        return lhs._tagName == rhs._tagName;
    }

    public static bool operator !=(FGameplayTag lhs, FGameplayTag rhs)
    {
        return !(lhs == rhs);
    }
}