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
	Fill,
	Delete,
	AddOvertime,
	RemoveOvertime,
	Count
}

public abstract class Stencil
{
	#region Properties
	//the radius of the stencil
	public float Radius { get => radius; set => radius = Mathf.Max(value,0); }
	//the stencil modifier type
	public StencilModifierType ModifierType { get; set; } = StencilModifierType.Fill;
	//the centre position of the stencil
	public Vector2 Position { get; set; }
	/*
		The strength of the voxel modification
		Set: what percent of the max will it be set to
		Add: what amount will be added
		AddOvertime: what amount will be added * deltaTime
	*/
	public float Strength { get => strength; set => strength = Mathf.Clamp01(value); } 
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
	protected delegate void ApplyFunctions(ref Voxel voxel, Vector2 position, Stencil stencil);

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
		applyFuncs[(int)ModifierType](ref voxel, position, this);
	}

	#region ApplyFunctions

	static ApplyFunctions[] applyFuncs = new ApplyFunctions[] { ApplyFill, ApplyDelete, ApplyAdd, ApplyRemove };

	static void ApplyFill(ref Voxel voxel, Vector2 position, Stencil stencil)
	{
		if (position.x >= stencil.XStart && position.x <= stencil.XEnd && position.y >= stencil.YStart && position.y <= stencil.YEnd)
		{
			voxel.value = Mathf.Max(stencil.Strength, 0);
		}
	}

	static void ApplyDelete(ref Voxel voxel, Vector2 position, Stencil stencil)
	{
		if (position.x >= stencil.XStart && position.x <= stencil.XEnd && position.y >= stencil.YStart && position.y <= stencil.YEnd)
		{
			voxel.value = 0;
		}
	}

	static void ApplyAdd(ref Voxel voxel, Vector2 position, Stencil stencil)
	{
		if (position.x >= stencil.XStart && position.x <= stencil.XEnd && position.y >= stencil.YStart && position.y <= stencil.YEnd)
		{
			voxel.value = Mathf.Clamp01(voxel.value + stencil.Strength * Time.deltaTime);
		}
	}

	static void ApplyRemove(ref Voxel voxel, Vector2 position, Stencil stencil)
	{
		if (position.x >= stencil.XStart && position.x <= stencil.XEnd && position.y >= stencil.YStart && position.y <= stencil.YEnd)
		{
			voxel.value = Mathf.Clamp01(voxel.value - stencil.Strength * Time.deltaTime);
		}
	}
	#endregion
}

public class CircleStencil : Stencil
{
	public CircleStencil(float strength, float radius, StencilModifierType type) : base(strength, radius, type)
	{ }

	public override StencilType StencilType { get { return StencilType.Circle; } }

	public override void Apply(ref Voxel voxel, Vector2 position)
	{
		applyFuncs[(int)ModifierType](ref voxel, position, this);
	}

	#region ApplyFunctions

	static ApplyFunctions[] applyFuncs = new ApplyFunctions[] { ApplyFill, ApplyDelete, ApplyAdd, ApplyRemove };

	static void ApplyFill(ref Voxel voxel, Vector2 position, Stencil stencil)
	{
		float x = position.x - stencil.Position.x;
		float y = position.y - stencil.Position.y;
		voxel.value = Mathf.Max(stencil.Strength * (stencil.Radius - Mathf.Sqrt(x * x + y * y) + 0.5f), voxel.value);
	}

	static void ApplyDelete(ref Voxel voxel, Vector2 position, Stencil stencil)
	{
		float x = position.x - stencil.Position.x;
		float y = position.y - stencil.Position.y;
		voxel.value = Mathf.Min(stencil.Strength * (Mathf.Sqrt(x * x + y * y - stencil.Radius) + 0.5f), voxel.value);
	}

	static void ApplyAdd(ref Voxel voxel, Vector2 position, Stencil stencil)
	{
		float x = position.x - stencil.Position.x;
		float y = position.y - stencil.Position.y;
		voxel.value = Mathf.Clamp01(voxel.value + Mathf.Max(stencil.Strength * (stencil.Radius - Mathf.Sqrt(x * x + y * y) + 0.5f) * Time.deltaTime, 0));
	}

	static void ApplyRemove(ref Voxel voxel, Vector2 position, Stencil stencil)
	{
		float x = position.x - stencil.Position.x;
		float y = position.y - stencil.Position.y;
		voxel.value = Mathf.Clamp01(voxel.value - Mathf.Max(stencil.Strength * (stencil.Radius - Mathf.Sqrt(x * x + y * y) + 0.5f) * Time.deltaTime,0));
	}

	#endregion
}
