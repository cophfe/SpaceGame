using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour
{
	//Exposed
	[SerializeField] Vector2Int chunkResolution = new Vector2Int(5,5);
	[SerializeField] Chunk chunkPrefab = null;
	[SerializeField] Vector2Int cellResolution = new Vector2Int(32, 32);
	[SerializeField] [Min(0.001f)] float cellSize = 1.0f;
	[SerializeField] [Range(0, 1)] float valueThreshold = 0.5f;

	//Temp exposed
	[Range(0.000f, 1)] public float perlinSampleSize = 0.1f;

	//Private
	Chunk[,] chunks;

	private void Awake()
	{
		chunks = new Chunk[chunkResolution.x, chunkResolution.y];

		for (int x = 0; x < chunkResolution.x; x++)
		{
			for (int y = 0; y < chunkResolution.y; y++)
			{
				CreateChunk(x, y);
			}
		}

		var collider = GetComponent<BoxCollider2D>();
		if (collider)
		{
			collider.size = new Vector2(cellResolution.x * cellSize * chunkResolution.x, cellResolution.y * cellSize * chunkResolution.y);
			collider.offset = collider.size / 2;
		}
	}
	private void OnValidate()
	{
		if (chunks != null)
		{
			for (int x = 0; x < chunkResolution.x; x++)
			{
				for (int y = 0; y < chunkResolution.y; y++)
				{
					float[,] v = chunks[x, y].Voxels;
					GetChunkData(x, y, ref v);
					chunks[x, y].Voxels = v;

					chunks[x, y].GenerateMesh();
				}
			}
		}
		
	}
	void CreateChunk(int x, int y)
	{

		Chunk chunk = Instantiate(chunkPrefab, transform);
		chunk.transform.localPosition = new Vector2(x * cellResolution.x - chunkResolution.x * cellResolution.x * 0.5f, y * cellResolution.y - chunkResolution.y * cellResolution.y * 0.5f);
		
		//set voxel data
		float[,] voxels = null;
		GetChunkData(x, y, ref voxels);

		chunk.Initialize(this, voxels);

		chunks[x, y] = chunk;
	}

	void GetChunkData(int chunkX, int chunkY, ref float[,] voxels)
	{
		if (voxels == null || voxels.Length != (cellResolution.x + 1 * cellResolution.x + 1))
			voxels = new float[cellResolution.x + 1, cellResolution.y + 1];

		Vector2 offset = new Vector2(chunkX * cellResolution.x * perlinSampleSize, chunkY * cellResolution.y * perlinSampleSize);

		for (int x = 0; x < cellResolution.x + 1; x++)
		{
			for (int y = 0; y < cellResolution.y + 1; y++)
			{
				voxels[x, y] = Mathf.Clamp01(Mathf.PerlinNoise(x * perlinSampleSize + offset.x, y * perlinSampleSize + offset.y));
			}
		}
	}

	public Bounds2D GetBounds()
	{
		return new Bounds2D(transform.position, cellResolution.x * cellSize * chunkResolution.x, cellResolution.y * cellSize * chunkResolution.y);
	}
	
	public Vector2Int ChunkResolution { get => chunkResolution; }
	public Vector2Int CellResolution { get => cellResolution; }
	public float CellSize { get => cellSize; }
	public float ValueThreshold { get => valueThreshold; }
	public Chunk[,] Chunks { get => chunks; }
}

public struct Bounds2D
{
	public Vector2 bottomLeft;
	public Vector2 topRight;

	public Bounds2D(Vector2 bottomLeft, Vector2 topRight)
	{
		this.bottomLeft = bottomLeft;
		this.topRight = topRight;
	}

	public Bounds2D(Vector2 centre, float width, float height)
	{
		this.bottomLeft = centre - new Vector2(width, height) * 0.5f;
		this.topRight = centre + new Vector2(width, height) * 0.5f;
	}

	public bool Contains(Vector2 point)
	{
		return point.x > bottomLeft.x && point.x < topRight.x
			&& point.y > bottomLeft.y && point.y < topRight.y;
	}

	public bool Intersects(Bounds2D other)
	{
		return !(other.bottomLeft.x > topRight.x
		|| other.topRight.x < bottomLeft.x
		|| other.topRight.y < bottomLeft.y
		|| other.bottomLeft.y > topRight.y);
	}

	public static bool Intersect(Bounds2D a, Bounds2D b)
	{
		return !(b.bottomLeft.x > a.topRight.x
		|| b.topRight.x < a.bottomLeft.x
		|| b.topRight.y < a.bottomLeft.y
		|| b.bottomLeft.y > a.topRight.y);
	}
}
