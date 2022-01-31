using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatLikeCoding
{

	public enum VoxelStencilType
	{
		Square,
		Circle
	}

	public class VoxelStencil
	{


		public bool FillType { get; set; } = true;
		public virtual float Radius { get => radius; set { radius = value; } }
		public virtual VoxelStencilType GetStencilType => VoxelStencilType.Square;
		//radius in units
		protected float radius = 1;
		protected Vector2 centre;

		public VoxelStencil(bool fillType, float radius)
		{
			FillType = fillType;
			Radius = radius;
		}

		public virtual void Apply(VoxelGrid.Voxel voxel)
		{
			Vector2 p = voxel.position;
			if (p.x >= XStart && p.x <= XEnd && p.y >= YStart && p.y <= YEnd)
			{
				voxel.state = FillType;
			}
		}

		public virtual void SetCenter(float x, float y)
		{
			centre.x = x;
			centre.y = y;
		}

		public void SetHorizontalCrossing(VoxelGrid.Voxel xMin, VoxelGrid.Voxel xMax)
		{
			if (xMin.state != xMax.state)
			{
				FindHorizontalCrossing(xMin, xMax);
			}
		}

		protected virtual void FindHorizontalCrossing(VoxelGrid.Voxel xMin, VoxelGrid.Voxel xMax)
		{
			if (xMin.position.y < YStart || xMin.position.y > YEnd)
			{
				return;
			}
		}

		public float XStart { get { return centre.x - radius; } }

		public float XEnd { get { return centre.x + radius; } }

		public float YStart { get { return centre.y - radius; } }

		public float YEnd { get { return centre.y + radius; } }
	}

	public class VoxelStencilCircle : VoxelStencil
	{
		public float sqrRadius = 1;
		public override float Radius { get => base.Radius; set { base.Radius = value; sqrRadius = radius * radius; } }
		public override VoxelStencilType GetStencilType => VoxelStencilType.Circle;
		public VoxelStencilCircle(bool fillType, float radius) : base(fillType, radius)
		{
		}

		public override void Apply(VoxelGrid.Voxel voxel)
		{
			float x = voxel.position.x - centre.x;
			float y = voxel.position.y - centre.y;
			if (x * x + y * y <= sqrRadius)
			{
				voxel.state = FillType;
			}
		}
	}
}