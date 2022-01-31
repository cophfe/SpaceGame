using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CatLikeCoding
{

	public class VoxelMap : MonoBehaviour
	{
		#region Exposed
		[SerializeField] float size = 2f;

		[SerializeField] int voxelResolution = 8;
		[SerializeField] int chunkResolution = 2;

		[SerializeField] VoxelGrid voxelGridPrefab = null;
		#endregion

		#region UnExposed
		VoxelGrid[] chunks;


		float chunkSize;
		float voxelSize;
		float halfSize;
		#endregion

		#region Properties
		public BoxCollider MapCollider { get; private set; } = null;
		public float HalfSize => halfSize;
		public float VoxelSize => voxelSize;
		public float ChunkSize => chunkSize;
		#endregion

		private void Awake()
		{
			halfSize = size * 0.5f;
			chunkSize = size / chunkResolution;
			voxelSize = chunkSize / voxelResolution;

			chunks = new VoxelGrid[chunkResolution * chunkResolution];
			for (int i = 0, y = 0; y < chunkResolution; y++)
			{
				for (int x = 0; x < chunkResolution; x++, i++)
				{
					CreateChunk(i, x, y);
				}
			}

			MapCollider = gameObject.AddComponent<BoxCollider>();
			MapCollider.size = new Vector3(size, size);

		}

		void CreateChunk(int i, int x, int y)
		{
			VoxelGrid chunk = Instantiate(voxelGridPrefab, transform);
			chunk.Initialize(voxelResolution, chunkSize);
			chunk.transform.localPosition = new Vector3(x * chunkSize - halfSize, y * chunkSize - halfSize);
			chunks[i] = chunk;

			//set last chunk's xneighbor
			if (x > 0)
			{
				chunks[i - 1].xNeighbor = chunk;
			}
			//set nextdoor y neighbor's neighbor chunk to this
			if (y > 0)
			{
				chunks[i - chunkResolution].yNeighbor = chunk;
				//set diagonal neighbor too
				if (x > 0)
				{
					chunks[i - chunkResolution - 1].xyNeighbor = chunk;
				}
			}

		}

		public void ApplyStencil(Vector2 point, VoxelStencil stencil)
		{
			//activeStencil.Initialize(fillTypeIndex == 0, (radiusIndex + 0.5f) * voxelSize);
			stencil.SetCenter(point.x, point.y);

			int xStart = (int)((stencil.XStart - voxelSize) / chunkSize);
			if (xStart < 0)
			{
				xStart = 0;
			}
			int xEnd = (int)((stencil.XEnd + voxelSize) / chunkSize);
			if (xEnd >= chunkResolution)
			{
				xEnd = chunkResolution - 1;
			}
			int yStart = (int)((stencil.YStart - voxelSize) / chunkSize);
			if (yStart < 0)
			{
				yStart = 0;
			}
			int yEnd = (int)((stencil.YEnd + voxelSize) / chunkSize);
			if (yEnd >= chunkResolution)
			{
				yEnd = chunkResolution - 1;
			}

			Debug.Log($"centre: ({point.x}, {point.y}) chunks X: s: {xStart}, e: {xEnd}. Y: s: {yStart}, e: {yEnd}");

			for (int y = yEnd; y >= yStart; y--)
			{
				int i = y * chunkResolution + xEnd;
				for (int x = xEnd; x >= xStart; x--, i--)
				{
					stencil.SetCenter(point.x - x * chunkSize, point.y - y * chunkSize);
					chunks[i].Apply(stencil);
				}
			}
		}
	}
}