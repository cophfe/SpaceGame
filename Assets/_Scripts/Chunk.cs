using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Chunk : MonoBehaviour
{
	//Exposed
	[SerializeField] Vector2Int voxelDimensions = new Vector2Int(32, 32);
	[SerializeField] [Min(0.001f)] float cellSize = 1.0f;
	[SerializeField] [Range(0, 1)]  float valueThreshold = 0.5f;
	//Temp Exposed
	[Range(0.000f, 1)] public float perlinSampleSize = 0.1f;


	//Private
	//Chunk up, right, upright;
	float[,] voxels = null;
	Mesh mesh;
	List<Vector3> vertices = new List<Vector3>();
	List<int> triangles = new List<int>();

	private void Start()
	{
		mesh = new Mesh();
		vertices = new List<Vector3>();
		triangles = new List<int>();
		GetComponent<MeshFilter>().mesh = mesh;

		//TEMP:
		//~~~~~~
		CalculateGridValues();
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		//~~~~~~

		GenerateMesh();

		stopwatch.Stop();
		UnityEngine.Debug.Log("Time without quad tree: " + (double)stopwatch.ElapsedMilliseconds / 1000.0d);
	}
	private void OnValidate()
	{
		if (voxelDimensions.x < 0)
			voxelDimensions.x = 0;
		if (voxelDimensions.y < 0)
			voxelDimensions.y = 0;

		if (Application.isPlaying && mesh != null)
		{
			//TEMP:
			//~~~~~~
			CalculateGridValues();
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			//~~~~~~
			GenerateMesh();

			//TEMP:
			//~~~~~~
			stopwatch.Stop();
			UnityEngine.Debug.Log("Time without quad tree: " + (double)stopwatch.ElapsedMilliseconds / 1000.0d);
			//~~~~~~
		}

	}

	public void CalculateGridValues()
	{
		voxels = new float[voxelDimensions.x, voxelDimensions.y];

		for (int x = 0; x < voxelDimensions.x; x++)
		{
			for (int y = 0; y < voxelDimensions.y; y++)
			{
				voxels[x, y] = Mathf.Clamp01(Mathf.PerlinNoise(x * perlinSampleSize, y * perlinSampleSize));
			}
		}
	}

	public void GenerateMesh()
	{
		mesh.Clear();
		vertices.Clear();
		triangles.Clear();

		//A cell is comprised of four voxels, one in each corner.
		//there is one less column and row for cells then voxels
		//go through every cell
		for (int x = 0; x < voxelDimensions.x - 1; x++)
		{
			for (int y = 0; y < voxelDimensions.y - 1; y++)
			{
				byte cellType = 0;

				if (voxels[x, y] > valueThreshold)
					cellType =  0b0001;
				if (voxels[x + 1, y] > valueThreshold)
					cellType |= 0b0010;
				if (voxels[x, y + 1] > valueThreshold)
					cellType |= 0b0100;
				if (voxels[x + 1, y + 1] > valueThreshold)
					cellType |= 0b1000;

				switch (cellType)
				{
					case 0:
						continue;
					case 1:
						AddTriangle(
							GetPointFromIndex(x, y),
							GetPointBetweenPoints(x, y, x, y + 1),
							GetPointBetweenPoints(x, y, x + 1, y)
							);
						break;
					case 2:
						AddTriangle(
							GetPointFromIndex(x + 1, y),
							GetPointBetweenPoints(x, y, x + 1, y),
							GetPointBetweenPoints(x + 1, y, x + 1, y + 1)
							);
						break;
					case 3:
						AddQuad(
							GetPointFromIndex(x, y),
							GetPointBetweenPoints(x, y, x, y + 1),
							GetPointBetweenPoints(x + 1, y, x + 1, y + 1),
							GetPointFromIndex(x + 1, y)
							);
						break;
					case 4:
						AddTriangle(
							GetPointFromIndex(x, y + 1),
							GetPointBetweenPoints(x, y + 1, x + 1, y + 1),
							GetPointBetweenPoints(x, y + 1, x, y)
							);
						break;
					case 5:
						AddQuad(
							GetPointFromIndex(x, y),
							GetPointFromIndex(x, y + 1),
							GetPointBetweenPoints(x, y + 1, x + 1, y + 1),
							GetPointBetweenPoints(x, y, x + 1, y)
							);
						break;
					case 6:
						AddTriangle(
							GetPointFromIndex(x, y + 1),
							GetPointBetweenPoints(x, y + 1, x + 1, y + 1),
							GetPointBetweenPoints(x, y + 1, x, y)
							);
						AddTriangle(
							GetPointFromIndex(x + 1, y),
							GetPointBetweenPoints(x, y, x + 1, y),
							GetPointBetweenPoints(x + 1, y, x + 1, y + 1)
							);
						break;
					case 7:
						AddPentagon(
							GetPointFromIndex(x, y + 1),
							GetPointBetweenPoints(x, y + 1, x + 1, y + 1),
							GetPointBetweenPoints(x + 1, y, x + 1, y + 1),
							GetPointFromIndex(x + 1, y),
							GetPointFromIndex(x, y)
							);
						break;
					case 8:
						AddTriangle(
							GetPointFromIndex(x + 1, y + 1),
							GetPointBetweenPoints(x + 1, y + 1, x + 1, y),
							GetPointBetweenPoints(x + 1, y + 1, x, y + 1)
							);
						break;
					case 9:
						AddTriangle(
							GetPointFromIndex(x + 1, y + 1),
							GetPointBetweenPoints(x + 1, y + 1, x + 1, y),
							GetPointBetweenPoints(x + 1, y + 1, x, y + 1)
							);
						AddTriangle(
							GetPointFromIndex(x, y),
							GetPointBetweenPoints(x, y, x, y + 1),
							GetPointBetweenPoints(x, y, x + 1, y)
							);
						break;
					case 10:
						AddQuad(
							GetPointFromIndex(x + 1, y + 1),
							GetPointFromIndex(x + 1, y),
							GetPointBetweenPoints(x + 1, y, x, y),
							GetPointBetweenPoints(x + 1, y + 1, x, y + 1)
							);
						break;
					case 11:
						AddPentagon(
							GetPointFromIndex(x, y),
							GetPointBetweenPoints(x, y, x, y + 1),
							GetPointBetweenPoints(x, y + 1, x + 1, y + 1),
							GetPointFromIndex(x + 1, y + 1),
							GetPointFromIndex(x + 1, y)
							);
						break;
					case 12:
						AddQuad(
							GetPointFromIndex(x, y + 1),
							GetPointFromIndex(x + 1, y + 1),
							GetPointBetweenPoints(x + 1, y + 1, x + 1, y),
							GetPointBetweenPoints(x, y + 1, x, y)
							);
						break;
					case 13:
						AddPentagon(
							GetPointFromIndex(x + 1, y + 1),
							GetPointBetweenPoints(x + 1, y + 1, x + 1, y),
							GetPointBetweenPoints(x, y, x + 1, y),
							GetPointFromIndex(x, y),
							GetPointFromIndex(x, y + 1)
							);
						break;
					case 14:
						AddPentagon(
							GetPointFromIndex(x + 1, y),
							GetPointBetweenPoints(x + 1, y, x, y),
							GetPointBetweenPoints(x, y, x, y + 1),
							GetPointFromIndex(x, y + 1),
							GetPointFromIndex(x + 1, y + 1)
							);
						break;
					case 15:
						AddQuad(
							GetPointFromIndex(x, y),
							GetPointFromIndex(x, y + 1),
							GetPointFromIndex(x + 1, y + 1),
							GetPointFromIndex(x + 1, y)
							);
						break;
				}
			}
		}

		mesh.indexFormat = vertices.Count >= 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();

		void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
		{
			triangles.Add(vertices.Count);
			triangles.Add(vertices.Count + 1);
			triangles.Add(vertices.Count + 2);
			vertices.Add(a);
			vertices.Add(b);
			vertices.Add(c);
		}

		void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
		{
			triangles.Add(vertices.Count);
			triangles.Add(vertices.Count + 1);
			triangles.Add(vertices.Count + 2);
			triangles.Add(vertices.Count);
			triangles.Add(vertices.Count + 2);
			triangles.Add(vertices.Count + 3);
			vertices.Add(a);
			vertices.Add(b);
			vertices.Add(c);
			vertices.Add(d);
		}

		void AddPentagon(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 e)
		{
			triangles.Add(vertices.Count);
			triangles.Add(vertices.Count + 1);
			triangles.Add(vertices.Count + 2);
			triangles.Add(vertices.Count);
			triangles.Add(vertices.Count + 2);
			triangles.Add(vertices.Count + 3);
			triangles.Add(vertices.Count);
			triangles.Add(vertices.Count + 3);
			triangles.Add(vertices.Count + 4);
			vertices.Add(a);
			vertices.Add(b);
			vertices.Add(c);
			vertices.Add(d);
			vertices.Add(e);
		}

		Vector2 GetPointBetweenPoints(int x1, int y1, int x2, int y2)
		{
			float v1 = voxels[x1, y1];
			float v2 = voxels[x2, y2];
			float t = (valueThreshold - v1) / (v2 - v1);

			return Vector3.Lerp(GetPointFromIndex(x1, y1), GetPointFromIndex(x2, y2), t);
		}
	}
	
	Vector2 GetPointFromIndex(int x, int y)
	{
		return new Vector2(x * cellSize, y * cellSize);
	}

	

	private void OnDrawGizmosSelected()
	{
		if (voxels != null)
		{
			Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
			Vector2 centerOffset = new Vector2((voxelDimensions.x - 1)* cellSize * -0.5f, (voxelDimensions.y - 1) * cellSize * -0.5f);
			int width = voxels.GetLength(0);
			int height = voxels.GetLength(1);

			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					Gizmos.color = voxels[x, y] > valueThreshold ? Color.white : Color.black;
					Gizmos.DrawCube(new Vector2(x * cellSize, y * cellSize) + centerOffset, Vector2.one * cellSize * 0.5f);
				}
			}
		}
	}
}


