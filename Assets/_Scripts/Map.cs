using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour
{
	//Exposed
	[SerializeField] Vector2Int chunkResolution = new Vector2Int(5, 5);
	[SerializeField] Chunk chunkPrefab = null;
	[SerializeField] int cellResolution = 32;
	[SerializeField] [Min(0.001f)] float cellSize = 1.0f;
	[SerializeField] [Range(0, 1)] float valueThreshold = 0.5f;

	//Temp exposed
	[Range(0.000f, 1)] public float perlinSampleSize = 0.1f;

	//Private
	Chunk[,] chunks;
	public Vector2 HalfSize { get; private set; }

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

		ChunkSize = cellResolution * cellSize;
		HalfSize = new Vector2(ChunkSize * chunkResolution.x * 0.5f, ChunkSize * chunkResolution.y * 0.5f);
	}
	private void Start()
	{
		//register self to GameManager
		GameManager.Instance.RegisterMap(this);
	}

	public void ApplyStencil(Stencil stencil)
	{
		Vector2 sPos = stencil.Position;
		Vector2 point = transform.InverseTransformPoint(stencil.Position);
		point += HalfSize;
		stencil.Position = point;

		//assuming stencil position is relative to the centre of the map
		//find the chunks affected
		int xStart = (int)((stencil.XStart - cellSize) / ChunkSize);
		if (xStart < 0)
			xStart = 0;
		
		int xEnd = (int)((stencil.XEnd + cellSize) / ChunkSize);
		if (xEnd >= chunkResolution.x)
			xEnd = chunkResolution.x - 1;
		
		int yStart = (int)((stencil.YStart - cellSize) / ChunkSize);
		if (yStart < 0)
			yStart = 0;
		
		int yEnd = (int)((stencil.YEnd + cellSize) / ChunkSize);
		if (yEnd >= chunkResolution.y)
			yEnd = chunkResolution.y - 1;

		Debug.Log($"centre: ({point.x}, {point.y}) chunks X: s: {xStart}, e: {xEnd}. Y: s: {yStart}, e: {yEnd}");
	
		for (int y = yEnd; y >= yStart; y--)
		{
			for (int x = xEnd; x >= xStart; x--)
			{
				stencil.Position = new Vector2(point.x - x * ChunkSize, point.y - y * ChunkSize);
				chunks[x, y].ApplyStencil(stencil);
			}
		}

		stencil.Position = sPos;
	}

	//check if a stencil is colliding with the map
	public bool IsColliding(Stencil stencil)
	{
		Vector2 pos = transform.position;
		//just do a 2d AABB check with the stencil
		return (stencil.XStart	< pos.x + HalfSize.x)
			&& (stencil.XEnd	> pos.x - HalfSize.x)
			&& (stencil.YStart	< pos.y + HalfSize.y)
			&& (stencil.YEnd	> pos.y - HalfSize.y);
	}

	private void OnValidate()
	{
		if (chunks != null)
		{
			for (int x = 0; x < chunkResolution.x; x++)
			{
				for (int y = 0; y < chunkResolution.y; y++)
				{
					Voxel[,] v = chunks[x, y].Voxels;
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
		chunk.transform.localPosition = new Vector2(x * cellResolution - chunkResolution.x * cellResolution * 0.5f, y * cellResolution - chunkResolution.y * cellResolution * 0.5f);

		//set voxel data
		Voxel[,] voxels = null;
		GetChunkData(x, y, ref voxels);

		chunk.Initialize(this, voxels);

		chunks[x, y] = chunk;
	}

	//should access chunk data from a file, or generate it if not in file
	void GetChunkData(int chunkX, int chunkY, ref Voxel[,] voxels)
	{
		if (voxels == null || voxels.Length != (cellResolution + 1 * cellResolution + 1))
			voxels = new Voxel[cellResolution + 1, cellResolution + 1];

		Vector2 offset = new Vector2(chunkX * cellResolution * perlinSampleSize, chunkY * cellResolution * perlinSampleSize);

		for (int x = 0; x < cellResolution + 1; x++)
		{
			for (int y = 0; y < cellResolution + 1; y++)
			{
				voxels[x, y].value = Mathf.Clamp01(Mathf.PerlinNoise(x * perlinSampleSize + offset.x, y * perlinSampleSize + offset.y));
			}
		}
	}

	public Bounds2D GetBounds()
	{
		return new Bounds2D(transform.position, HalfSize.x * 2, HalfSize.y * 2);
	}
	
	public Vector2Int ChunkResolution { get => chunkResolution; }
	public int CellResolution { get => cellResolution; }
	public float CellSize { get => cellSize; }
	public float ChunkSize { get; private set; }
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
