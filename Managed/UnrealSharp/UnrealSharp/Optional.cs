using UnrealSharp.Core;
using UnrealSharp.Core.Attributes;
using UnrealSharp.Core.Marshallers;
using UnrealSharp.Interop;

namespace UnrealSharp;

public static class Optional
{
    public static TOptional<T> Of<T>(T value)
    {
        return new TOptional<T>(value);
    }

    public static TOptional<T> OfNullable<T>(T? value) where T : class
    {
        return value != null ? new TOptional<T>(value) : Empty<T>();
    }

    public static TOptional<T> OfNullable<T>(T? value) where T : struct
    {
        return value.HasValue ? new TOptional<T>(value.Value) : Empty<T>();
    }

    public static TOptional<T> Empty<T>()
    {
        return new TOptional<T>();
    }
}

[Binding]
public struct TOptional<T>
{
    private readonly T? _value;

    public bool IsSet { get; }

    public TOptional()
    {
        _value = default;
        IsSet = false;
    }

    internal TOptional(T value)
    {
        _value = value;
        IsSet = true;
    }

    public T Get(T defaultValue)
    {
        return IsSet ? _value! : defaultValue;
    }

    public T? GetOrDefault()
    {
        return IsSet ? _value : default;
    }
}

public class OptionalMarshaller<T>(IntPtr nativeProperty, MarshallingDelegates<T>.ToNative toNative, MarshallingDelegates<T>.FromNative fromNative)
{
    public void ToNative(IntPtr nativeBuffer, int arrayIndex, TOptional<T> obj)
    {
        unsafe
        {
            if (obj.IsSet)
            {
                var result = FOptionalPropertyExporter.CallMarkSetAndGetInitializedValuePointerToReplace(nativeProperty, nativeBuffer);
                toNative(result, 0, obj.GetOrDefault()!);
            }
            else
            {
                FOptionalPropertyExporter.MarkUnset(nativeProperty, nativeBuffer);
            }
        }
    }

    public TOptional<T> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        unsafe
        {
            if (!FOptionalPropertyExporter.CallIsSet(nativeProperty, nativeBuffer).ToManagedBool()) return Optional.Empty<T>();
            var result = FOptionalPropertyExporter.GetValuePointerForRead(nativeProperty, nativeBuffer);
            return Optional.Of(fromNative(result, 0));
        }
    }
}