using System.Text;

namespace UnrealSharp.Core;

/// <summary>An owned UTF-8 value. Copies share immutable managed storage, never native memory.</summary>
public readonly struct FUtf8String : IEquatable<FUtf8String>
{
    private readonly byte[]? _bytes;

    public FUtf8String(string? value)
    {
        _bytes = string.IsNullOrEmpty(value) ? null : Encoding.UTF8.GetBytes(value);
    }

    public FUtf8String(ReadOnlySpan<byte> utf8)
    {
        _bytes = utf8.ToArray();
    }

    public ReadOnlySpan<byte> Bytes => _bytes;
    public int Length => Bytes.Length;
    public bool IsEmpty => Bytes.IsEmpty;
    public override string ToString() => Encoding.UTF8.GetString(Bytes);
    public bool Equals(FUtf8String other) => Bytes.SequenceEqual(other.Bytes);
    public override bool Equals(object? obj) => obj is FUtf8String other && Equals(other);
    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.AddBytes(Bytes);
        return hash.ToHashCode();
    }
    public static bool operator ==(FUtf8String left, FUtf8String right) => left.Equals(right);
    public static bool operator !=(FUtf8String left, FUtf8String right) => !left.Equals(right);
}
