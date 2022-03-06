using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StencilType : byte
{
	Rectangle,
	Circle,
	Count,
	Error
}

public abstract class PixelStencil
{
	//gets whether a pixel in the modifier bounding box should be modified or not
	public abstract bool ShouldModify(ref Pixel pixel, Vector2 position, PixelModifier modifier);

	//gets the target value for the stencil
	public abstract float GetTargetValue(ref Pixel pixel, Vector2 position, PixelModifier modifier);

	//gets the target value for the stencil when removing stencil
	public abstract float GetTargetRemoveValue(ref Pixel pixel, Vector2 position, PixelModifier modifier);


	public abstract StencilType Type { get; }
}

public class PixelStencilCircle : PixelStencil
{
	public PixelStencilCircle()
	{

	}

	public override bool ShouldModify(ref Pixel pixel, Vector2 position, PixelModifier modifier)
	{
		return true;
	}

	//note: do not need to get pixel value, since target value does not rely on previous value
	public sealed override float GetTargetValue(ref Pixel pixel, Vector2 position, PixelModifier modifier)
	{

		position = position - modifier.Centre;

		//this value is wierdly a bit off (depending on the closeness to the centrepoint, which is not good because this can be close to the centrepoint)
		//heres the desmos implementation: https://www.desmos.com/calculator/p0e7pd65gy (works as long as the line is tangent to the x or y axis, and as long as it aint too close to the circle centrepoint)
		return modifier.Radius + modifier.World.ValueThreshold - position.magnitude;
	}

	public override float GetTargetRemoveValue(ref Pixel pixel, Vector2 position, PixelModifier modifier)
	{
		position = position - modifier.Centre;
		return position.magnitude + modifier.World.ValueThreshold - modifier.Radius;
	}

	public override StencilType Type => StencilType.Circle;
}




public class PixelStencilRectangle : PixelStencil
{
	public PixelStencilRectangle()
	{

	}

	public override bool ShouldModify(ref Pixel pixel, Vector2 position, PixelModifier modifier)
	{
		return true;
	}

	public sealed override float GetTargetValue(ref Pixel pixel, Vector2 position, PixelModifier modifier)
	{
		return 1;
	}

	public override float GetTargetRemoveValue(ref Pixel pixel, Vector2 position, PixelModifier modifier)
	{
		return 0;
	}

	public override StencilType Type => StencilType.Rectangle;
}
