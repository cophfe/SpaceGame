using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace CatLikeCoding
{
	//to get a better idea of how dual contouring and chunks can be implemented, I will go through this implementation
	public class VoxelGrid : MonoBehaviour
	{
		#region Exposed
		//needs to know its neighbors
		[System.NonSerialized] public VoxelGrid xNeighbor, yNeighbor, xyNeighbor;
		[System.NonSerialized] public Voxel[] voxels;
		#endregion

		#region UnExposed
		//for now voxel size is defined by resolution (keeping a constant size for the chunk as a whole), but later voxel size will be a public variable
		//the amount of voxels in one row or column
		int resolution = 5;
		//the size of a single voxel
		float voxelSize;
		//the total size of one side of the grid
		float gridSize;
		//the voxel array

		//this grid's mesh
		Mesh mesh;
		//the vertices in this grid
		List<Vector3> vertices;
		//the triangles in this grid
		List<int> triangles;

		//storage vessels for voxel information from neighbouring grids (since voxels store their position local to their own grid its best to create new localised voxels instead of directly accessing the other grid)
		Voxel dummyX, dummyY, dummyT;

		//stores vertex indexes for vertices around a cell being generated. cells check the vertices above and to the left of them (in case of duplicate vertices), and write the index of their lower and left vertices for future cells
		//entire rows need to be cached because you have to advance a row into the loop before they're used for checking with the cells below them. side vertices are useful for the next cell in the loop, so the entire row doesn't need to be cached
		private int[] rowCacheMax, rowCacheMin;
		private int edgeCacheMin, edgeCacheMax;
		#endregion

		public void Initialize(int resolution, float size)
		{
			this.resolution = resolution;
			gridSize = size;
			voxelSize = size / resolution;
			voxels = new Voxel[resolution * resolution];

			dummyX = new Voxel();
			dummyY = new Voxel();
			dummyT = new Voxel();
			//i = y * resolution + x
			//y = i/resolution (floored)
			//x = i - y * resolution (if y not calculated, x = i % resolution)

			for (int i = 0, y = 0; y < resolution; y++)
			{
				for (int x = 0; x < resolution; x++, i++)
				{
					//loop through every voxel to initiate
					CreateVoxel(i, x, y);
				}
			}

			mesh = new Mesh();
			GetComponent<MeshFilter>().mesh = mesh;
			mesh.name = "VoxelGrid Mesh";
			vertices = new List<Vector3>();
			triangles = new List<int>();

			rowCacheMax = new int[resolution * 2 + 1];
			rowCacheMin = new int[resolution * 2 + 1];

			Refresh();
		}

		void CreateVoxel(int i, int x, int y)
		{
			voxels[i] = new Voxel(x, y, voxelSize);
		}

		void Refresh()
		{
			Triangulate();
		}

		void Triangulate()
		{
			vertices.Clear();
			triangles.Clear();
			mesh.Clear();

			FillFirstRowCache();
			TriangulateCellRows();

			if (yNeighbor != null)
				TriangulateGapRow();

			mesh.vertices = vertices.ToArray();
			mesh.triangles = triangles.ToArray();
		}

		void FillFirstRowCache()
		{
			CacheFirstCorner(voxels[0]);

			int i;
			for (i = 0; i < resolution - 1; i++)
			{
				CacheNextEdgeAndCorner(i * 2, voxels[i], voxels[i + 1]);
			}
			if (xNeighbor != null)
			{
				dummyX.BecomeXDummyOf(xNeighbor.voxels[0], gridSize);
				CacheNextEdgeAndCorner(i * 2, voxels[i], dummyX);
			}
		}

		void CacheFirstCorner(Voxel voxel)
		{
			if (voxel.state)
			{
				//vertices.count == current index of the voxels
				rowCacheMax[0] = vertices.Count;
				vertices.Add(voxel.position);
			}
		}

		void CacheNextEdgeAndCorner(int i, Voxel xMin, Voxel xMax)
		{
			if (xMin.state != xMax.state)
			{
				rowCacheMax[i + 1] = vertices.Count;
				Vector3 p;
				p.x = xMin.xEdge;
				p.y = xMin.position.y;
				p.z = 0f;
				vertices.Add(p);
			}
			if (xMax.state)
			{
				rowCacheMax[i + 2] = vertices.Count;
				vertices.Add(xMax.position);
			}
		}

		void TriangulateCellRows()
		{
			//cells in one row
			int cells = resolution - 1;
			for (int i = 0, y = 0; y < cells; y++, i++)
			{
				SwapRowCaches();
				CacheFirstCorner(voxels[i + resolution]);
				CacheNextMiddleEdge(voxels[i], voxels[i + resolution]);

				for (int x = 0; x < cells; x++, i++)
				{
					Voxel
						a = voxels[i],
						b = voxels[i + 1],
						c = voxels[i + resolution],
						d = voxels[i + resolution + 1];

					int cacheIndex = x * 2;
					CacheNextEdgeAndCorner(cacheIndex, c, d);
					CacheNextMiddleEdge(b, d);
					TriangulateCell(cacheIndex, a, b, c, d);

				}
				if (xNeighbor != null)
					TriangulateGapCell(i);
			}
		}

		private void SwapRowCaches()
		{
			int[] rowSwap = rowCacheMin;
			rowCacheMin = rowCacheMax;
			rowCacheMax = rowSwap;
		}

		private void CacheNextMiddleEdge(Voxel yMin, Voxel yMax)
		{
			edgeCacheMin = edgeCacheMax;
			if (yMin.state != yMax.state)
			{
				edgeCacheMax = vertices.Count;
				Vector3 p;
				p.x = yMin.position.x;
				p.y = yMin.yEdge;
				p.z = 0f;
				vertices.Add(p);
			}
		}

		void TriangulateCell(int i, Voxel a, Voxel b, Voxel c, Voxel d)
		{
			//create cell from the voxel corners

			//check what type of cell (0 to 15 based on the corner values)
			int cellType = 0;
			if (a.state)
			{
				cellType = 1;
			}
			if (b.state)
			{
				cellType |= 2;
			}
			if (c.state)
			{
				cellType |= 4;
			}
			if (d.state)
			{
				cellType |= 8;
			}

			switch (cellType)
			{
				case 0:
					return;
				case 1:
					AddTriangle(rowCacheMin[i], edgeCacheMin, rowCacheMin[i + 1]);
					break;
				case 2:
					AddTriangle(rowCacheMin[i + 2], rowCacheMin[i + 1], edgeCacheMax);
					break;
				case 3:
					AddQuad(rowCacheMin[i], edgeCacheMin, edgeCacheMax, rowCacheMin[i + 2]);
					break;
				case 4:
					AddTriangle(rowCacheMax[i], rowCacheMax[i + 1], edgeCacheMin);
					break;
				case 5:
					AddQuad(rowCacheMin[i], rowCacheMax[i], rowCacheMax[i + 1], rowCacheMin[i + 1]);
					break;
				case 6:
					AddTriangle(rowCacheMin[i + 2], rowCacheMin[i + 1], edgeCacheMax);
					AddTriangle(rowCacheMax[i], rowCacheMax[i + 1], edgeCacheMin);
					break;
				case 7:
					AddPentagon(
						rowCacheMin[i], rowCacheMax[i], rowCacheMax[i + 1], edgeCacheMax, rowCacheMin[i + 2]);
					break;
				case 8:
					AddTriangle(rowCacheMax[i + 2], edgeCacheMax, rowCacheMax[i + 1]);
					break;
				case 9:
					AddTriangle(rowCacheMin[i], edgeCacheMin, rowCacheMin[i + 1]);
					AddTriangle(rowCacheMax[i + 2], edgeCacheMax, rowCacheMax[i + 1]);
					break;
				case 10:
					AddQuad(rowCacheMin[i + 1], rowCacheMax[i + 1], rowCacheMax[i + 2], rowCacheMin[i + 2]);
					break;
				case 11:
					AddPentagon(
						rowCacheMin[i + 2], rowCacheMin[i], edgeCacheMin, rowCacheMax[i + 1], rowCacheMax[i + 2]);
					break;
				case 12:
					AddQuad(edgeCacheMin, rowCacheMax[i], rowCacheMax[i + 2], edgeCacheMax);
					break;
				case 13:
					AddPentagon(
						rowCacheMax[i], rowCacheMax[i + 2], edgeCacheMax, rowCacheMin[i + 1], rowCacheMin[i]);
					break;
				case 14:
					AddPentagon(
						rowCacheMax[i + 2], rowCacheMin[i + 2], rowCacheMin[i + 1], edgeCacheMin, rowCacheMax[i]);
					break;
				case 15:
					AddQuad(rowCacheMin[i], rowCacheMax[i], rowCacheMax[i + 2], rowCacheMin[i + 2]);
					break;
			}
		}

		private void AddTriangle(int a, int b, int c)
		{
			triangles.Add(a);
			triangles.Add(b);
			triangles.Add(c);
		}

		private void AddQuad(int a, int b, int c, int d)
		{
			triangles.Add(a);
			triangles.Add(b);
			triangles.Add(c);
			triangles.Add(a);
			triangles.Add(c);
			triangles.Add(d);
		}

		private void AddPentagon(int a, int b, int c, int d, int e)
		{
			triangles.Add(a);
			triangles.Add(b);
			triangles.Add(c);
			triangles.Add(a);
			triangles.Add(c);
			triangles.Add(d);
			triangles.Add(a);
			triangles.Add(d);
			triangles.Add(e);
		}

		void TriangulateGapCell(int i)
		{
			//(i+1 only works if the neighbouring grids are the same width)
			Voxel dummySwap = dummyT;
			dummySwap.BecomeXDummyOf(xNeighbor.voxels[i + 1], gridSize);
			dummyT = dummyX;
			dummyX = dummySwap;

			int cacheIndex = (resolution - 1) * 2;
			CacheNextEdgeAndCorner(cacheIndex, voxels[i + resolution], dummyX);
			CacheNextMiddleEdge(dummyT, dummyX);
			TriangulateCell(cacheIndex, voxels[i], dummyT, voxels[i + resolution], dummyX);
		}

		private void TriangulateGapRow()
		{
			dummyY.BecomeYDummyOf(yNeighbor.voxels[0], gridSize);
			int cells = resolution - 1;
			int offset = cells * resolution;
			SwapRowCaches();
			CacheFirstCorner(dummyY);
			CacheNextMiddleEdge(voxels[cells * resolution], dummyY);

			for (int x = 0; x < cells; x++)
			{
				Voxel dummySwap = dummyT;
				dummySwap.BecomeYDummyOf(yNeighbor.voxels[x + 1], gridSize);
				dummyT = dummyY;
				dummyY = dummySwap;

				int cacheIndex = x * 2;
				CacheNextEdgeAndCorner(cacheIndex, dummyT, dummyY);
				CacheNextMiddleEdge(voxels[x + offset + 1], dummyY);
				TriangulateCell(cacheIndex, voxels[x + offset], voxels[x + offset + 1], dummyT, dummyY);
			}

			if (xNeighbor != null)
			{
				dummyT.BecomeXYDummyOf(xyNeighbor.voxels[0], gridSize);

				int cacheIndex = cells * 2;
				CacheNextEdgeAndCorner(cacheIndex, dummyY, dummyT);
				CacheNextMiddleEdge(dummyX, dummyT);
				TriangulateCell(cacheIndex, voxels[voxels.Length - 1], dummyX, dummyY, dummyT);
			}
		}

		public void Apply(VoxelStencil stencil)
		{
			int xStart = (int)(stencil.XStart / voxelSize);
			if (xStart < 0)
			{
				xStart = 0;
			}
			int xEnd = (int)(stencil.XEnd / voxelSize);
			if (xEnd >= resolution)
			{
				xEnd = resolution - 1;
			}
			int yStart = (int)(stencil.YStart / voxelSize);
			if (yStart < 0)
			{
				yStart = 0;
			}
			int yEnd = (int)(stencil.YEnd / voxelSize);
			if (yEnd >= resolution)
			{
				yEnd = resolution - 1;
			}

			for (int y = yStart; y <= yEnd; y++)
			{
				int i = y * resolution + xStart;
				for (int x = xStart; x <= xEnd; x++, i++)
				{
					stencil.Apply(voxels[i]);
				}
			}
			Refresh();
		}

		[System.Serializable]
		public class Voxel
		{
			public bool state;
			//xEdgePosition and yEdgePosition are wastes of space (only one float worth of unique value, and that value is just position's value + size * 0.5f)
			// position can also be computed on the fly cheaply, but idk if that is better honestly
			public Vector2 position;
			public float xEdge, yEdge;

			public Voxel()
			{

			}

			public Voxel(int x, int y, float size)
			{
				position.x = (x + 0.5f) * size;
				position.y = (y + 0.5f) * size;

				xEdge = position.x + size * 0.5f;
				yEdge = position.y + size * 0.5f;
			}

			public void BecomeXDummyOf(Voxel voxel, float offset)
			{
				state = voxel.state;
				position = voxel.position;
				position.x += offset;
				xEdge = voxel.xEdge + offset;
				yEdge = voxel.yEdge;
			}

			public void BecomeYDummyOf(Voxel voxel, float offset)
			{
				state = voxel.state;
				position = voxel.position;
				position.y += offset;
				xEdge = voxel.xEdge;
				yEdge = voxel.yEdge + offset;
			}

			public void BecomeXYDummyOf(Voxel voxel, float offset)
			{
				state = voxel.state;
				position = voxel.position;
				position.x += offset;
				position.y += offset;
				xEdge = voxel.xEdge + offset;
				yEdge = voxel.yEdge + offset;
			}

		}
	}


}