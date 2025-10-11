﻿#pragma once

UENUM()
enum class ECSPropertyType : uint8
{
	Unknown,

	Bool,

	Int8,
	Int16,
	Int,
	Int64,

	Byte,
	UInt16,
	UInt32,
	UInt64,

	Double,
	Float,

	Enum,

	Interface,
	Struct,
	Class,

	Object,
	ObjectPtr,
	DefaultComponent,
	LazyObject,
	WeakObject,

	ScriptInterface,

	SoftClass,
	SoftObject,

	Delegate,
	MulticastInlineDelegate,
	MulticastSparseDelegate,

	Array,
	Map,
	Set,
    Optional,
        
	String,
	Name,
	Text,
	
	GameplayTag,
	GameplayTagContainer,

	InternalNativeFixedSizeArray,
	InternalManagedFixedSizeArray
};
