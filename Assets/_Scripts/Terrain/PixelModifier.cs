using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ModifierType 
{
	Fill,
	Delete,
	AddOvertime,
	RemoveOvertime,
	Count
}

public class PixelModifier
{
	PixelWorld map;									//the current map it is being used on
	float radius;									//the radius of the stencil
	ModifierType modifierType = ModifierType.Fill;	//the stencil modifier type
	float strength = 1;								//affects the speed of change of pixels
	Vector2 centre;                                 //the centre position of the modification
	float rotation = 0;								//rotation in radians
	PixelStencil stencil;                           //the current stencil
	Pixel.MaterialType material;
	
	protected delegate float ApplyFunctions(ref Pixel pixel, Vector2 position, PixelModifier stencil);
	static ApplyFunctions[] apply = new ApplyFunctions[] { ApplyFill, ApplyDelete, ApplyAdd, ApplyRemove };

	public PixelModifier(float strength, float radius, ModifierType type, Pixel.MaterialType pixelType, PixelStencil stencil)
	{
		this.strength = strength;
		this.radius = radius;
		this.modifierType = type;
		this.stencil = stencil;
		material = pixelType;
	}

	public bool IsRemoving()
	{
		return modifierType == ModifierType.Delete || modifierType == ModifierType.RemoveOvertime;
	}

	//returns the change in pixel value
	public float Apply(ref Pixel pixel, Vector2 position)
	{
		//calls the correct apply function based on the modifier type
		//I'm not sure if this is better than a switch statement honestly. its definitely messier.

		if (stencil.ShouldModify(position, this, ref pixel))
			return apply[(int)modifierType](ref pixel, position, this);

		return 0;
	}

	#region Apply Functions
	static float ApplyFill(ref Pixel pixel, Vector2 position, PixelModifier modifier)
	{
		float targetValue = modifier.stencil.GetTargetValue(position, modifier);
		if (pixel.type1 == modifier.material || (pixel.type2 != modifier.material && pixel.value1 == 0))
		{
			pixel.type1 = modifier.material;
			float oldValue = pixel.value1;
			float newValue = Mathf.Clamp(targetValue, oldValue, 1 - pixel.value2);
			pixel.value1 = newValue;
			return newValue - oldValue;
		}
		else
		{
			pixel.type2 = modifier.material;
			float oldValue = pixel.value2;
			float newValue = Mathf.Clamp(targetValue, oldValue, 1 - pixel.value1);
			pixel.value2 = newValue;
			return newValue - oldValue;
		}
	}

	static float ApplyDelete(ref Pixel pixel, Vector2 position, PixelModifier modifier)
	{
		float oldValue = pixel.Value;
		float t = pixel.Material1Percentage;

		float newValue = Mathf.Clamp(modifier.stencil.GetTargetRemoveValue(position, modifier), 0, oldValue);
		pixel.value1 -= newValue * t;
		pixel.value2 -= newValue * (1-t);

		return pixel.Value - oldValue;
	}

	static float ApplyAdd(ref Pixel pixel, Vector2 position, PixelModifier modifier)
	{
		float targetValue = modifier.stencil.GetTargetValue(position, modifier);
		if (pixel.type1 == modifier.material || (pixel.type2 != modifier.material && pixel.value1 == 0))
		{
			pixel.type1 = modifier.material;
			float oldValue = pixel.value1;
			float newValue = Mathf.Clamp(Mathf.MoveTowards(oldValue, targetValue, modifier.strength * Time.deltaTime), oldValue, 1 - pixel.value2);
			pixel.value1 = newValue;
			return newValue - oldValue;
		}
		else
		{
			pixel.type2 = modifier.material;
			float oldValue = pixel.value2;
			float newValue = Mathf.Clamp(Mathf.MoveTowards(oldValue, targetValue, modifier.strength * Time.deltaTime), oldValue, 1 - pixel.value1);
			pixel.value2 = newValue;
			return newValue - oldValue;
		}
		
	}

	static float ApplyRemove(ref Pixel pixel, Vector2 position, PixelModifier modifier)
	{
		float oldValue = pixel.Value;
		float t = pixel.Material1Percentage;
		if (float.IsNaN(t))
			t = 0;

		float targetValue = modifier.stencil.GetTargetRemoveValue(position, modifier);
		float newValue = Mathf.Clamp(Mathf.MoveTowards(oldValue, targetValue, modifier.strength * Time.deltaTime)
			,0, oldValue);

		pixel.value1 = t * newValue;
		pixel.value2 = (1-t) * newValue;
		return newValue - oldValue;
	}
	
	#endregion

	#region Readonly Properties
	public float XStart { get { return centre.x - radius ; } }

	public float XEnd { get { return centre.x + radius ; } }

	public float YStart { get { return centre.y - radius ; } }

	public float YEnd { get { return centre.y + radius ; } }
	#endregion

	#region GetSet properties
	//everything is accessible in this class.

	public PixelWorld World { get => map; set => map = value; }
	public float Radius { get =>radius; set => radius = Mathf.Max(0, value);  }
	public float Strength { get => strength; set => strength = Mathf.Max(0, value);  }
	public ModifierType Type { get => modifierType; set=>modifierType = value; }
	public Vector2 Centre { get => centre; set => centre = value; }
	public float Rotation { get => rotation; set => rotation = value; }
	public Pixel.MaterialType PixelMaterial { get => material; set => material = value; }
	public PixelStencil Stencil { get => stencil; set => stencil = value; }
	#endregion
}