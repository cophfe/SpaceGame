using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StencilType : int
{
	Square,
	Circle,
	Count
}

public enum StencilModifierType 
{
	Set,
	Add,
	AddOvertime,
	Count
}

public abstract class Stencil
{
	#region Properties
	//the radius of the stencil
	public float Radius { get => radius; set => radius = Mathf.Max(value,0); }
	//the stencil modifier type
	public StencilModifierType ModifierType { get; set; } = StencilModifierType.Set;
	//the centre position of the stencil
	public Vector2 Position { get; set; }
	/*
		The strength of the voxel modification
		Set: what percent of the max will it be set to
		Add: what amount will be added
		AddOvertime: what amount will be added * deltaTime
	*/
	public float Strength { get => strength; set => strength = Mathf.Clamp(value, -1, 1); } 
	#endregion

	float strength;
	float radius = 1.0f;

	public Stencil(float strength, float radius, StencilModifierType type)
	{
		Strength = strength;
		Radius = radius;
		ModifierType = type;
	}

	public abstract StencilType StencilType { get; }

	public abstract void Apply(ref Voxel voxel, Vector2 position);

	#region Readonly Properties
	public float XStart { get { return Position.x - Radius; } }

	public float XEnd { get { return Position.x + Radius; } }

	public float YStart { get { return Position.y - Radius; } }

	public float YEnd { get { return Position.y + Radius; } }
	#endregion
}


public class SquareStencil : Stencil
{
	public SquareStencil(float strength, float radius, StencilModifierType type) : base(strength, radius, type)
	{ }

	public override StencilType StencilType { get { return StencilType.Square; } }

	public override void Apply(ref Voxel voxel, Vector2 position)
	{
		if (position.x >= XStart && position.x <= XEnd && position.y >= YStart && position.y <= YEnd)
		{
			switch (ModifierType)
			{
				case StencilModifierType.Set:
					voxel.value = Mathf.Max(Strength, 0);
					break;
				case StencilModifierType.Add:
					voxel.value = Mathf.Clamp01(voxel.value + Strength);
					break;
				case StencilModifierType.AddOvertime:
					voxel.value = Mathf.Clamp01(voxel.value + Strength * Time.deltaTime);
					break;
			}
		}
	}
}

public class CircleStencil : Stencil
{
	public CircleStencil(float strength, float radius, StencilModifierType type) : base(strength, radius, type)
	{ }

	public override StencilType StencilType { get { return StencilType.Circle; } }

	public override void Apply(ref Voxel voxel, Vector2 position)
	{
		float x = position.x - Position.x;
		float y = position.y - Position.y;
		
		switch (ModifierType)
		{
			case StencilModifierType.Set:
				voxel.value = Mathf.Max(Strength *(Radius -  Mathf.Sqrt(x * x + y * y)), voxel.value);
				break;
			case StencilModifierType.Add:
				voxel.value = Mathf.Clamp01(voxel.value + Strength * (Radius - Mathf.Sqrt(x * x + y * y)));
				break;
			case StencilModifierType.AddOvertime:
				voxel.value = Mathf.Clamp01(voxel.value + Strength * (Radius - Mathf.Sqrt(x * x + y * y)) * Time.deltaTime);
				break;
		}
	}
}
