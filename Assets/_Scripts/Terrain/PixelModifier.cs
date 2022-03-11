using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ModifierType 
{
	AddOvertime,
	RemoveOvertime,
	Count
}

public class PixelModifier
{
	PixelWorld map;									//the current map it is being used on
	float radius;									//the radius of the stencil
	ModifierType modifierType = ModifierType.AddOvertime;	//the stencil modifier type
	float strength = 1;								//affects the speed of change of pixels
	Vector2 centre;                                 //the centre position of the modification
	float rotation = 0;								//rotation in radians
	PixelStencil stencil;                           //the current stencil
	Pixel.MaterialType material;
	
	public delegate void OnRemove(float amountRemoved, float materialDistribution, Pixel.MaterialType type1, Pixel.MaterialType type2); //remove removes from both pixel types, and cannot be cancelled halfway through (no reason to)
	public delegate float OnAdd(float amountAdded, Pixel.MaterialType type); //can change the value added
	OnRemove onRemove;
	OnAdd onAdd;
	public void SetOnRemove(OnRemove newOnRemove)
	{
		if (newOnRemove != null)
			onRemove = newOnRemove;
	}
	public void SetOnAdd(OnAdd newOnAdd)
	{
		if (newOnAdd != null)
			onAdd = newOnAdd;
	}

	protected delegate void ApplyFunctions(ref Pixel pixel, Vector2 position, PixelModifier stencil);
	static ApplyFunctions[] apply = new ApplyFunctions[] { ApplyAdd, ApplyRemove };

	public PixelModifier(float strength, float radius, ModifierType type, Pixel.MaterialType pixelType, PixelStencil stencil)
	{
		this.strength = strength;
		this.radius = radius;
		this.modifierType = type;
		this.stencil = stencil;
		material = pixelType;

		//add dummy functions so that there is no need to if check
		SetOnRemove(DefaultOnRemove);
		SetOnAdd(DefaultOnAdd);
	}

	public bool IsRemoving()
	{
		return modifierType == ModifierType.RemoveOvertime;
	}

	//returns the change in pixel value
	public void Apply(ref Pixel pixel, Vector2 position)
	{
		//calls the correct apply function based on the modifier type
		//I'm not sure if this is better than a switch statement honestly. its definitely messier.
		apply[(int)modifierType](ref pixel, position, this);
	}

	#region Apply Functions
	static void ApplyAdd(ref Pixel pixel, Vector2 position, PixelModifier modifier)
	{
		if (modifier.stencil.ShouldModify(position, modifier, ref pixel))
		{
			float targetValue = modifier.stencil.GetTargetValue(position, modifier);
			if (pixel.type1 == modifier.material || (pixel.type2 != modifier.material && pixel.value1 == 0))
			{
				pixel.type1 = modifier.material;
				float newValue = Mathf.Clamp(Mathf.MoveTowards(pixel.value1, targetValue, modifier.strength * Time.deltaTime), pixel.value1, 1 - pixel.value2);
				pixel.value1 = pixel.value1 + modifier.onAdd(newValue - pixel.value1, pixel.type1);
			}
			else
			{
				pixel.type2 = modifier.material;
				float newValue = Mathf.Clamp(Mathf.MoveTowards(pixel.value2, targetValue, modifier.strength * Time.deltaTime), pixel.value2, 1 - pixel.value1);
				pixel.value2 = pixel.value2 + modifier.onAdd(newValue - pixel.value2, pixel.type2);
			}
		}
		else if (pixel.Value < modifier.map.ValueThreshold)
		{
			ApplyRemove(ref pixel, position, modifier);
		}
		
		
	}

	static void ApplyRemove(ref Pixel pixel, Vector2 position, PixelModifier modifier)
	{
		float oldValue = pixel.Value;
		float t = pixel.Material1Percentage;
		if (float.IsNaN(t))
			t = 0;

		float targetValue = modifier.stencil.GetTargetRemoveValue(position, modifier);
		float newValue = Mathf.Clamp(Mathf.MoveTowards(oldValue, targetValue, modifier.strength * Time.deltaTime)
			,0, oldValue);

		modifier.onRemove(newValue - oldValue, t, pixel.type1, pixel.type2);
		pixel.value1 = t * newValue;
		pixel.value2 = (1-t) * newValue;
	}
	
	#endregion

	#region Readonly Properties
	public float XStart { get { return centre.x - radius ; } }

	public float XEnd { get { return centre.x + radius ; } }

	public float YStart { get { return centre.y - radius ; } }

	public float YEnd { get { return centre.y + radius ; } }

	public float DefaultOnAdd(float amountAdded, Pixel.MaterialType type) { return amountAdded; }
	public void DefaultOnRemove(float amountRemoved, float materialDistribution, Pixel.MaterialType type1, Pixel.MaterialType type2) { }
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