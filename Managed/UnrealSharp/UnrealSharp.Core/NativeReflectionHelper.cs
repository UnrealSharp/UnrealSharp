using UnrealSharp.Core.Interop;

namespace UnrealSharp.Core;

public static class NativeReflectionHelper
{
	private enum ECSFieldType : byte
	{
		Unknown,
		Class,
		Struct,
		Enum,
		Interface,
		Delegate
	}

	private static ECSFieldType GetFieldType(Type type)
	{
		if (type.IsInterface)
		{
			return ECSFieldType.Interface;
		}

		if (type.IsEnum)
		{
			return ECSFieldType.Enum;
		}

		if (typeof(Delegate).IsAssignableFrom(type))
		{
			return ECSFieldType.Delegate;
		}
		
		if (type.IsValueType)
		{
			return ECSFieldType.Struct;
		}
		
		if (type.IsClass)
		{
			return ECSFieldType.Class;
		}

		return ECSFieldType.Unknown;
	}
	
	public static IntPtr GetNativeField<T>() => GetNativeField(typeof(T));
	public static IntPtr GetNativeField(Type type) => GetNativeField(type, type.Name);
	public static IntPtr GetNativeField(Type type, string sourceName) =>
		Bind_UCoreUObject.CallGetNativeField(type.Assembly.GetName().Name!, type.Namespace, sourceName,
			(byte)GetFieldType(type));
}