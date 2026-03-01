namespace UnrealSharp.SlateCore;

public partial struct FMargin
{
	public FMargin(float uniformMargin)
	{
		Left = uniformMargin;
		Top = uniformMargin;
		Right = uniformMargin;
		Bottom = uniformMargin;
	}
	
	public FMargin(float horizontal, float vertical)
	{
		Left = horizontal;
		Right = horizontal;
		Top = vertical;
		Bottom = vertical;
	}
	
	public bool IsZero => Left == 0f && Top == 0f && Right == 0f && Bottom == 0f;
	
	public float TotalHorizontal => Left + Right;
	public float TotalVertical => Top + Bottom;

	public override string ToString()
	{
		return $"Left={Left}, Top={Top}, Right={Right}, Bottom={Bottom}";
	}
}