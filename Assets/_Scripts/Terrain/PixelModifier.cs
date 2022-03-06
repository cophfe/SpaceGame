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
	PixelStencil stencil;							//the current stencil (affects the target value
	
	protected delegate void ApplyFunctions(ref Pixel pixel, Vector2 position, PixelModifier stencil);
	static ApplyFunctions[] apply = new ApplyFunctions[] { ApplyFill, ApplyDelete, ApplyAdd, ApplyRemove };

	public PixelModifier(float strength, float radius, ModifierType type, PixelStencil stencil)
	{
		this.strength = strength;
		this.radius = radius;
		this.modifierType = type;
		this.stencil = stencil;
	}

	public bool IsRemoving()
	{
		return modifierType == ModifierType.Delete || modifierType == ModifierType.RemoveOvertime;
	}

	public void Apply(ref Pixel pixel, Vector2 position)
	{
		//calls the correct apply function based on the modifier type
		//I'm not sure if this is better than a switch statement honestly. its definitely messier.

		if (stencil.ShouldModify(ref pixel, position, this))
			apply[(int)modifierType](ref pixel, position, this);
	}

	#region Apply Functions
	static void ApplyFill(ref Pixel pixel, Vector2 position, PixelModifier modifier)
	{
		pixel.value = Mathf.Clamp(modifier.stencil.GetTargetValue(ref pixel, position, modifier), pixel.value, 1);
	}

	static void ApplyDelete(ref Pixel pixel, Vector2 position, PixelModifier modifier)
	{
		pixel.value = Mathf.Clamp(modifier.stencil.GetTargetRemoveValue(ref pixel, position, modifier), 0, pixel.value);
	}

	static void ApplyAdd(ref Pixel pixel, Vector2 position, PixelModifier modifier)
	{
		float targetValue = modifier.stencil.GetTargetValue(ref pixel, position, modifier);
		float newValue = Mathf.MoveTowards(pixel.value, targetValue, modifier.strength * Time.deltaTime);
		pixel.value = Mathf.Clamp(newValue, pixel.value, 1);
	}

	static void ApplyRemove(ref Pixel pixel, Vector2 position, PixelModifier modifier)
	{
		float targetValue = modifier.stencil.GetTargetRemoveValue(ref pixel, position, modifier);
		float newValue = Mathf.MoveTowards(pixel.value, targetValue, modifier.strength * Time.deltaTime);
		pixel.value = Mathf.Clamp(newValue, 0, pixel.value);
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
	public PixelStencil Stencil { get => stencil; set => stencil = value; }
	#endregion
}