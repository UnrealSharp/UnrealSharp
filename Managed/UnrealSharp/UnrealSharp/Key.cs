using UnrealSharp.Interop;

namespace UnrealSharp;

internal struct SharedPtrMirror
{
	private IntPtr _object;
	private IntPtr _sharedReferenceCount;
}

public struct Key
{
	public Name Name { get; }

	public static readonly int NativeDataSize;

	public Key(Keys key)
	{
		Name = key == Keys.Invalid ? Name.None : new Name(key.ToString());
	}
	
	public static implicit operator Key(Keys key)
	{
		return new Key(key);
	}
	
	static Key()
	{
		IntPtr nativeStructPtr = UCoreUObjectExporter.CallGetNativeClassFromName("Key");
		NativeDataSize = UScriptStructExporter.CallGetNativeStructSize(nativeStructPtr);
	}
	
	public Key(IntPtr inNativeStruct)
	{
		unsafe
		{
			Name = *(Name*)inNativeStruct.ToPointer();
		}
	}

	public void ToNative(IntPtr buffer)
	{
		unsafe
		{
			*(Name*) buffer.ToPointer() = Name;
			*(SharedPtrMirror*) IntPtr.Add(buffer, sizeof(Name)).ToPointer() = default;
		}
	}
}

public static class KeyMarshaller
{
	public static Key FromNative(IntPtr nativeBuffer, int arrayIndex)
	{
		return BlittableMarshaller<Key>.FromNative(nativeBuffer, arrayIndex, Key.NativeDataSize);
	}

	public static void ToNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner, Key obj)
	{
		BlittableMarshaller<Key>.ToNative(nativeBuffer, arrayIndex, obj, Key.NativeDataSize);
	}
}

public enum Keys
{
	MouseX,
	MouseY,
	MouseScrollUp,
	MouseScrollDown,

	LeftMouseButton,
	RightMouseButton,
	MiddleMouseButton,
	ThumbMouseButton,
	ThumbMouseButton2,

	BackSpace,
	Tab,
	Enter,
	Pause,

	CapsLock,
	Escape,
	SpaceBar,
	PageUp,
	PageDown,
	End,
	Home,

	Left,
	Up,
	Right,
	Down,

	Insert,
	Delete,

	Zero,
	One,
	Two,
	Three,
	Four,
	Five,
	Six,
	Seven,
	Eight,
	Nine,

	A,
	B,
	C,
	D,
	E,
	F,
	G,
	H,
	I,
	J,
	K,
	L,
	M,
	N,
	O,
	P,
	Q,
	R,
	S,
	T,
	U,
	V,
	W,
	X,
	Y,
	Z,

	NumPadZero,
	NumPadOne,
	NumPadTwo,
	NumPadThree,
	NumPadFour,
	NumPadFive,
	NumPadSix,
	NumPadSeven,
	NumPadEight,
	NumPadNine,

	Multiply,
	Add,
	Subtract,
	Decimal,
	Divide,

	F1,
	F2,
	F3,
	F4,
	F5,
	F6,
	F7,
	F8,
	F9,
	F10,
	F11,
	F12,

	NumLock,

	ScrollLock,

	LeftShift,
	RightShift,
	LeftControl,
	RightControl,
	LeftAlt,
	RightAlt,
	LeftCommand,
	RightCommand,

	Semicolon,
	Equals,
	Comma,
	Underscore,
	Period,
	Slash,
	Tilde,
	LeftBracket,
	Backslash,
	RightBracket,
	Quote,

	// Platform Keys
	// These keys platform specific versions of keys that go by different names.
	// The delete key is a good example, on Windows Delete is the virtual key for Delete.
	// On Macs, the Delete key is the virtual key for BackSpace.
	Platform_Delete,

	// Gameplay Keys
	Gamepad_LeftX,
	Gamepad_LeftY,
	Gamepad_RightX,
	Gamepad_RightY,
	Gamepad_LeftTriggerAxis,
	Gamepad_RightTriggerAxis,

	Gamepad_LeftThumbstick,
	Gamepad_RightThumbstick,
	Gamepad_Special_Left,
	Gamepad_Special_Right,
	Gamepad_FaceButton_Bottom,
	Gamepad_FaceButton_Right,
	Gamepad_FaceButton_Left,
	Gamepad_FaceButton_Top,
	Gamepad_LeftShoulder,
	Gamepad_RightShoulder,
	Gamepad_LeftTrigger,
	Gamepad_RightTrigger,
	Gamepad_DPad_Up,
	Gamepad_DPad_Down,
	Gamepad_DPad_Right,
	Gamepad_DPad_Left,

	// Virtual key codes used for input axis button press/release emulation
	Gamepad_LeftStick_Up,
	Gamepad_LeftStick_Down,
	Gamepad_LeftStick_Right,
	Gamepad_LeftStick_Left,

	Gamepad_RightStick_Up,
	Gamepad_RightStick_Down,
	Gamepad_RightStick_Right,
	Gamepad_RightStick_Left,

	// static const FKey Vector axes (FVector; not float)
	Tilt,
	RotationRate,
	Gravity,
	Acceleration,

	// Gestures
	Gesture_SwipeLeftRight,
	Gesture_SwipeUpDown,
	Gesture_TwoFingerSwipeLeftRight,
	Gesture_TwoFingerSwipeUpDown,
	Gesture_Pinch,
	Gesture_Flick,

	// PS4-specific
	PS4_Special,

	// Steam Controller Specific;
	Steam_Touch_0,
	Steam_Touch_1,
	Steam_Touch_2,
	Steam_Touch_3,
	Steam_Back_Left,
	Steam_Back_Right,

	// Xbox One global speech commands
	Global_Menu,
	Global_View,
	Global_Pause,
	Global_Play,
	Global_Back,

	Invalid,
        
	// Fingers
	Touch1,
	Touch2,
	Touch3,
	Touch4,
	Touch5,
	Touch6,
	Touch7,
	Touch8,
	Touch9,
	Touch10,
}