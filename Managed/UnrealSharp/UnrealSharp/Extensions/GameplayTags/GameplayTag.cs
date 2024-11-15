using UnrealSharp.UnrealSharpCore;

namespace UnrealSharp.GameplayTags;

public partial struct FGameplayTag
{
    public FGameplayTag(FName tagName)
    {
        this = UCSGameplayTagExtensions.RequestGameplayTag(tagName);
        if (!IsValid)
        {
            throw new Exception($"Failed to create GameplayTag with name {tagName}");
        }
    }
    
    public FGameplayTag(string tagName) : this(new FName(tagName)) {}
    
    /// <summary>
    /// Returns empty GameplayTag
    /// </summary>
    public static FGameplayTag None => new(FName.None);
    
    /// <summary>
    /// Check if this tag is exactly the same as TagToCheck
    /// </summary>
    /// <param name="tagToCheck">The tag to check against</param>
    /// <returns>True if this tag matches TagToCheck</returns>
    public bool MatchesTagExact(FGameplayTag tagToCheck) 
    {
        return TagName == tagToCheck.TagName;
    }
    
    /// <summary>
    /// Is tag valid?
    /// </summary>
    /// <returns>True if tag is valid</returns>
    public bool IsValid => TagName.IsValid;
    
    public bool Equals(FGameplayTag other)
    {
        return TagName.Equals(other.TagName);
    }

    public override bool Equals(object? obj)
    {
        return obj is FGameplayTag other && Equals(other);
    }

    public override int GetHashCode()
    {
        return TagName.GetHashCode();
    }

    public override string ToString()
    {
        return TagName.ToString();
    }
    
    public static bool operator == (FGameplayTag lhs, FGameplayTag rhs)
    {
        return lhs.TagName == rhs.TagName;
    }

    public static bool operator !=(FGameplayTag lhs, FGameplayTag rhs)
    {
        return !(lhs == rhs);
    }
}